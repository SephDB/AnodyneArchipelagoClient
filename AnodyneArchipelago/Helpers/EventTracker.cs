using System.Collections.Concurrent;
using AnodyneSharp.Entities;
using AnodyneSharp.Entities.Enemy.Apartment;
using AnodyneSharp.Entities.Enemy.Bedroom;
using AnodyneSharp.Entities.Enemy.Circus;
using AnodyneSharp.Entities.Enemy.Crowd;
using AnodyneSharp.Entities.Enemy.Etc;
using AnodyneSharp.Entities.Enemy.Hotel.Boss;
using AnodyneSharp.Entities.Enemy.Redcave;
using AnodyneSharp.Logging;
using AnodyneSharp.Registry;
using AnodyneSharp.States;
using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Models;
using Microsoft.Xna.Framework;
using Newtonsoft.Json.Linq;

namespace AnodyneArchipelago.Helpers
{

    public class EventTracker
    {
        private record EventWatch(string EventName, Func<bool> Test, Action Set, Func<bool>? RequiresQuickload = null)
        {
            public ulong BitMask => ((ulong)1) << EventWatchList.IndexOf(this);
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
                Tentacle("Tentacle_CL", new Guid("09241266-9657-6152-bb37-5ff0a7fddcf9")),
                Tentacle("Tentacle_CR", new Guid("95d6cf28-66df-54a5-0aec-6b54f56a2edc")),
                Tentacle("Tentacle_L", new Guid("a0b1ccc8-849a-b61d-7742-bfaf11013b2a")),
                Tentacle("Tentacle_R", new Guid("ed3b58c5-9191-013c-6935-777766e39a65")),
                GameEvent("Opened_Redcave_R","red_cave_r_ss",1, () => GlobalState.CURRENT_MAP_NAME == "REDSEA" && GlobalState.CurrentMapGrid == new Point(4,4)),
                GameEvent("Opened_Redcave_L","red_cave_l_ss",1, () => GlobalState.CURRENT_MAP_NAME == "REDSEA" && GlobalState.CurrentMapGrid == new Point(2,4)),
                GameEvent("Opened_Redcave_N","red_cave_n_ss",2, () => GlobalState.CURRENT_MAP_NAME == "REDSEA" && GlobalState.CurrentMapGrid == new Point(3,3)),
                new("Extended_Swap",() => GlobalState.events.GetEvent("ExtendedSwap") > 0, () => ArchipelagoManager.EnableExtendedSwap()),
                BigKey("Green_Key",0),
                BigKey("Red_Key",1),
                BigKey("Blue_Key",2),
                GameEvent("SeenPuzzleHOTEL","SeenPuzzleHOTEL", RequiresQuickload:() => GlobalState.CURRENT_MAP_NAME == "GO" && GlobalState.CurrentMapGrid == new Point(2,3)),
                GameEvent("SeenPuzzleAPARTMENT","SeenPuzzleAPARTMENT", RequiresQuickload:() => GlobalState.CURRENT_MAP_NAME == "GO" && GlobalState.CurrentMapGrid == new Point(2,3)),
                GameEvent("SeenPuzzleCIRCUS","SeenPuzzleCIRCUS", RequiresQuickload:() => GlobalState.CURRENT_MAP_NAME == "GO" && GlobalState.CurrentMapGrid == new Point(2,3)),
                NexusGate("STREET", "Street"),
                NexusGate("OVERWORLD", "Overworld"),
                NexusGate("REDCAVE", "Red Cave exit"),
                NexusGate("CROWD", "Crowd exit"),
                NexusGate("APARTMENT", "Apartment floor 1"),
                NexusGate("HOTEL", "Hotel floor 4"),
                NexusGate("CIRCUS", "Circus"),
                NexusGate("CLIFF", "Cliff"),
                NexusGate("FOREST", "Forest"),
                NexusGate("WINDMILL", "Windmill entrance"),
                NexusGate("REDSEA", "Red Sea"),
                NexusGate("BEACH", "Beach"),
                NexusGate("BEDROOM", "Bedroom exit"),
                NexusGate("FIELDS", "Fields"),
                NexusGate("GO", "Go bottom"),
                NexusGate("TERMINAL", "Terminal"),
                NexusGate("HAPPY", "Happy"),
                NexusGate("SPACE", "Space"),
                NexusGate("CELL", "Cell"),
                NexusGate("SUBURB", "Suburb"),
                NexusGate("BLUE", "Blue"),
                GameEvent("BlueDone","BlueDone"),
                GameEvent("HappyDone","HappyDone")
            ];

