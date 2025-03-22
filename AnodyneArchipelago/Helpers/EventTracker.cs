using AnodyneSharp.Entities;
using AnodyneSharp.Registry;
using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Models;
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
                Boss("Defeated_Seer","BEDROOM"),
                Boss("Defeated_Wall","CROWD"),
                Boss("Defeated_Rogue","REDCAVE"),
                Boss("Defeated_Manager","HOTEL"),
                Boss("Defeated_Watcher","APARTMENT"),
                Boss("Defeated_Servants","CIRCUS"),
                Boss("Defeated_Sage","TERMINAL"),
                Boss("Defeated_Briar","GO"),
                GameEvent("Opened_Windmill","WindmillOpened"),
                Tentacle("Tentacle_CL", Locations.LocationsGuids["Red Cave - Middle Cave Left Tentacle"]),
                Tentacle("Tentacle_CR", Locations.LocationsGuids["Red Cave - Middle Cave Right Tentacle"]),
                Tentacle("Tentacle_L", Locations.LocationsGuids["Red Cave - Left Cave Tentacle"]),
                Tentacle("Tentacle_R", Locations.LocationsGuids["Red Cave - Right Cave Tentacle"]),
                GameEvent("Opened_Redcave_R","red_cave_r_ss"),
                GameEvent("Opened_Redcave_L","red_cave_l_ss"),
                GameEvent("Opened_Redcave_C","red_cave_n_ss",2) with {Set = () => { } }, //no-op set since tentacle set handles this already. If not, we're not in vanilla and the relevant item sets this directly
                new("Extended_Swap",() => GlobalState.events.GetEvent("ExtendedSwap") > 0, () => Plugin.ArchipelagoManager!.EnableExtendedSwap()),
                BigKey("Green_Key",0),
                BigKey("Red_Key",1),
                BigKey("Blue_Key",2),
            ];

        static EventWatch GameEvent(string DataName, string EventName, int count = 1, Func<bool>? RequiresQuickload = null)
        {
            return new(DataName, () => GlobalState.events.GetEvent(EventName) >= count, () => GlobalState.events.SetEvent(EventName, count), RequiresQuickload);
        }

        static EventWatch Boss(string DataName, string AreaName)
        {
            return new(DataName, () => GlobalState.events.BossDefeated.Contains(AreaName), () => GlobalState.events.BossDefeated.Add(AreaName));
        }

        static EventWatch Tentacle(string DataName, Guid guid)
        {
            return new(DataName, () => !(EntityManager.State.GetValueOrDefault(guid)?.Alive ?? true), () =>
            {
                if (Plugin.ArchipelagoManager!.VanillaRedCave)
                {
                    EntityPreset preset = EntityManager.GetMapEntities("REDCAVE").Find(p => p.EntityID == guid)!;
                    int f = preset.Frame;
                    EntityManager.SetAlive(guid, false);
                    //re-count the number the event should be to prevent local double counting, in case both co-op players are in the cutscene with perfect timing difference
                    int count = EntityManager.GetMapEntities("REDCAVE").Where(p => p.Type == typeof(Red_Pillar) && p.Frame == f && EntityManager.State.TryGetValue(p.EntityID, out var s) && !s.Alive).Count();
                    char c = " nnlr"[f];
                    GlobalState.events.SetEvent($"red_cave_{c}_ss", count);
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
                EventArray = EventArray + newEvents.Select(e=>e.EventName).ToArray();
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
