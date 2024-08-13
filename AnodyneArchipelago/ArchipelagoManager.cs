﻿using AnodyneArchipelago.Helpers;
using AnodyneArchipelago.Patches;
using AnodyneSharp.Entities;
using AnodyneSharp.Entities.Enemy;
using AnodyneSharp.Entities.Gadget.Treasures;
using AnodyneSharp.Logging;
using AnodyneSharp.MapData;
using AnodyneSharp.Registry;
using AnodyneSharp.Resources;
using AnodyneSharp.Sounds;
using AnodyneSharp.States;
using AnodyneSharp.UI;
using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.BounceFeatures.DeathLink;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.MessageLog.Messages;
using Archipelago.MultiClient.Net.Models;
using Archipelago.MultiClient.Net.Packets;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using static AnodyneSharp.States.CutsceneState;

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

    public enum PlayerSprite
    {
        Young = 0,
        JPlayer,
        Nova
    }

    public class ArchipelagoManager
    {
        private ArchipelagoSession _session;
        private static int ItemIndex
        {
            get => GlobalState.events.GetEvent("ArchipelagoItemIndex");
            set => GlobalState.events.SetEvent("ArchipelagoItemIndex", value);
        }
        private HashSet<long> Checked = new();
        private DeathLinkService _deathLinkService;

        private string _seedName;
        private long _endgameCardRequirement = 36;
        private ColorPuzzle _colorPuzzle = new();
        private bool ColorPuzzleRandomized = true;
        private bool _unlockSmallKeyGates = false;
        private BigKeyShuffle _bigKeyShuffle;
        private bool _vanillaHealthCicadas = false;
        private bool _vanillaRedCave = false;
        private bool _splitWindmill = false;
        private bool _forestBunnyChest = false;
        private VictoryCondition _victoryCondition;
        private List<string> _unlockedGates = new();
        private Dictionary<string, Guid> _checkGates = new();
        private PostgameMode _postgameMode;
        private PlayerSprite _playerSprite;

        private Texture2D _originalPlayerTexture;
        private Texture2D _originalCellTexture;
        private Texture2D _originalReflectionTexture;

        private readonly Queue<string> _messages = new();
        private DeathLink? _pendingDeathLink = null;
        private string? _deathLinkReason = null;
        private bool _receiveDeath = false;

        private Task<Dictionary<string, ScoutedItemInfo>> _scoutTask;

        private ScreenChangeTracker changeTracker = new();

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

        public string? DeathLinkReason
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
                _session.Locations.CheckedLocationsUpdated += NewCheckedLocations;

                RoomInfoPacket roomInfoPacket = await _session.ConnectAsync();
                _seedName = roomInfoPacket.SeedName;

                result = await _session.LoginAsync("Anodyne", slotName, ItemsHandlingFlags.AllItems, null, null, null, password == "" ? null : password);
            }
            catch (Exception e)
            {
                return new LoginFailure(e.GetBaseException().Message);
            }

            if (result is LoginFailure failure)
            {
                return failure;
            }

            LoginSuccessful login = (result as LoginSuccessful)!;

            _endgameCardRequirement = (long)login.SlotData.GetValueOrDefault("endgame_card_requirement", 36);

            if (login.SlotData.ContainsKey("seed"))
            {
                Random rand = new Random((int)(long)login.SlotData["seed"]);
                _colorPuzzle.Initialize(rand);
            }

            ColorPuzzleRandomized = (bool)login.SlotData.GetValueOrDefault("randomize_color_puzzle", true);

            _unlockSmallKeyGates = (bool)login.SlotData.GetValueOrDefault("unlock_gates", false);

            _bigKeyShuffle = (BigKeyShuffle)(long)login.SlotData.GetValueOrDefault("shuffle_big_gates", (long)BigKeyShuffle.AnyWorld);

            _vanillaHealthCicadas = (bool)login.SlotData.GetValueOrDefault("vanilla_health_cicadas", false);

            _vanillaRedCave = (bool)login.SlotData.GetValueOrDefault("vanilla_red_cave", false);

            _splitWindmill = (bool)login.SlotData.GetValueOrDefault("split_windmill", false);

            _forestBunnyChest = (bool)login.SlotData.GetValueOrDefault("forest_bunny_chest", false);

            _victoryCondition = (VictoryCondition)(long)login.SlotData.GetValueOrDefault("victory_condition", (long)VictoryCondition.DefeatBriar);

            _playerSprite = (PlayerSprite)(long)login.SlotData.GetValueOrDefault("player_sprite", (long)PlayerSprite.Young);

            if (login.SlotData.ContainsKey("nexus_gates_unlocked"))
            {
                _unlockedGates = new(((Newtonsoft.Json.Linq.JArray)login.SlotData["nexus_gates_unlocked"]).Values<string>());
            }
            else
            {
                _unlockedGates = new();
            }

            _postgameMode = (PostgameMode)(long)login.SlotData.GetValueOrDefault("postgame_mode", (long)PostgameMode.Disabled);

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

            (_originalPlayerTexture, _originalCellTexture, _originalReflectionTexture) = GetPlayerTextures();

            PatchPlayerTextures(_playerSprite);

            _scoutTask = Task.Run(ScoutAllLocations);

            return result;
        }

        public static string GetNexusGateMapName(string region)
        {
            region = string.Join("", region.Split(' ').TakeWhile(s => Char.IsUpper(s[0]))).ToUpperInvariant();

            return region switch
            {
                "TEMPLE" => "BEDROOM",
                "CLIFFS" => "CLIFF",
                "MOUNTAINCAVERN" => "CROWD",
                "DEEPFOREST" => "FOREST",
                "YOUNGTOWN" => "SUBURB",
                _ => region
            };
        }

        public static string GetTrackerMapName(string map)
        {
            return map switch
            {
                "BEDROOM" => "TEMPLE",
                "CROWD" => "CAVERN",
                "SUBURB" => "TOWN",
                _ => map
            };
        }

        private void SendTrackerUpdate()
        {
            _session.Socket.SendPacketAsync(new BouncePacket()
            {
                Slots = [_session.ConnectionInfo.Slot],
                Data = new()
                {
                    ["type"] = "MapUpdate",
                    ["mapName"] = GetTrackerMapName(changeTracker.Tracker.mapName),
                    ["mapIndex"] = changeTracker.Tracker.location.X + GlobalState.MAP_GRID_WIDTH * changeTracker.Tracker.location.Y
                }
            });
        }

        public void PostSaveloadInit(bool newGame)
        {
            if (newGame)
            {
                foreach (string gate in _unlockedGates)
                {
                    string mapName = GetNexusGateMapName(gate);
                    if (mapName.Length > 0)
                    {
                        GlobalState.events.ActivatedNexusPortals.Add(mapName);
                    }
                }
                foreach (Guid guid in _checkGates.Values)
                {
                    EntityManager.SetAlive(guid, false);
                }
            }
            // Pretend we're always in a pre-credits state so that swap is an allowlist, not a denylist.
            GlobalState.events.SetEvent("SeenCredits", 0);

            foreach (long location_id in _session.Locations.AllLocations)
            {
                if (GlobalState.events.GetEvent($"ArchipelagoLoc-{location_id}") != 0)
                {
                    Checked.Add(location_id);
                }
            }

            //Send locations that were missed last time we saved
            _session.Locations.CompleteLocationChecks(Checked.Except(_session.Locations.AllLocationsChecked).ToArray());

            Checked.UnionWith(_session.Locations.AllLocationsChecked);
        }

        private void NewCheckedLocations(System.Collections.ObjectModel.ReadOnlyCollection<long> newCheckedLocations)
        {
            Checked.UnionWith(newCheckedLocations);
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

            _session.Socket.DisconnectAsync();
            _session = null;

            PatchPlayerTextures(_originalPlayerTexture, _originalCellTexture, _originalReflectionTexture);
        }

        private async Task<Dictionary<string, ScoutedItemInfo>> ScoutAllLocations()
        {
            Dictionary<long, ScoutedItemInfo> locationInfo = await _session.Locations.ScoutLocationsAsync([.. _session.Locations.AllLocations]);

            Dictionary<string, ScoutedItemInfo> result = new();
            foreach (ScoutedItemInfo networkItem in locationInfo.Values)
            {
                string name = _session.Locations.GetLocationNameFromId(networkItem.LocationId, networkItem.LocationGame);
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
            long id = _session.Locations.GetLocationIdFromName("Anodyne", location);

            GlobalState.events.IncEvent($"ArchipelagoLoc-{id}");
            if (Checked.Add(id))
            {
                Task.Run(() => _session.Locations.CompleteLocationChecksAsync(Checked.Except(_session.Locations.AllLocationsChecked).ToArray())).ConfigureAwait(false);
            }
            else
            {
                _messages.Enqueue($"{location} was already checked.");
            }
        }

        public void Update()
        {
            if (_session == null)
            {
                // We're not connected.
                return;
            }

            if(changeTracker.Update() && !GlobalState.glitch.active && changeTracker.Tracker.mapName != "")
            {
                SendTrackerUpdate();
            }

            if (Plugin.ReadyToReceive())
            {
                if (_session.Items.Index > ItemIndex || _messages.Count > 0)
                {
                    GlobalState.SetSubstate(new APItemCutscene(GetItemsAndMessages()));
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

        private IEnumerator<CutsceneEvent> GetItemsAndMessages()
        {
            Queue<BaseTreasure> treasures = new();

            while (ItemIndex < _session.Items.Index)
            {
                var item = HandleItem(_session.Items.AllItemsReceived[ItemIndex]);
                GlobalState.events.IncEvent("ArchipelagoItemIndex");
                item.treasure.GetTreasure();
                treasures.Enqueue(item.treasure);
                yield return new EntityEvent(Enumerable.Repeat(item.treasure, 1));
                yield return new DialogueEvent(item.diag); //This pauses until dialogue is finished
            }

            while (_messages.TryDequeue(out string? message))
            {
                yield return new DialogueEvent(message);
            }

            //spawn treasures that aren't done yet into playstate
            while (treasures.TryDequeue(out BaseTreasure? t))
            {
                if (t.exists)
                {
                    GlobalState.SpawnEntity(t);
                }
            }

            yield break;
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

        private (BaseTreasure treasure, string diag) HandleItem(ItemInfo item)
        {
            BaseTreasure? treasure = null;

            string itemName = item.ItemName;

            bool handled = true;

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
                treasure = new CardTreasure(Plugin.Player.Position, cardIndex);
            }
            else if (itemName == "Cardboard Box")
            {
                GlobalState.events.SetEvent("ReceivedCardboardBox", 1);
            }
            else if (itemName == "Biking Shoes")
            {
                GlobalState.events.SetEvent("ReceivedBikingShoes", 1);
            }
            else if (itemName == "Person Trap")
            {
                int offset = 16;

                int min = -offset;
                int max = offset + 1;

                for (int i = 0; i < 6; i++)
                {
                    GlobalState.SpawnEntity(
                        new Person(
                            new EntityPreset(
                                typeof(Person),
                                Plugin.Player.Position + new Vector2(GlobalState.RNG.Next(min, max), GlobalState.RNG.Next(min, max)),
                                new Guid(),
                                GlobalState.RNG.Next(0, 5)
                                ),
                            Plugin.Player)
                        );
                }
            }
            else if (itemName == "Gas Trap")
            {
                Plugin.Player.reversed = true;
                GlobalState.wave.active = true;
            }
            else if (itemName.StartsWith("Nexus Gate"))
            {
                string mapname = GetNexusGateMapName(itemName[12..^1]);
                if (_checkGates.TryGetValue(mapname, out var gate))
                {
                    EntityManager.SetAlive(gate, true);
                    GlobalState.events.ActivatedNexusPortals.Add(mapname);
                }
                else
                {
                    DebugLogger.AddError($"Couldn't find nexus gate to unlock at {mapname}.", false);
                }
            }
            else if (TreasureHelper.GetSecretNumber(itemName) != -1)
            {
                treasure = new SecretTreasure(Plugin.Player.Position, TreasureHelper.GetSecretNumber(itemName), -1);
            }
            else
            {
                handled = false;
                DebugLogger.AddError($"Missing item handling: {itemName}!", false);
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

            if (!handled)
            {
                message += " But it didn't have any effect.";
            }

            treasure ??= SpriteTreasure.Get(Plugin.Player.Position - new Vector2(4, 4), itemName);

            return (treasure, message);
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
                    if (itemSendLogMessage is not HintItemSendLogMessage && itemSendLogMessage.IsSenderTheActivePlayer && !itemSendLogMessage.IsReceiverTheActivePlayer)
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
            if (path.EndsWith("Entities.xml"))
            {
                EntityPatches patcher = new(stream);

                patcher.RemoveNexusBlockers();
                patcher.RemoveMitraCliff();
                patcher.RemoveSageSoftlock();

                if (ColorPuzzleRandomized)
                {
                    patcher.SetColorPuzzle(ColorPuzzle);
                }

                patcher.Set36CardRequirement((int)_endgameCardRequirement);

                if (UnlockSmallKeyGates)
                {
                    patcher.OpenSmallKeyGates();
                }

                if (BigKeyShuffle == BigKeyShuffle.Unlocked)
                {
                    patcher.OpenBigKeyGates();
                }

                if (VictoryCondition == VictoryCondition.AllCards)
                {
                    patcher.SetAllCardsVictory();
                }

                foreach (long location_id in _session.Locations.AllLocations)
                {
                    string name = _session.Locations.GetLocationNameFromId(location_id);

                    if (name.EndsWith("Key") || name.EndsWith("Tentacle"))
                    {
                        patcher.SetFreeStanding(Locations.LocationsGuids[name], name, (int)location_id);
                    }
                    else if (name.EndsWith("Cicada"))
                    {
                        patcher.SetCicada(Locations.LocationsGuids[name], name);
                    }
                    else if (name.EndsWith("Chest"))
                    {
                        patcher.SetTreasureChest(Locations.LocationsGuids[name], name, (int)location_id);
                    }
                    else if (name == "Windmill - Activation")
                    {
                        patcher.SetWindmillCheck((int)location_id);
                    }
                    else if (name.EndsWith("Warp Pad"))
                    {
                        Guid guid = patcher.SetNexusPad(name, (int)location_id);
                        _checkGates.Add(GetNexusGateMapName(name), guid); //Need to set them not alive when loading save for the first time
                    }
                    else if (name.EndsWith("Cardboard Box"))
                    {
                        patcher.SetBoxTradeCheck((int)location_id);
                    }
                    else if (name.EndsWith("Shopkeeper Trade"))
                    {
                        patcher.SetShopkeepTradeCheck();
                    }
                    else if (name.EndsWith("Mitra Trade"))
                    {
                        patcher.SetMitraTradeCheck();
                    }
                    else if (name.EndsWith("Defeat Briar"))
                    {
                        //no-op since this is done at credits checking
                    }
                    else
                    {
                        DebugLogger.AddError($"Missing location patch: {name}", false);
                    }
                }

                stream = patcher.Get();
            }
            else if (path.EndsWith("Swapper.dat"))
            {
                stream?.Close();
                string newContents = string.Join("\n",
                    SwapData.GetRectanglesForMap(path.Split('.')[^3], GlobalState.events.GetEvent("ExtendedSwap") == 1)
                            .Select(r => $"Allow\t{r.X}\t{r.Y}\t{r.Width}\t{r.Height}"));
                stream = new MemoryStream(Encoding.Default.GetBytes(newContents));
            }
            else if (path.EndsWith("BG.csv"))
            {
                using StreamReader reader = new(stream);
                stream = new MemoryStream(Encoding.Default.GetBytes(MapPatches.ChangeMap(path.Split('.')[^3], reader.ReadToEnd())));
            }
            return stream;
        }

        public void OnCredits()
        {
            GlobalState.events.SetEvent("DefeatedBriar", 1);
            if (PostgameMode == PostgameMode.Vanilla)
            {
                EnableExtendedSwap();
            }

            if (VictoryCondition == VictoryCondition.DefeatBriar)
            {
                ActivateGoal();
            }
            else if (VictoryCondition == VictoryCondition.AllCards)
            {
                SendLocation("GO - Defeat Briar");
            }
        }

        public void OnDeath(DeathState deathState)
        {
            if (DeathLinkEnabled)
            {
                if (ReceivedDeath)
                {
                    string message = DeathLinkReason ?? "Received unknown death.";
                    message = Util.WordWrap(message, 20);

                    FieldInfo labelInfo = typeof(DeathState).GetField("_continueLabel", BindingFlags.NonPublic | BindingFlags.Instance);
                    UILabel label = (UILabel)labelInfo.GetValue(deathState);
                    label.SetText(message);
                    label.Position = new Vector2(8, 8);

                    ReceivedDeath = false;
                    DeathLinkReason = null;
                }
                else
                {
                    SendDeath();
                }
            }
        }

        private static void PatchPlayerTextures(PlayerSprite playerSprite)
        {
            string texture;

            switch (playerSprite)
            {
                case PlayerSprite.JPlayer:
                    texture = "jplayer";
                    break;
                case PlayerSprite.Nova:
                    texture = "nova";
                    break;
                default:
                    return;
            }

            PatchPlayerTextures(ResourceManager.GetTexture(texture), ResourceManager.GetTexture(texture + "_cell"), ResourceManager.GetTexture(texture + "reflection"));
        }

        private static void PatchPlayerTextures(Texture2D? texture, Texture2D? textureCell, Texture2D? textureReflection)
        {
            Dictionary<string, Texture2D> textures = (Dictionary<string, Texture2D>)typeof(ResourceManager).GetField("_textures", BindingFlags.NonPublic | BindingFlags.Static)!.GetValue(null)!;
            textures["young_player"] = texture ?? textures["young_player"];
            textures["young_player_cell"] = textureCell ?? textures["young_player_cell"];
            textures["young_player_reflection"] = textureReflection ?? textures["young_player_reflection"];
        }

        private static (Texture2D texture, Texture2D textureCell, Texture2D textureReflection) GetPlayerTextures()
        {
            Dictionary<string, Texture2D> textures = (Dictionary<string, Texture2D>)typeof(ResourceManager).GetField("_textures", BindingFlags.NonPublic | BindingFlags.Static)!.GetValue(null)!;
            return (textures["young_player"], textures["young_player_cell"], textures["young_player_reflection"]);
        }
    }
}
