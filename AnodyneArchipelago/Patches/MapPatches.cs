using AnodyneSharp.Registry;
using Microsoft.Xna.Framework;

namespace AnodyneArchipelago.Patches
{
    public class MapPatches
    {
        public static string ChangeMap(string mapName, string BG)
        {
            var map = BG.Trim().Split('\n').Select(s => s.Split(',').Select(int.Parse).ToArray()).ToArray();

            if (mapName == "FIELDS")
            {
                // Place a rock blocking access to Terminal without the red key.
                map[47][31] = 11;
            }
            else if (mapName == "CROWD")
            {
                //Fix easy OOL jump past Crowd's statue
                map[43][92] = 4;
            }

            if (Plugin.ArchipelagoManager?.ColorPuzzleRandomized ?? false)
            {
                ColorPuzzle puzzle = Plugin.ArchipelagoManager!.ColorPuzzle;

                if (mapName == "HOTEL")
                {
                    Point pos = puzzle.HotelPos;

                    map[116][78] = 82;
                    map[113 + pos.Y][73 + pos.X] = 31;
                }
                else if (mapName == "APARTMENT")
                {
                    Point pos = puzzle.ApartmentPos;

                    map[52][86] = 153;
                    map[51 + pos.Y][82 + pos.X] = 101;
                }
                else if (mapName == "CIRCUS")
                {
                    Point pos = puzzle.CircusPos;

                    map[15][72] = 60;
                    map[11 + pos.Y][72 + pos.X] = 46;
                }
                else if (mapName == "GO")
                {
                    int hotelTile = map[35][27];
                    int apartmentTile = map[33][26];
                    int circusTile = map[36][22];
                    map[33][26] = map[35][27] = map[36][22] = 114;
                    if (Plugin.ArchipelagoManager!.ColorPuzzleHelp)
                    {
                        if (GlobalState.events.GetEvent("SeenPuzzleHOTEL") > 0)
                        {
                            map[32 + puzzle.HotelPos.Y][22 + puzzle.HotelPos.X] = hotelTile;
                        }
                        if (GlobalState.events.GetEvent("SeenPuzzleAPARTMENT") > 0)
                        {
                            map[32 + puzzle.ApartmentPos.Y][22 + puzzle.ApartmentPos.X] = apartmentTile;
                        }
                        if (GlobalState.events.GetEvent("SeenPuzzleCIRCUS") > 0)
                        {
                            map[32 + puzzle.CircusPos.Y][22 + puzzle.CircusPos.X] = circusTile;
                        }
                    }
                }
            }

            return string.Join('\n', map.Select(line => string.Join(',', line)));
        }
    }
}
