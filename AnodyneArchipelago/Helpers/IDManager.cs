using System;
using System.Collections.Generic;
using System.Text;

namespace AnodyneArchipelago.Helpers
{
    public enum RegionID
    {
        APARTMENT,
        BEACH,
        BEDROOM,
        BLANK,
        BLUE,
        BOSS_RUSH,
        CELL,
        CIRCUS,
        CLIFF,
        CROWD,
        DEBUG,
        DRAWER,
        FIELDS,
        FOREST,
        GO,
        HAPPY,
        HOTEL,
        NEXUS,
        OVERWORLD,
        REDCAVE,
        REDSEA,
        SPACE,
        STREET,
        SUBURB,
        TERMINAL,
        WINDMILL
    }

    public enum LocationType
    {
        Chest,
        Cicada,
        BigKey,
        Tentacle,
        Dust,
        Nexus,
        AreaEvent
    }

    public enum ItemType
    {
        Inventory,
        Cicada,
        Card,
        Secret,
        Keys,
        BigKey,
        StatueUnlocks,
        Heal,
        Nexus,
        Trap,
        RedCaveUnlock,
        Fountain,
        TradingQuest
    }

    public record struct Location(RegionID Region, LocationType Type, long Index)
    {
        public readonly long ID => (((1000 + (long)Region) * 1000) + (long)Type) * 1000 + Index;

        public static Location Create(long ID)
        {
            long index = ID % 1000;
            ID /= 1000;
            long type = ID % 1000;
            ID /= 1000;
            long region = ID % 1000;
            return new Location((RegionID)region, (LocationType)type, index);
        }
    }

    public record struct Item(ItemType Type, long SubType, RegionID Region = RegionID.APARTMENT)
    {
        public readonly long ID => (((1000 + (long)Type) * 1000) + (long)Region) * 1000 + SubType;

        public static Item Create(long ID)
        {
            long index = ID % 1000;
            ID /= 1000;
            long region = ID % 1000;
            ID /= 1000;
            long type = ID % 1000;
            return new Item((ItemType)type, index, (RegionID)region);
        }
    }
}