        static EventWatch NexusGate(string MapName, string eventName)
        {
            return new($"Nexus{eventName}", () => GlobalState.events.ActivatedNexusPortals.Contains(MapName), () => GlobalState.events.ActivatedNexusPortals.Add(MapName), () => GlobalState.CURRENT_MAP_NAME == "NEXUS");
        }

        static EventWatch GameEvent(string DataName, string EventName, int count = 1, Func<bool>? RequiresQuickload = null)
        {
            return new(DataName, () => GlobalState.events.GetEvent(EventName) >= count, () => GlobalState.events.SetEvent(EventName, count), RequiresQuickload);
        }

        static EventWatch Boss(string DataName, string AreaName, Type? boss_type = null)
        {
            EventWatch ret = new(DataName, () => GlobalState.events.BossDefeated.Contains(AreaName), () =>
            {
                GlobalState.events.BossDefeated.Add(AreaName);
                if (boss_type != null)
                {
                    EntityManager.GetMapEntities(AreaName).Find(p => p.Type == boss_type)!.Alive = false;
                }
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
        private ConcurrentDictionary<Guid, ulong> _requestedChanges = new();
        private ulong LastFrameMask = 0;

        private DataStorageElement BitMap { get => _session.DataStorage[Scope.Slot, BitMapName]; set => _session.DataStorage[Scope.Slot, BitMapName] = value; }
        private DataStorageElement EventArray { get => _session.DataStorage[Scope.Slot, ArrayName]; set => _session.DataStorage[Scope.Slot, ArrayName] = value; }

        public EventTracker(ArchipelagoSession session)
        {
            _session = session;

            foreach (var ev in EventWatchList)
            {
                DebugLogger.AddDebug($"{ev.BitMask}");
            }

            BitMap.OnValueChanged += BitMap_OnValueChanged;
            BitMap.Initialize(0);
            EventArray.Initialize(Enumerable.Empty<string>());

            BitMap.GetAsync<ulong>().ContinueWith(m => MaskToEvents(m.Result).ForEach(_serverEvents.Enqueue));
        }

        private void BitMap_OnValueChanged(JToken originalValue, JToken newValue, Dictionary<string, JToken> additionalArguments)
        {
            //Throw everything in to update next frame, it can filter there on what is actually set
            ulong bitmask = (ulong)newValue;
            var events = MaskToEvents(bitmask);
            events.ForEach(_serverEvents.Enqueue);

            if (additionalArguments.TryGetValue("UpdateCheck", out JToken? value) && _requestedChanges.TryRemove((Guid)value, out ulong requested))
            {
                //Only get the events we actually added and add them to the datastorage set
                ulong mask = NewlySet((ulong)originalValue & requested, (ulong)newValue & requested);
                DebugLogger.AddDebug($"Received new mask {mask}");
                var newEvents = MaskToEvents(mask);
                foreach (var ev in newEvents)
                {
                    DebugLogger.AddDebug($"Setting {ev.EventName}");
                }
                EventArray += newEvents.Select(e => e.EventName).ToArray();
            }
        }

        public void Update()
        {
            ulong currentMask = CurrentMask();
            ulong newEntries = NewlySet(LastFrameMask, currentMask);

            bool needsQuickLoad = false;

            while (_serverEvents.TryDequeue(out var ev))
            {
                ulong mask = ev.BitMask;
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
                foreach (var ev in MaskToEvents(newEntries))
                {
                    DebugLogger.AddDebug($"Setting mask for {ev.EventName}");
                }
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

        static List<EventWatch> MaskToEvents(ulong mask)
        {
            List<EventWatch> ret = [];
            foreach (var ev in EventWatchList)
            {
                if ((mask & ev.BitMask) == ev.BitMask)
                {
                    ret.Add(ev);
                }
            }
            return ret;
        }

        static ulong CurrentMask()
        {
            ulong currentMask = 0;

            foreach (var ev in EventWatchList)
            {
                if (ev.Test())
                {
                    currentMask |= ev.BitMask;
                }
            }

            return currentMask;
        }

        static ulong NewlySet(ulong old, ulong newValue)
        {
            return newValue & ~old;
        }
    }
}
