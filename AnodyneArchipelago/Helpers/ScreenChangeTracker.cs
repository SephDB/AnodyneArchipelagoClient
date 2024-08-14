using AnodyneSharp.Registry;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Text;

namespace AnodyneArchipelago.Helpers
{
    public class ScreenChangeTracker
    {
        public (string mapName, Point location) Tracker;
        
        public bool Update()
        {
            (string map, Point location) pos = (GlobalState.CURRENT_MAP_NAME, GlobalState.CurrentMapGrid);
            if(pos != Tracker)
            {
                Tracker = pos;
                return true;
            }
            return false;
        }

        public int GetIndex()
        {
            return Tracker.location.X + Tracker.location.Y * GlobalState.MAP_GRID_WIDTH;
        }
    }
}
