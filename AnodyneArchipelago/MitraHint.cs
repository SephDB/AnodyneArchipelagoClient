using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace AnodyneArchipelago
{
    public readonly struct MitraHint
    {
        [JsonProperty("item")]
        public readonly int itemID;
        [JsonProperty("location")]
        public readonly int locationID;
        [JsonProperty("location_player")]
        public readonly int playerSlot;
    }
}
