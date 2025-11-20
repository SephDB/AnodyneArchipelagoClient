using Newtonsoft.Json;

namespace AnodyneArchipelago
{
    public readonly struct MitraHint
    {
        [JsonProperty("item")]
        public readonly long itemID;
        [JsonProperty("location")]
        public readonly long locationID;
        [JsonProperty("location_player")]
        public readonly int playerSlot;

        public override string ToString()
        {
            return $"{playerSlot} - {locationID} - {itemID}";
        }
    }
}
