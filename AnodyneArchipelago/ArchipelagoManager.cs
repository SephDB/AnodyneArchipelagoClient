using AnodyneArchipelago.Patches;
using AnodyneSharp.Entities;
using AnodyneSharp.Entities.Gadget.Treasures;
using AnodyneSharp.MapData;
using AnodyneSharp.Registry;
using AnodyneSharp.Sounds;
using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.BounceFeatures.DeathLink;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.MessageLog.Messages;
using Archipelago.MultiClient.Net.Models;
using Archipelago.MultiClient.Net.Packets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace AnodyneArchipelago
{
    public enum BigKeyShuffle
    {
        Vanilla = 0,
        Unlocked = 1,
        OwnWorld = 3,
        AnyWorld = 4,
        DifferentWorld = 5,
    }

    public enum VictoryCondition
    {
        DefeatBriar = 0,
        AllCards = 1,
    }

    public enum PostgameMode
    {
        Disabled = 0,
        Vanilla = 1,
        Unlocked = 2,
        Progression = 3,
    }

    public class ArchipelagoManager
    {
        private ArchipelagoSession _session;
        private int _itemIndex = 0;
        private DeathLinkService _deathLinkService;

        private string _seedName;
        private long _endgameCardRequirement = 36;
        private ColorPuzzle _colorPuzzle = new();
        private bool _unlockSmallKeyGates = false;
        private BigKeyShuffle _bigKeyShuffle;
        private bool _vanillaHealthCicadas = false;
        private bool _vanillaRedCave = false;
        private bool _splitWindmill = false;
        private bool _forestBunnyChest = false;
        private VictoryCondition _victoryCondition;
        private List<string> _unlockedGates = new();
        private PostgameMode _postgameMode;

        private readonly Queue<ItemInfo> _itemsToCollect = new();
        private readonly Queue<string> _messages = new();
        private DeathLink? _pendingDeathLink = null;
        private string _deathLinkReason = null;
        private bool _receiveDeath = false;

        private Task<Dictionary<string, ScoutedItemInfo>> _scoutTask;

        public long EndgameCardRequirement => _endgameCardRequirement;
        public ColorPuzzle ColorPuzzle => _colorPuzzle;
        public bool UnlockSmallKeyGates => _unlockSmallKeyGates;
        public BigKeyShuffle BigKeyShuffle => _bigKeyShuffle;
        public bool VanillaHealthCicadas => _vanillaHealthCicadas;
        public bool VanillaRedCave => _vanillaRedCave;
        public bool SplitWindmill => _splitWindmill;
        public bool ForestBunnyChest => _forestBunnyChest;
        public VictoryCondition VictoryCondition => _victoryCondition;
        public PostgameMode PostgameMode => _postgameMode;

        public bool DeathLinkEnabled => _deathLinkService != null;

        public bool ReceivedDeath
        {
            get { return _receiveDeath; }
            set { _receiveDeath = value; }
        }

        public string DeathLinkReason
        {
            get { return _deathLinkReason; }
            set { _deathLinkReason = value; }
        }

        public async Task<LoginResult> Connect(string url, string slotName, string password)
        {
            LoginResult result;
            try
            {
                _session = ArchipelagoSessionFactory.CreateSession(url);
                _session.MessageLog.OnMessageReceived += OnMessageReceived;

                RoomInfoPacket roomInfoPacket = await _session.ConnectAsync();
                _seedName = roomInfoPacket.SeedName;

                result = await _session.LoginAsync("Anodyne", slotName, ItemsHandlingFlags.AllItems, null, null, null, password == "" ? null : password);
            }
            catch (Exception e)
            {
                return new LoginFailure(e.GetBaseException().Message);
            }

            _itemIndex = 0;
            _itemsToCollect.Clear();

            LoginSuccessful login = result as LoginSuccessful;
            if (login.SlotData.ContainsKey("endgame_card_requirement"))
            {
                _endgameCardRequirement = (long)login.SlotData["endgame_card_requirement"];
            }

            if (login.SlotData.ContainsKey("seed"))
            {
                Random rand = new Random((int)(long)login.SlotData["seed"]);
                _colorPuzzle.Initialize(rand);
            }

            if (login.SlotData.ContainsKey("unlock_gates"))
            {
                _unlockSmallKeyGates = (bool)login.SlotData["unlock_gates"];
            }
            else
            {
                _unlockSmallKeyGates = false;
            }

            if (login.SlotData.ContainsKey("shuffle_big_gates"))
            {
                _bigKeyShuffle = (BigKeyShuffle)(long)login.SlotData["shuffle_big_gates"];
            }
            else
            {
                _bigKeyShuffle = BigKeyShuffle.AnyWorld;
            }

            if (login.SlotData.ContainsKey("vanilla_health_cicadas"))
            {
                _vanillaHealthCicadas = (bool)login.SlotData["vanilla_health_cicadas"];
            }
            else
            {
                _vanillaHealthCicadas = false;
            }

            if (login.SlotData.ContainsKey("vanilla_red_cave"))
            {
                _vanillaRedCave = (bool)login.SlotData["vanilla_red_cave"];
            }
            else
            {
                _vanillaRedCave = false;
            }

            if (login.SlotData.ContainsKey("split_windmill"))
            {
                _splitWindmill = (bool)login.SlotData["split_windmill"];
            }
            else
            {
                _splitWindmill = false;
            }

            if (login.SlotData.ContainsKey("forest_bunny_chest"))
            {
                _forestBunnyChest = (bool)login.SlotData["forest_bunny_chest"];
            }
            else
            {
                _forestBunnyChest = false;
            }

            if (login.SlotData.ContainsKey("victory_condition"))
            {
                _victoryCondition = (VictoryCondition)(long)login.SlotData["victory_condition"];
            }
            else
            {
                _victoryCondition = VictoryCondition.DefeatBriar;
            }

            if (login.SlotData.ContainsKey("nexus_gates_unlocked"))
            {
                _unlockedGates = new(((Newtonsoft.Json.Linq.JArray)login.SlotData["nexus_gates_unlocked"]).Values<string>());
            }
            else
            {
                _unlockedGates = new();
            }

            if (login.SlotData.ContainsKey("postgame_mode"))
            {
                _postgameMode = (PostgameMode)(long)login.SlotData["postgame_mode"];
            }
            else
            {
                _postgameMode = PostgameMode.Disabled;
            }

            if (login.SlotData.ContainsKey("death_link") && (bool)login.SlotData["death_link"])
            {
                _deathLinkReason = null;
                _receiveDeath = false;

                _deathLinkService = _session.CreateDeathLinkService();
                _deathLinkService.OnDeathLinkReceived += OnDeathLinkReceived;
                _deathLinkService.EnableDeathLink();
            }
            else
            {
                _deathLinkService = null;
            }

            _scoutTask = Task.Run(() => ScoutAllLocations());

            Plugin.OnConnect();

            return result;
        }

        public static string GetNexusGateMapName(string region)
        {
            switch (region)
            {
                case "Apartment floor 1": return "APARTMENT";
                case "Beach": return "BEACH";
                case "Bedroom exit": return "BEDROOM";
                case "Blue": return "BLUE";
                case "Cell": return "CELL";
                case "Circus": return "CIRCUS";
                case "Cliff": return "CLIFF";
                case "Crowd floor 1": return "CROWD";
                case "Fields": return "FIELDS";
                case "Forest": return "FOREST";
                case "Go bottom": return "GO";
                case "Happy": return "HAPPY";
                case "Hotel floor 4": return "HOTEL";
                case "Overworld": return "OVERWORLD";
                case "Red Cave top": return "REDCAVE";
                case "Red Sea": return "REDSEA";
                case "Suburb": return "SUBURB";
                case "Space": return "SPACE";
                case "Terminal": return "TERMINAL";
                case "Windmill entrance": return "WINDMILL";
                default: return "";
            }
        }

        public void PostSaveloadInit()
        {
            foreach (string gate in _unlockedGates)
            {
                string mapName = GetNexusGateMapName(gate);
                if (mapName.Length > 0)
                {
                    GlobalState.events.ActivatedNexusPortals.Add(mapName);
                }
            }
        }

        ~ArchipelagoManager()
        {
            Disconnect();
        }

        public void Disconnect()
        {
            if (_session == null)
            {
                return;
            }

            Plugin.OnDisconnect();

            _session.Socket.DisconnectAsync();
            _session = null;
        }

        private async Task<Dictionary<string, ScoutedItemInfo>> ScoutAllLocations()
        {
            Dictionary<long, ScoutedItemInfo> locationInfo = await _session.Locations.ScoutLocationsAsync([.. _session.Locations.AllLocations]);

            Dictionary<string, ScoutedItemInfo> result = new();
            foreach (ScoutedItemInfo networkItem in locationInfo.Values)
            {
                string name = _session.Locations.GetLocationNameFromId(networkItem.LocationId,networkItem.LocationGame);
                if (name != null)
                {
                    result[name] = networkItem;
                }
            }

            return result;
        }

        public ItemInfo? GetScoutedLocation(string locationName)
        {
            if (_scoutTask == null || !_scoutTask.IsCompleted || !_scoutTask.Result.ContainsKey(locationName))
            {
                return null;
            }

            return _scoutTask.Result[locationName];
        }

        public string GetItemName(long id)
        {
            return _session.Items.GetItemName(id);
        }

        public string GetSeed()
        {
            return _seedName;
        }

        public int GetPlayer()
        {
            return _session.ConnectionInfo.Slot;
        }

        public void SendLocation(string location)
        {
            if (_session == null)
            {
                //Plugin.Instance.Log.LogError("Attempted to send location while disconnected");
                return;
            }

            _session.Locations.CompleteLocationChecks(_session.Locations.GetLocationIdFromName("Anodyne", location));
        }

        public void Update()
        {
            if (_session == null)
            {
                // We're not connected.
                return;
            }

            if (_session.Items.Index > _itemIndex)
            {
                for (int i = _itemIndex; i < _session.Items.Index; i++)
                {
                    string itemKey = $"ArchipelagoItem-{i}";
                    if (GlobalState.events.GetEvent(itemKey) != 0)
                    {
                        continue;
                    }

                    GlobalState.events.SetEvent(itemKey, 1);

                    ItemInfo item = _session.Items.AllItemsReceived[i];
                    _itemsToCollect.Enqueue(item);
                }

                _itemIndex = _session.Items.AllItemsReceived.Count;
            }

            if (Plugin.ReadyToReceive())
            {
                if (_itemsToCollect.Count > 0)
                {
                    ItemInfo item = _itemsToCollect.Dequeue();
                    HandleItem(item);
                }
                else if (_messages.Count > 0)
                {
                    GlobalState.Dialogue = _messages.Dequeue();
                }
                else if (_pendingDeathLink != null)
                {
                    GlobalState.CUR_HEALTH = 0;

                    string message;
                    if (_pendingDeathLink.Cause == null)
                    {
                        message = $"Received death from {_pendingDeathLink.Source}.";
                    }
                    else
                    {
                        message = $"Received death. Cause: {_pendingDeathLink.Cause}";
                    }

                    _pendingDeathLink = null;
                    _deathLinkReason = message;
                    _receiveDeath = true;
                }
            }
        }

        private static string GetMapNameForDungeon(string dungeon)
        {
            switch (dungeon)
            {
                case "Temple of the Seeing One": return "BEDROOM";
                case "Apartment": return "APARTMENT";
                case "Mountain Cavern": return "CROWD";
                case "Hotel": return "HOTEL";
                case "Red Cave": return "REDCAVE";
                case "Circus": return "CIRCUS";
                default: return "STREET";
            }
        }

        private static int GetCardNumberForName(string name)
        {
            switch (name)
            {
                case "Edward": return 0;
                case "Annoyer": return 1;
                case "Seer": return 2;
                case "Shieldy": return 3;
                case "Slime": return 4;
                case "PewLaser": return 5;
                case "Suburbian": return 6;
                case "Watcher": return 7;
                case "Silverfish": return 8;
                case "Gas Guy": return 9;
                case "Mitra": return 10;
                case "Miao": return 11;
                case "Windmill": return 12;
                case "Mushroom": return 13;
                case "Dog": return 14;
                case "Rock": return 15;
                case "Fisherman": return 16;
                case "Walker": return 17;
                case "Mover": return 18;
                case "Slasher": return 19;
                case "Rogue": return 20;
                case "Chaser": return 21;
                case "Fire Pillar": return 22;
                case "Contorts": return 23;
                case "Lion": return 24;
                case "Arthur and Javiera": return 25;
                case "Frog": return 26;
                case "Person": return 27;
                case "Wall": return 28;
                case "Blue Cube King": return 29;
                case "Orange Cube King": return 30;
                case "Dust Maid": return 31;
                case "Dasher": return 32;
                case "Burst Plant": return 33;
                case "Manager": return 34;
                case "Sage": return 35;
                case "Young": return 36;
                case "Carved Rock": return 37;
                case "City Man": return 38;
                case "Intra": return 39;
                case "Torch": return 40;
                case "Triangle NPC": return 41;
                case "Killer": return 42;
                case "Goldman": return 43;
                case "Broom": return 44;
                case "Rank": return 45;
                case "Follower": return 46;
                case "Rock Creature": return 47;
                case "Null": return 48;
                default: return 0;
            }
        }

        private void HandleItem(ItemInfo item)
        {
            if (item.Player == _session.ConnectionInfo.Slot && item.LocationId >= 0)
            {
                string itemKey = $"ArchipelagoLocation-{item.LocationId}";
                if (GlobalState.events.GetEvent(itemKey) > 0)
                {
                    return;
                }

                GlobalState.events.SetEvent(itemKey, 1);
            }

            string itemName = item.ItemName;

            if (itemName.StartsWith("Small Key"))
            {
                string dungeonName = itemName[11..];
                dungeonName = dungeonName[..^1];

                string mapName = GetMapNameForDungeon(dungeonName);
                GlobalState.inventory.AddMapKey(mapName, 1);
            }
            else if (itemName == "Green Key")
            {
                GlobalState.inventory.BigKeyStatus[0] = true;
            }
            else if (itemName == "Blue Key")
            {
                GlobalState.inventory.BigKeyStatus[2] = true;
            }
            else if (itemName == "Red Key")
            {
                GlobalState.inventory.BigKeyStatus[1] = true;
            }
            else if (itemName == "Jump Shoes")
            {
                GlobalState.inventory.CanJump = true;
            }
            else if (itemName == "Health Cicada")
            {
                GlobalState.MAX_HEALTH += 1;
                GlobalState.CUR_HEALTH = GlobalState.MAX_HEALTH;
            }
            else if (itemName == "Heal")
            {
                GlobalState.CUR_HEALTH = GlobalState.MAX_HEALTH;
            }
            else if (itemName == "Broom")
            {
                GlobalState.inventory.HasBroom = true;

                if (GlobalState.inventory.EquippedBroom == BroomType.NONE)
                {
                    GlobalState.inventory.EquippedBroom = BroomType.Normal;
                }
            }
            else if (itemName == "Swap")
            {
                GlobalState.inventory.HasTransformer = true;

                if (GlobalState.inventory.EquippedBroom == BroomType.NONE)
                {
                    GlobalState.inventory.EquippedBroom = BroomType.Transformer;
                }

                if ((_postgameMode == PostgameMode.Vanilla && GlobalState.events.GetEvent("DefeatedBriar") > 0) ||
                    _postgameMode == PostgameMode.Unlocked)
                {
                    EnableExtendedSwap();
                }
            }
            else if (itemName == "Progressive Swap")
            {
                GlobalState.inventory.HasTransformer = true;

                if (GlobalState.inventory.EquippedBroom == BroomType.NONE)
                {
                    GlobalState.inventory.EquippedBroom = BroomType.Transformer;
                }

                GlobalState.events.IncEvent("SwapStage");

                if (GlobalState.events.GetEvent("SwapStage") > 1)
                {
                    EnableExtendedSwap();

                    itemName = "Progressive Swap (Extended)";
                }
                else
                {
                    itemName = "Progressive Swap (Limited)";
                }
            }
            else if (itemName == "Extend")
            {
                GlobalState.inventory.HasLengthen = true;

                if (GlobalState.inventory.EquippedBroom == BroomType.NONE)
                {
                    GlobalState.inventory.EquippedBroom = BroomType.Long;
                }
            }
            else if (itemName == "Widen")
            {
                GlobalState.inventory.HasWiden = true;

                if (GlobalState.inventory.EquippedBroom == BroomType.NONE)
                {
                    GlobalState.inventory.EquippedBroom = BroomType.Wide;
                }
            }
            else if (itemName == "Temple of the Seeing One Statue")
            {
                // TODO: This and the other two: move while on the same map.
                GlobalState.events.SetEvent("StatueMoved_Temple", 1);
            }
            else if (itemName == "Mountain Cavern Statue")
            {
                GlobalState.events.SetEvent("StatueMoved_Mountain", 1);
            }
            else if (itemName == "Red Cave Statue")
            {
                GlobalState.events.SetEvent("StatueMoved_Grotto", 1);
            }
            else if (itemName == "Progressive Red Cave")
            {
                GlobalState.events.IncEvent("ProgressiveRedGrotto");
                if (!VanillaRedCave)
                {
                    switch (GlobalState.events.GetEvent("ProgressiveRedGrotto"))
                    {
                        case 1:
                            GlobalState.events.SetEvent("red_cave_l_ss", 1);
                            break;
                        case 2:
                            GlobalState.events.SetEvent("red_cave_r_ss", 1);
                            break;
                        case 3:
                            GlobalState.events.SetEvent("red_cave_n_ss", 2);
                            break;
                    }
                }
            }
            else if (itemName.StartsWith("Card ("))
            {
                string cardName = itemName[6..];
                cardName = cardName[..^1];

                int cardIndex = GetCardNumberForName(cardName);
                CardTreasure cardTreasure = new(Plugin.Player.Position, cardIndex);
                cardTreasure.GetTreasure();
                GlobalState.SpawnEntity(cardTreasure);
            }
            else if (itemName == "Cardboard Box")
            {
                GlobalState.events.SetEvent("ReceivedCardboardBox", 1);
            }
            else if (itemName == "Biking Shoes")
            {
                GlobalState.events.SetEvent("ReceivedBikingShoes", 1);
            }

            string message;
            if (item.Player == _session.ConnectionInfo.Slot)
            {
                message = $"Found {itemName}!";
            }
            else
            {
                string otherPlayer = _session.Players.GetPlayerAlias(item.Player);
                message = $"Received {itemName} from {otherPlayer}.";
            }

            GlobalState.Dialogue = message;
        }

        public void ActivateGoal()
        {
            _session.SetGoalAchieved();
        }

        private void OnMessageReceived(LogMessage message)
        {
            switch (message)
            {
                case ItemSendLogMessage itemSendLogMessage:
                    if (itemSendLogMessage.IsSenderTheActivePlayer && !itemSendLogMessage.IsReceiverTheActivePlayer)
                    {
                        string itemName = itemSendLogMessage.Item.ItemName;

                        string messageText;
                        string otherPlayer = _session.Players.GetPlayerAlias(itemSendLogMessage.Receiver.Slot);
                        messageText = $"Sent {itemName} to {otherPlayer}.";

                        SoundManager.PlaySoundEffect("gettreasure");
                        _messages.Enqueue(messageText);
                    }
                    break;
            }
        }

        public void SendDeath()
        {
            if (_deathLinkService != null)
            {
                string player = _session.Players.GetPlayerName(_session.ConnectionInfo.Slot);
                string reason = $"{player} {DeathHelper.GetDeathReason()}";

                if (_deathLinkReason != null)
                {
                    reason = $"{player} {_deathLinkReason}";
                }

                _deathLinkService.SendDeathLink(new DeathLink(player, reason));
            }
        }

        private void OnDeathLinkReceived(DeathLink deathLink)
        {
            _pendingDeathLink = deathLink;
        }

        public void EnableExtendedSwap()
        {
            GlobalState.events.SetEvent("ExtendedSwap", 1);

            if (GlobalState.Map != null)
            {
                // Refresh current map swap data.
                FieldInfo nameField = typeof(Map).GetField("mapName", BindingFlags.NonPublic | BindingFlags.Instance);
                string mapName = (string)nameField.GetValue(GlobalState.Map);

                FieldInfo swapperField = typeof(Map).GetField("swapper", BindingFlags.NonPublic | BindingFlags.Instance);
                SwapperControl swapper = new(mapName);
                swapperField.SetValue(GlobalState.Map, swapper);
            }
        }

        internal Stream? PatchFile(Stream stream, string path)
        {
            if(path.EndsWith("Entities.xml"))
            {
                EntityPatches patcher = new(stream);

                patcher.RemoveNexusBlockers();
                patcher.RemoveMitraCliff();

                patcher.SetColorPuzzle(ColorPuzzle);

                patcher.Set36CardRequirement((int)_endgameCardRequirement);

                if(UnlockSmallKeyGates)
                {
                    patcher.OpenSmallKeyGates();
                }

                if(BigKeyShuffle == BigKeyShuffle.Unlocked)
                {
                    patcher.OpenBigKeyGates();
                }

                foreach (long location_id in _session.Locations.AllLocations) {
                    string name = _session.Locations.GetLocationNameFromId(location_id);
                    if(name.EndsWith("Key") || name.EndsWith("Tentacle"))
                    {
                        patcher.SetFreeStanding(Locations.LocationsGuids[name], name);
                    }
                    else if(name.EndsWith("Cicada"))
                    {
                        patcher.SetCicada(Locations.LocationsGuids[name], name);
                    }
                    else if(name.EndsWith("Chest"))
                    {
                        patcher.SetTreasureChest(Locations.LocationsGuids[name], name);
                    }
                    else if(name == "Windmill - Activation")
                    {
                        patcher.SetWindmillCheck();
                    }
                }

                stream = patcher.Get();
            }
            return stream;
        }
    }
}
