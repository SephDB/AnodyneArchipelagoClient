using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace AnodyneArchipelago
{
    public readonly struct ShopItem
    {
        [JsonProperty("item")]
        public readonly int itemID;
        [JsonProperty("player")]
        public readonly int playerSlot;
    }
}
