﻿using Microsoft.Xna.Framework;

namespace AnodyneArchipelago
{
    public class SwapData
    {
        public static List<Rectangle> GetRectanglesForMap(string mapName, bool extendedSwap)
        {
            if (extendedSwap)
            {
                switch (mapName)
                {
                    case "APARTMENT": return [new Rectangle(1280, 1120, 161, 161)];
                    case "BEACH":
                        return [
                        new Rectangle(336, 160, 48, 48),     // Secret glen
                        new Rectangle(0, 736, 160, 160),     // Left edge, to get out of bounds
                        new Rectangle(688, 1072, 48, 64),    // Bottom edge, to get to secret chest
                    ];
                    case "BEDROOM": return [new Rectangle(832, 368, 256, 416)];
                    case "BLANK":
                        return [
                        new Rectangle(0, 0, 480, 960),       // Left half
                        new Rectangle(640, 0, 320, 1120),    // Right half
                    ];
                    case "CELL":
                        return [
                        // This is basically all of the map except for the outer walls, the 24 Card Gate area, and the gauntlet
                        new Rectangle(16,16,1248,304-16),
                        new Rectangle(16,352,768,912),
                        new Rectangle(48,304,1216,416),
                    ];
                    case "CIRCUS": return [new Rectangle(1120, 0, 161, 161)];
                    case "DRAWER": return [new Rectangle(0, 0, 960, 1440)];
                    case "FIELDS":
                        return [
                        new Rectangle(208, 192, 176, 112),   // Near terminal secret chest
                        new Rectangle(736, 336, 208, 144),   // Near overworld secret chest
                        new Rectangle(1488, 1120, 256, 160), // Secret glen
                        new Rectangle(1296, 1600, 128, 160), // Blocked river 1
                        new Rectangle(1648, 1488, 112, 96),  // Blocked river 2
                    ];
                    case "FOREST": return [new Rectangle(0, 0, 800, 1440)];
                    case "GO":
                        return [
                        new Rectangle(352, 496, 96, 112),    // Color puzzle
                        new Rectangle(32, 656, 208, 128),    // Secret color puzzle
                    ];
                    case "HOTEL":
                        return [
                        new Rectangle(1280, 1760, 161, 161), // Post-boss room
                        new Rectangle(480, 72, 320, 72),     // Roof secret
                    ];
                    case "OVERWORLD": return [new Rectangle(16, 480, 272, 176)];
                    case "REDSEA": return [new Rectangle(480, 976, 160, 112)];
                    case "SUBURB": return [new Rectangle(320, 640, 160, 160)];
                    case "STREET": return [new Rectangle(160, 864, 160, 160)];
                    case "SPACE": return [new Rectangle(800, 640, 160, 160)];
                    case "WINDMILL": return [new Rectangle(224, 1216, 96, 48)];
                }
            }
            else
            {
                switch (mapName)
                {
                    case "APARTMENT": return [new Rectangle(1280, 1120, 161, 161)];
                    case "CIRCUS": return [new Rectangle(1120, 0, 161, 161)];
                    case "GO": return [new Rectangle(352, 496, 96, 112)];
                    case "HOTEL": return [new Rectangle(1280, 1760, 161, 161)];
                }
            }

            return [];
        }
    }
}
