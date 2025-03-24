using AnodyneSharp.Entities;
using AnodyneSharp.Entities.Enemy.Apartment;
using AnodyneSharp.Entities.Enemy.Bedroom;
using AnodyneSharp.Entities.Enemy.Circus;
using AnodyneSharp.Entities.Enemy.Crowd;
using AnodyneSharp.Entities.Enemy.Etc;
using AnodyneSharp.Entities.Enemy.Hotel.Boss;
using AnodyneSharp.Entities.Enemy.Redcave;
using AnodyneSharp.Entities.Interactive.Npc.Hotel;
using AnodyneSharp.Registry;
using AnodyneSharp.States;
using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Models;
using Microsoft.Xna.Framework;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace AnodyneArchipelago.Helpers
{

    public class EventTracker
    {
        private record EventWatch(string EventName, Func<bool> Test, Action Set, Func<bool>? RequiresQuickload = null)
        {
            public long BitMask => 1 << EventWatchList.IndexOf(this);
        };

        static readonly string BitMapName = "EventMap";
        static readonly string ArrayName = "EventArray";

        //Append-only for backwards compatibility reasons
        static readonly List<EventWatch> EventWatchList = [
                Boss("Defeated_Seer","BEDROOM", typeof(Seer)),
                Boss("Defeated_Wall","CROWD", typeof(WallBoss)),
                Boss("Defeated_Rogue","REDCAVE", typeof(Red_Boss)),
                Boss("Defeated_Manager","HOTEL", typeof(LandPhase)) with {
                    RequiresQuickload = () => GlobalState.CURRENT_MAP_NAME == "HOTEL"
                        && EntityManager.GetGridEntities(GlobalState.CURRENT_MAP_NAME, GlobalState.CurrentMapGrid).Find(p => p.Type == typeof(LandPhase) || p.Type == typeof(WaterPhase)) != null
                },
                Boss("Defeated_Watcher","APARTMENT", typeof(SplitBoss)),
                Boss("Defeated_Servants","CIRCUS", typeof(CircusFolks)),
                Boss("Defeated_Sage","TERMINAL", typeof(SageBoss)),
                Boss("Defeated_Briar","GO"), //No boss type for reloading here, Briar always spawns
                GameEvent("Opened_Windmill","WindmillOpened"),
                Tentacle("Tentacle_CL", Locations.LocationsGuids["Red Cave - Middle Cave Left Tentacle"]),
                Tentacle("Tentacle_CR", Locations.LocationsGuids["Red Cave - Middle Cave Right Tentacle"]),
                Tentacle("Tentacle_L", Locations.LocationsGuids["Red Cave - Left Cave Tentacle"]),
                Tentacle("Tentacle_R", Locations.LocationsGuids["Red Cave - Right Cave Tentacle"]),
                GameEvent("Opened_Redcave_R","red_cave_r_ss",1, () => GlobalState.CURRENT_MAP_NAME == "REDSEA" && GlobalState.CurrentMapGrid == new Point(4,4)),
                GameEvent("Opened_Redcave_L","red_cave_l_ss",1, () => GlobalState.CURRENT_MAP_NAME == "REDSEA" && GlobalState.CurrentMapGrid == new Point(2,4)),
                GameEvent("Opened_Redcave_N","red_cave_n_ss",2, () => GlobalState.CURRENT_MAP_NAME == "REDSEA" && GlobalState.CurrentMapGrid == new Point(3,3)),
                new("Extended_Swap",() => GlobalState.events.GetEvent("ExtendedSwap") > 0, () => Plugin.ArchipelagoManager!.EnableExtendedSwap()),
                BigKey("Green_Key",0),
                BigKey("Red_Key",1),
                BigKey("Blue_Key",2),
            ];

        static EventWatch GameEvent(string DataName, string EventName, int count = 1, Func<bool>? RequiresQuickload = null)
        {
            return new(DataName, () => GlobalState.events.GetEvent(EventName) >= count, () => GlobalState.events.SetEvent(EventName, count), RequiresQuickload);
        }

        static EventWatch Boss(string DataName, string AreaName, Type? boss_type = null)
        {
            EventWatch ret = new(DataName, () => GlobalState.events.BossDefeated.Contains(AreaName), () =>
            {
                GlobalState.events.BossDefeated.Add(AreaName);
                EntityManager.GetMapEntities(AreaName).Find(p => p.Type == boss_type)!.Alive = false;
            });
            if (boss_type != null)
            {
                ret = ret with
                {
                    RequiresQuickload = () => GlobalState.CURRENT_MAP_NAME == AreaName && EntityManager.GetGridEntities(GlobalState.CURRENT_MAP_NAME, GlobalState.CurrentMapGrid).Find(p => p.Type == boss_type) != null
                };
            }
            return ret;
        }


        static EventWatch Tentacle(string DataName, Guid guid)
        {
            return new(DataName, () => !(EntityManager.State.GetValueOrDefault(guid)?.Alive ?? true), () =>
            {
                if (Plugin.ArchipelagoManager!.VanillaRedCave)
                {
                    EntityPreset preset = EntityManager.GetMapEntities("REDCAVE").Find(p => p.EntityID == guid)!;
                    int f = preset.Frame;
                    preset.Alive = false;

                    char c = " nnlr"[f];
                    GlobalState.events.IncEvent($"red_cave_{c}_ss");
                }
            },
            () => Plugin.ArchipelagoManager!.VanillaRedCave && EntityManager.GetGridEntities(GlobalState.CURRENT_MAP_NAME, GlobalState.CurrentMapGrid).Find(p => p.EntityID == guid) != null
            );
        }

        static EventWatch BigKey(string DataName, int id)
        {
            return new(DataName, () => GlobalState.inventory.BigKeyStatus[id], () => GlobalState.inventory.BigKeyStatus[id] = true);
        }

        private ArchipelagoSession _session;
        private ConcurrentQueue<EventWatch> _serverEvents = new();
        private ConcurrentDictionary<Guid, long> _requestedChanges = new();
        private long LastFrameMask = 0;

        private DataStorageElement BitMap { get => _session.DataStorage[Scope.Slot, BitMapName]; set => _session.DataStorage[Scope.Slot, BitMapName] = value; }
        private DataStorageElement EventArray { get => _session.DataStorage[Scope.Slot, ArrayName]; set => _session.DataStorage[Scope.Slot, ArrayName] = value; }

        public EventTracker(ArchipelagoSession session)
        {
            _session = session;

            BitMap.OnValueChanged += BitMap_OnValueChanged;
            BitMap.Initialize(0);
            EventArray.Initialize(Enumerable.Empty<string>());

            BitMap.GetAsync<long>().ContinueWith(m => MaskToEvents(m.Result).ForEach(_serverEvents.Enqueue));
        }

        private void BitMap_OnValueChanged(JToken originalValue, JToken newValue, Dictionary<string, JToken> additionalArguments)
        {
            //Throw everything in to update next frame, it can filter there on what is actually set
            long bitmask = (long)newValue;
            var events = MaskToEvents(bitmask);
            events.ForEach(_serverEvents.Enqueue);

            if (additionalArguments.TryGetValue("UpdateCheck", out JToken? value) && _requestedChanges.TryRemove((Guid)value, out long requested))
            {
                //Only get the events we actually added and add them to the datastorage set
                long mask = NewlySet((long)originalValue & requested, (long)newValue & requested);
                var newEvents = MaskToEvents(mask);
                EventArray = EventArray + newEvents.Select(e => e.EventName).ToArray();
            }
        }

        public void Update()
        {
            long currentMask = CurrentMask();
            long newEntries = NewlySet(LastFrameMask, currentMask);

            bool needsQuickLoad = false;

            while (_serverEvents.TryDequeue(out var ev))
            {
                long mask = ev.BitMask;
                if ((mask & currentMask) == mask)
                {
                    newEntries &= ~mask;
                    continue;
                }
                ev.Set();
                needsQuickLoad |= ev.RequiresQuickload?.Invoke() ?? false;
            }

            if (newEntries != 0)
            {
                Guid guid = Guid.NewGuid();
                _requestedChanges.TryAdd(guid, newEntries);
                BitMap = BitMap + Bitwise.Or(newEntries) + AdditionalArgument.Add("UpdateCheck", guid);
            }

            LastFrameMask = CurrentMask();

            if (needsQuickLoad)
            {
                GlobalState.CheckPoint quicksave_checkpoint = new(GlobalState.CURRENT_MAP_NAME, Plugin.Player!.Position);
                GlobalState.Save save = new();
                GlobalState.ResetValues();
                GlobalState.LoadSave(save);
                quicksave_checkpoint.Warp(Vector2.Zero);
                GlobalState.WARP = false;
                GlobalState.GameState.SetState<PlayState>();
                //Instant transition
                GlobalState.black_overlay.Deactivate();
                GlobalState.pixelation.Deactivate();
            }
        }

        static List<EventWatch> MaskToEvents(long mask)
        {
            List<EventWatch> ret = [];
            foreach (var ev in EventWatchList)
            {
                if ((mask & 1) == 1)
                {
                    ret.Add(ev);
                }
                mask >>= 1;
            }
            return ret;
        }

        static long CurrentMask()
        {
            long currentMask = 0;

            int currentBit = 0;
            foreach (var ev in EventWatchList)
            {
                if (ev.Test())
                {
                    currentMask |= 1u << currentBit;
                }
                currentBit++;
            }

            return currentMask;
        }

        static long NewlySet(long old, long newValue)
        {
            return newValue & ~old;
        }
    }
}
