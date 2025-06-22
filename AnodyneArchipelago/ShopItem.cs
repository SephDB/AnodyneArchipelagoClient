using Newtonsoft.Json;

namespace AnodyneArchipelago
{
    public readonly struct ShopItem(long itemID, int playerSlot)
    {
        [JsonProperty("item")]
        public readonly long itemID = itemID;
        [JsonProperty("player")]
        public readonly int playerSlot = playerSlot;
    }
}
