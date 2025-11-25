using AnodyneArchipelago.Helpers;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AnodyneArchipelago
{
    public class SwapData
    {
        private Dictionary<RegionID, List<Rectangle>> _singleSwap = [];
        private Dictionary<RegionID, List<Rectangle>> _extendedSwap = [];

        public SwapData(JArray swapData) 
        {
            RegionID[] regions = Enum.GetValues<RegionID>();

            _singleSwap = [];
            _extendedSwap = [];

            for (int i = 0; i < regions.Length; i++) 
            { 
                RegionID region = regions[i];
                JToken values = swapData.ElementAt(i);

                _singleSwap.Add(region, ConvertData(values.First()));
                _extendedSwap.Add(region, ConvertData(values.Last()));
            }
        }

        public List<Rectangle> GetRectanglesForMap(RegionID region, bool extendedSwap)
        {
            List<Rectangle> areas = _singleSwap.GetValueOrDefault(region) ?? [];

            if (extendedSwap)
            {
                areas.AddRange(_extendedSwap.GetValueOrDefault(region) ?? []);
            }

            return areas;
        }

        private static List<Rectangle> ConvertData(JToken values)
        {
            List<Rectangle> areas = [];
            foreach (var value in values)
            {
                var v = value.Values<int>().ToArray();
                areas.Add(new(v[0], v[1], v[2], v[3]));
            }

            return areas;
        }
    }
}
