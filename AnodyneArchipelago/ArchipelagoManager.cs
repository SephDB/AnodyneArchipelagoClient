using System;
using System.ComponentModel;
using System.Reflection;
using System.Text;
using AnodyneArchipelago.Helpers;
using AnodyneArchipelago.Patches;
using AnodyneSharp.Dialogue;
using AnodyneSharp.Entities;
using AnodyneSharp.Entities.Enemy;
using AnodyneSharp.Entities.Enemy.Circus;
using AnodyneSharp.Entities.Gadget;
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
using Newtonsoft.Json.Linq;
using static AnodyneSharp.Registry.GlobalState;
using static AnodyneSharp.States.CutsceneState;

namespace AnodyneArchipelago
{
    public enum SmallKeyMode
    {
        Unlocked = 0,
        SmallKeys = 1,
        KeyRings = 2
    }

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

    public enum MatchDifferentWorldItem
    {
        Disabled = 0,
        Match,
        MatchExtra
    }

    public enum MitraHintType
    {
        None = 0,
        Vague,
        Precise,
        PreciseHint
    }

    public class ArchipelagoManager
    {
        private ArchipelagoSession? _session;
        private static int ItemIndex
        {
            get => events.GetEvent("ArchipelagoItemIndex");
            set => events.SetEvent("ArchipelagoItemIndex", value);
        }
        private HashSet<long> Checked = [];
        private DeathLinkService? _deathLinkService;
        private EventTracker? _eventTracker;

        private EntityPatches? _patches;

        private string _seedName = "NULL";
        private long? _dustsanityBase = null;
        private Dictionary<Guid, string> BigGateTypes = [];
        private string[] _unlockedGates = [];
        private Dictionary<string, Guid> _checkGates = [];
        private string? _playerSpriteName;
        private Texture2D? _originalPlayerTexture;
        private Texture2D? _originalCellTexture;
        private Texture2D? _originalReflectionTexture;

        private readonly Queue<string> _messages = new();
        private DeathLink? _pendingDeathLink = null;
        private Task<Dictionary<string, ScoutedItemInfo>>? _scoutTask;

        private ScreenChangeTracker screenTracker = new();

        public ColorPuzzle ColorPuzzle { get; } = new();
        public bool UnlockSmallKeyGates => SmallkeyMode == SmallKeyMode.Unlocked;
        public SmallKeyMode SmallkeyMode { get; private set; } = SmallKeyMode.SmallKeys;
        public BigKeyShuffle BigKeyShuffle { get; private set; }
        public bool VanillaHealthCicadas { get; private set; } = false;
        public bool VanillaRedCave { get; private set; } = false;
        public bool SplitWindmill { get; private set; } = false;
        public bool ForestBunnyChest { get; private set; } = false;
        public MatchDifferentWorldItem MatchDifferentWorldItem { get; private set; }
        public bool HideTrapItems { get; private set; }
        public VictoryCondition VictoryCondition { get; private set; }
        public PostgameMode PostgameMode { get; private set; }

        public bool DeathLinkEnabled => _deathLinkService != null;

        public bool ColorPuzzleRandomized { get; private set; } = true;

        public bool ColorPuzzleHelp { get; private set; }

        public MitraHintType MitraHintType { get; private set; } = MitraHintType.None;

        public MitraHint[] MitraHints { get; private set; } = [];
        public ShopItem[] ShopItems { get; private set; } = [];

        public bool IncludeBlueHappy { get; private set; } = false;

        public bool ReceivedDeath { get; set; } = false;

        public string? DeathLinkReason { get; set; } = null;

        public Version ApVersion { get; private set; }

        public async Task<LoginResult> Connect(string url, string slotName, string password)
        {
            LoginResult? result;
            try
            {
                _session = ArchipelagoSessionFactory.CreateSession(url);
                _session.MessageLog.OnMessageReceived += OnMessageReceived;
                _session.Locations.CheckedLocationsUpdated += NewCheckedLocations;

                RoomInfoPacket? roomInfoPacket = await _session.ConnectAsync();
                _seedName = roomInfoPacket.SeedName;

                result = await _session.LoginAsync("Anodyne", slotName, ItemsHandlingFlags.AllItems, null, null, null, password == "" ? null : password);
            }
            catch (Exception e)
            {
                DebugLogger.AddException(e);

                return new LoginFailure(e.GetBaseException().Message);
            }

            if (result is LoginFailure failure)
            {
                return failure;
            }

            LoginSuccessful? login = (result as LoginSuccessful)!;

            foreach (var (key, (value, id)) in Locations.Gates)
            {
                BigGateTypes[id] = (string)login.SlotData.GetValueOrDefault(key, value);
            }

            if (login.SlotData.ContainsKey("endgame_card_requirement"))
            {
                BigGateTypes[Locations.Gates["terminal_endgame_gate"].guid] = $"cards_{(long)login.SlotData["endgame_card_requirement"]}";
            }


            if (login.SlotData.ContainsKey("seed"))
            {
                Random? rand = new((int)(long)login.SlotData["seed"]);
                ColorPuzzle.Initialize(rand);
            }

            ColorPuzzleRandomized = GetSlotData("randomize_color_puzzle", false, login);

            bool smallKeyGateUnlocked = (bool)login.SlotData.GetValueOrDefault("unlock_gates", false);

            SmallkeyMode = GetSlotData("small_key_mode", smallKeyGateUnlocked ? SmallKeyMode.Unlocked : SmallKeyMode.SmallKeys, login);

            BigKeyShuffle = GetSlotData("shuffle_big_gates", BigKeyShuffle.AnyWorld, login);

            VanillaHealthCicadas = GetSlotData("vanilla_health_cicadas", false, login);

            VanillaRedCave = GetSlotData("vanilla_red_cave", false, login);

            SplitWindmill = GetSlotData("split_windmill", false, login);

            ForestBunnyChest = GetSlotData("forest_bunny_chest", false, login);

            VictoryCondition = GetSlotData("victory_condition", VictoryCondition.DefeatBriar, login);

            _unlockedGates = GetSlotDataArray<string>("nexus_gates_unlocked", login);

            PostgameMode = GetSlotData("postgame_mode", PostgameMode.Disabled, login);

            if (GetSlotData("death_link", false, login))
            {
                DeathLinkReason = null;
                ReceivedDeath = false;

                _deathLinkService = _session.CreateDeathLinkService();
                _deathLinkService.OnDeathLinkReceived += OnDeathLinkReceived;
                _deathLinkService.EnableDeathLink();
            }
            else
            {
                _deathLinkService = null;
            }

            if (GetSlotData<long?>("dust_sanity_base", null, login) is long val)
            {
                _dustsanityBase = val;
            }

            MitraHintType = GetSlotData("mitra_hint_type", MitraHintType.None, login);

            if (MitraHintType != MitraHintType.None)
            {
                MitraHints = GetSlotDataArray<MitraHint>("mitra_hints", login);

                if (MitraHints.Length == 0)
                {
                    DebugLogger.AddWarning("Mitra hints are turned on, but no hints are found!");
                    MitraHintType = MitraHintType.None;
                }
            }

            if (login.SlotData.ContainsKey("shop_items"))
            {
                ShopItems = GetSlotDataArray<ShopItem>("shop_items", login);
            }

            IncludeBlueHappy = GetSlotData("include_blue_happy", false, login);

            ApVersion = new(GetSlotData("version", "0.0.0", login).ToString());

            (_originalPlayerTexture, _originalCellTexture, _originalReflectionTexture) = GetPlayerTextures();

            _scoutTask = Task.Run(ScoutAllLocations);

            _eventTracker = new(_session);

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
            _session!.Socket.SendPacketAsync(new BouncePacket()
            {
                Slots = [_session.ConnectionInfo.Slot],
                Data = new()
                {
                    ["type"] = "MapUpdate",
                    ["mapName"] = GetTrackerMapName(screenTracker.Tracker.mapName),
                    ["mapIndex"] = screenTracker.Tracker.location.X + MAP_GRID_WIDTH * screenTracker.Tracker.location.Y
                }
            });
        }

        public void PostSaveloadInit(bool newGame, string playerSprite, MatchDifferentWorldItem matchDifferentWorldItem, bool hideTrapItems, bool colorPuzzleHelp)
        {
            Checked.UnionWith(_session!.Locations.AllLocationsChecked);

            _playerSpriteName = playerSprite == "Random" ? RandomizeSprite() : playerSprite;
            MatchDifferentWorldItem = matchDifferentWorldItem;
            HideTrapItems = hideTrapItems;
            ColorPuzzleHelp = colorPuzzleHelp;

            PatchPlayerTextures(_playerSpriteName);

            if (newGame)
            {
                foreach (string gate in _unlockedGates)
                {
                    string? mapName = GetNexusGateMapName(gate);
                    if (mapName.Length > 0)
                    {
                        events.ActivatedNexusPortals.Add(mapName);
                    }
                }
                foreach (Guid guid in _checkGates.Values)
                {
                    EntityManager.SetAlive(guid, false);
                }

                //Shut up Sage
                foreach (Guid guid in _patches!.GetSages())
                {
                    EntityManager.SetActive(guid, true);
                }
                DialogueManager.GetDialogue("sage", "TERMINAL", "entrance"); //Sage in TERMINAL has its own logic, can be mostly shut up by setting this dialogue to dirty

                events.IncEvent("CheckpointTutorial"); //turn off the checkpoint tutorial
            }
            // Pretend we're always in a pre-credits state so that swap is an allowlist, not a denylist.
            events.SetEvent("SeenCredits", 0);

            foreach (long location_id in _session.Locations.AllLocations)
            {
                if (events.GetEvent($"ArchipelagoLoc-{location_id}") != 0)
                {
                    Checked.Add(location_id);
                }
            }

            //Send locations that were missed last time we saved
            _session.Locations.CompleteLocationChecks([.. Checked.Except(_session.Locations.AllLocationsChecked)]);
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
            Dictionary<long, ScoutedItemInfo>? locationInfo = await _session!.Locations.ScoutLocationsAsync([.. _session.Locations.AllLocations]);

            Dictionary<string, ScoutedItemInfo>? result = [];
            foreach (ScoutedItemInfo networkItem in locationInfo.Values)
            {
                string? name = _session.Locations.GetLocationNameFromId(networkItem.LocationId, networkItem.LocationGame);
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

        public string GetItemName(long id, int player)
        {
            return _session!.Items.GetItemName(id, GetGameName(player)) ?? "Something";
        }

        public string GetGameName(int player)
        {
            return _session!.Players.GetPlayerInfo(player).Game;
        }


        public string GetSeed()
        {
            return _seedName;
        }

        public int GetPlayer()
        {
            return _session!.ConnectionInfo.Slot;
        }

        public string GetPlayerName(int slot)
        {
            return _session!.Players.GetPlayerName(slot);
        }

        public string GetCurrentPlayerName()
        {
            return GetPlayerName(GetPlayer());
        }

        public string GetLocationName(long locationId, string game)
        {
            return _session!.Locations.GetLocationNameFromId(locationId, game);
        }

        public string GetPlayerLocationName(long locationId, int player)
        {
            return GetLocationName(locationId, _session!.Players.GetPlayerInfo(player).Game);
        }

        public async void SendHint(long locationId)
        {
            await _session!.Locations.ScoutLocationsAsync(true, locationId);
        }

        public bool IsChecked(long location)
        {
            return Checked.Contains(location);
        }

        public void SendLocation(string location)
        {
            if (_session == null)
            {
                //Plugin.Instance.Log.LogError("Attempted to send location while disconnected");
                return;
            }
            long id = _session.Locations.GetLocationIdFromName("Anodyne", location);

            events.IncEvent($"ArchipelagoLoc-{id}");
            if (Checked.Add(id))
            {
                Task.Run(() => _session.Locations.CompleteLocationChecksAsync([.. Checked.Except(_session.Locations.AllLocationsChecked)])).ConfigureAwait(false);
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

            if (screenTracker.Update() && !glitch.active && screenTracker.Tracker.mapName != "")
            {
                SendTrackerUpdate();
            }


            if (Plugin.ReadyToReceive())
            {
                if (_session.Items.Index > ItemIndex || _messages.Count > 0)
                {
                    SetSubstate(new APItemCutscene(GetItemsAndMessages()));
                }
                else if (_pendingDeathLink != null)
                {
                    CUR_HEALTH = 0;

                    string? message;
                    if (_pendingDeathLink.Cause == null)
                    {
                        message = $"Received death from {_pendingDeathLink.Source}.";
                    }
                    else
                    {
                        message = $"Received death. Cause: {_pendingDeathLink.Cause}";
                    }

                    _pendingDeathLink = null;
                    DeathLinkReason = message;
                    ReceivedDeath = true;
                }
            }

            _eventTracker?.Update();
        }

        private IEnumerator<CutsceneEvent> GetItemsAndMessages()
        {
            Queue<BaseTreasure>? treasures = new();

            while (ItemIndex < _session!.Items.Index)
            {
                var (treasure, diag) = HandleItem(_session.Items.AllItemsReceived[ItemIndex]);
                events.IncEvent("ArchipelagoItemIndex");
                treasure.GetTreasure();
                treasures.Enqueue(treasure);
                yield return new EntityEvent(Enumerable.Repeat(treasure, 1));
                yield return new DialogueEvent(diag); //This pauses until dialogue is finished
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
                    SpawnEntity(t);
                }
            }

            yield break;
        }

        public static string GetMapNameForDungeon(string dungeon)
        {
            return dungeon switch
            {
                "Temple of the Seeing One" or "Temple" => "BEDROOM",
                "Apartment" => "APARTMENT",
                "Mountain Cavern" or "Cavern" => "CROWD",
                "Hotel" => "HOTEL",
                "Red Cave" => "REDCAVE",
                "Circus" => "CIRCUS",
                _ => "STREET",
            };
        }

        private static int GetCardNumberForName(string name)
        {
            return name switch
            {
                "Edward" => 0,
                "Annoyer" => 1,
                "Seer" => 2,
                "Shieldy" => 3,
                "Slime" => 4,
                "PewLaser" => 5,
                "Suburbian" => 6,
                "Watcher" => 7,
                "Silverfish" => 8,
                "Gas Guy" => 9,
                "Mitra" => 10,
                "Miao" => 11,
                "Windmill" => 12,
                "Mushroom" => 13,
                "Dog" => 14,
                "Rock" => 15,
                "Fisherman" => 16,
                "Walker" => 17,
                "Mover" => 18,
                "Slasher" => 19,
                "Rogue" => 20,
                "Chaser" => 21,
                "Fire Pillar" => 22,
                "Contorts" => 23,
                "Lion" => 24,
                "Arthur and Javiera" => 25,
                "Frog" => 26,
                "Person" => 27,
                "Wall" => 28,
                "Blue Cube King" => 29,
                "Orange Cube King" => 30,
                "Dust Maid" => 31,
                "Dasher" => 32,
                "Burst Plant" => 33,
                "Manager" => 34,
                "Sage" => 35,
                "Young" => 36,
                "Carved Rock" => 37,
                "City Man" => 38,
                "Intra" => 39,
                "Torch" => 40,
                "Triangle NPC" => 41,
                "Killer" => 42,
                "Goldman" => 43,
                "Broom" => 44,
                "Rank" => 45,
                "Follower" => 46,
                "Rock Creature" => 47,
                "Null" => 48,
                _ => 0,
            };
        }

        private (BaseTreasure treasure, string diag) HandleItem(ItemInfo item)
        {
            BaseTreasure? treasure = null;

            string? itemName = item.ItemName;

            bool handled = true;

            if (itemName.StartsWith("Small Key"))
            {
                string? dungeonName = itemName[11..^1];

                string? mapName = GetMapNameForDungeon(dungeonName);
                inventory.AddMapKey(mapName, 1);
            }
            else if (itemName.StartsWith("Key Ring"))
            {
                string? dungeonName = itemName[10..^1];

                string? mapName = GetMapNameForDungeon(dungeonName);
                events.SetEvent($"{mapName}_KeyRing_Obtained", 1);
                inventory.AddMapKey(mapName, 9);
            }
            else if (itemName == "Green Key")
            {
                inventory.BigKeyStatus[0] = true;
            }
            else if (itemName == "Blue Key")
            {
                inventory.BigKeyStatus[2] = true;
            }
            else if (itemName == "Red Key")
            {
                inventory.BigKeyStatus[1] = true;
            }
            else if (itemName == "Jump Shoes")
            {
                inventory.CanJump = true;
            }
            else if (itemName == "Health Cicada")
            {
                MAX_HEALTH += 1;
                CUR_HEALTH = MAX_HEALTH;
            }
            else if (itemName == "Heal")
            {
                CUR_HEALTH += 1;
            }
            else if (itemName == "Big Heal")
            {
                CUR_HEALTH += 3;
            }
            else if (itemName == "Broom")
            {
                inventory.HasBroom = true;

                if (inventory.EquippedBroom == BroomType.NONE)
                {
                    inventory.EquippedBroom = BroomType.Normal;
                }
            }
            else if (itemName == "Swap")
            {
                inventory.HasTransformer = true;

                if (inventory.EquippedBroom == BroomType.NONE)
                {
                    inventory.EquippedBroom = BroomType.Transformer;
                }

                if ((PostgameMode == PostgameMode.Vanilla && events.GetEvent("DefeatedBriar") > 0) ||
                    PostgameMode == PostgameMode.Unlocked)
                {
                    EnableExtendedSwap();
                }
            }
            else if (itemName == "Progressive Swap")
            {
                inventory.HasTransformer = true;

                if (inventory.EquippedBroom == BroomType.NONE)
                {
                    inventory.EquippedBroom = BroomType.Transformer;
                }

                events.IncEvent("SwapStage");

                if (events.GetEvent("SwapStage") > 1)
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
                inventory.HasLengthen = true;

                if (inventory.EquippedBroom == BroomType.NONE)
                {
                    inventory.EquippedBroom = BroomType.Long;
                }
            }
            else if (itemName == "Widen")
            {
                inventory.HasWiden = true;

                if (inventory.EquippedBroom == BroomType.NONE)
                {
                    inventory.EquippedBroom = BroomType.Wide;
                }
            }
            else if (itemName == "Temple of the Seeing One Statue")
            {
                // TODO: This and the other two: move while on the same map.
                events.SetEvent("StatueMoved_Temple", 1);
            }
            else if (itemName == "Mountain Cavern Statue")
            {
                events.SetEvent("StatueMoved_Mountain", 1);
            }
            else if (itemName == "Red Cave Statue")
            {
                events.SetEvent("StatueMoved_Grotto", 1);
            }
            else if (itemName == "Progressive Red Cave")
            {
                events.IncEvent("ProgressiveRedGrotto");
                if (!VanillaRedCave)
                {
                    switch (events.GetEvent("ProgressiveRedGrotto"))
                    {
                        case 1:
                            events.SetEvent("red_cave_l_ss", 1);
                            break;
                        case 2:
                            events.SetEvent("red_cave_r_ss", 1);
                            break;
                        case 3:
                            events.SetEvent("red_cave_n_ss", 2);
                            break;
                    }
                }
            }
            else if (itemName.StartsWith("Card ("))
            {
                string? cardName = itemName[6..];
                cardName = cardName[..^1];

                int cardIndex = GetCardNumberForName(cardName);
                treasure = new CardTreasure(Plugin.Player.Position, cardIndex);
            }
            else if (itemName == "Cardboard Box")
            {
                events.SetEvent("ReceivedCardboardBox", 1);
            }
            else if (itemName == "Biking Shoes")
            {
                events.SetEvent("ReceivedBikingShoes", 1);
            }
            else if (itemName == "Person Trap")
            {
                int offset = 16;

                int min = -offset;
                int max = offset + 1;

                for (int? i = 0; i < 6; i++)
                {
                    SpawnEntity(
                        new Person(
                            new EntityPreset(
                                typeof(Person),
                                Plugin.Player.Position + new Vector2(RNG.Next(min, max), RNG.Next(min, max)),
                                new Guid(),
                                RNG.Next(0, 5)
                                ),
                            Plugin.Player)
                        );
                }
            }
            else if (itemName == "Gas Trap")
            {
                Plugin.Player.reversed = true;
                wave.active = true;
            }
            else if (itemName == "Miao")
            {
                events.SetEvent("ReceivedMiao", 1);
            }
            else if (itemName.StartsWith("Nexus Gate"))
            {
                string? mapname = GetNexusGateMapName(itemName[12..^1]);
                if (_checkGates.TryGetValue(mapname, out var gate))
                {
                    EntityManager.SetAlive(gate, true);
                    events.ActivatedNexusPortals.Add(mapname);
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

            string? message;

            if (item.Player == _session!.ConnectionInfo.Slot)
            {
                message = $"Found {itemName}!";
            }
            else
            {
                string? playerName = _session.Players.GetPlayerAlias(item.Player);
                message = $"Received {itemName} from {playerName}.";
            }

            if (!handled)
            {
                message += " But it didn't have any effect.";
            }

            treasure ??= SpriteTreasure.Get(Plugin.Player.Position - new Vector2(4, 4), itemName, GetPlayer());

            return (treasure, message);
        }

        public void ActivateGoal()
        {
            _session!.SetGoalAchieved();
        }

        private void OnMessageReceived(LogMessage message)
        {
            switch (message)
            {
                case ItemSendLogMessage itemSendLogMessage:
                    if (itemSendLogMessage is not HintItemSendLogMessage && itemSendLogMessage.IsSenderTheActivePlayer && !itemSendLogMessage.IsReceiverTheActivePlayer)
                    {
                        string? itemName = itemSendLogMessage.Item.ItemName;

                        string? messageText;
                        string? otherPlayer = _session!.Players.GetPlayerAlias(itemSendLogMessage.Receiver.Slot);
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
                string? player = _session!.Players.GetPlayerName(_session.ConnectionInfo.Slot);
                string? reason = $"{player} {DeathHelper.GetDeathReason()}";

                if (DeathLinkReason != null)
                {
                    reason = $"{player} {DeathLinkReason}";
                }

                _deathLinkService.SendDeathLink(new DeathLink(player, reason));
            }
        }

        private void OnDeathLinkReceived(DeathLink deathLink)
        {
            _pendingDeathLink = deathLink;
        }

        public static void EnableExtendedSwap()
        {
            events.SetEvent("ExtendedSwap", 1);

            if (GlobalState.Map != null)
            {
                // Refresh current map swap data.
                FieldInfo? nameField = typeof(Map).GetField("mapName", BindingFlags.NonPublic | BindingFlags.Instance);
                string mapName = (string)nameField!.GetValue(GlobalState.Map)!;

                FieldInfo? swapperField = typeof(Map).GetField("swapper", BindingFlags.NonPublic | BindingFlags.Instance);
                SwapperControl? swapper = new(mapName);
                swapperField!.SetValue(GlobalState.Map, swapper);
            }
        }

        internal Stream? PatchFile(Stream stream, string path)
        {
            if (path.EndsWith("Entities.xml"))
            {
                _patches = new(stream, _dustsanityBase);

                _patches.RemoveNexusBlockers();
                _patches.RemoveMitraCutscenes();
                _patches.FixHotelSoftlock();
                _patches.FixHappyNexusPad();

                //0.3.0 is the earliest version with version checks
                if (ApVersion.IsNewer(0, 3, 0))
                {
                    _patches.LockMiao();
                }

                if (ColorPuzzleRandomized)
                {
                    _patches.SetColorPuzzle(ColorPuzzle);
                }

                if (!ForestBunnyChest)
                {
                    _patches.ForestChestJoke();
                }

                foreach (var (id, value) in BigGateTypes)
                {
                    _patches.SetBigGateReq(id, value);
                }

                if (UnlockSmallKeyGates)
                {
                    _patches.OpenSmallKeyGates();
                }
                else if (SmallkeyMode == SmallKeyMode.KeyRings)
                {
                    _patches.MakeKeyRingGates();
                }

                if (BigKeyShuffle == BigKeyShuffle.Unlocked)
                {
                    _patches.OpenBigKeyGates();
                }

                if (VictoryCondition == VictoryCondition.AllCards)
                {
                    _patches.SetAllCardsVictory();
                }

                if (IncludeBlueHappy)
                {
                    _patches.PatchHappyAndBlue();
                }

                foreach (long location_id in _session!.Locations.AllLocations)
                {
                    string? name = _session.Locations.GetLocationNameFromId(location_id);

                    if (name.EndsWith("Completion Reward"))
                    {
                        //New happy and blue locations crash currently!
                        continue;
                    }

                    if (_patches.IsDustID(location_id))
                    {
                        _patches.SetDust(location_id, name);
                    }
                    else if (name.EndsWith("Key") || name.EndsWith("Tentacle"))
                    {
                        _patches.SetFreeStanding(Locations.LocationsGuids[name], name, (int)location_id);
                    }
                    else if (name.EndsWith("Cicada"))
                    {
                        _patches.SetCicada(Locations.LocationsGuids[name], name);
                    }
                    else if (name.EndsWith("Chest"))
                    {
                        _patches.SetTreasureChest(Locations.LocationsGuids[name], name, (int)location_id);
                    }
                    else if (name == "Windmill - Activation")
                    {
                        _patches.SetWindmillCheck((int)location_id);
                    }
                    else if (name.EndsWith("Warp Pad"))
                    {
                        Guid guid = _patches.SetNexusPad(name, (int)location_id);
                        _checkGates.Add(GetNexusGateMapName(name), guid); //Need to set them not alive when loading save for the first time
                    }
                    else if (name.EndsWith("Cardboard Box"))
                    {
                        _patches.SetBoxTradeCheck((int)location_id);
                    }
                    else if (name.EndsWith("Shopkeeper Trade"))
                    {
                        _patches.SetShopkeepTradeCheck();
                    }
                    else if (name.EndsWith("Mitra Trade"))
                    {
                        _patches.SetMitraTradeCheck();
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

                stream = _patches.Get();
            }
            else if (path.EndsWith("Swapper.dat"))
            {
                stream?.Close();
                string? newContents = string.Join("\n",
                    SwapData.GetRectanglesForMap(path.Split('.')[^3], events.GetEvent("ExtendedSwap") == 1)
                            .Select(r => $"Allow\t{r.X}\t{r.Y}\t{r.Width}\t{r.Height}"));
                stream = new MemoryStream(Encoding.Default.GetBytes(newContents));
            }
            else if (path.EndsWith("BG.csv"))
            {
                using StreamReader? reader = new(stream);
                stream = new MemoryStream(Encoding.Default.GetBytes(MapPatches.ChangeMap(path.Split('.')[^3], reader.ReadToEnd())));
            }
            return stream;
        }

        public void OnCredits()
        {
            events.SetEvent("DefeatedBriar", 1);
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
                    string? message = DeathLinkReason ?? "Received unknown death.";
                    message = Util.WordWrap(message, 20);

                    FieldInfo? labelInfo = typeof(DeathState).GetField("_continueLabel", BindingFlags.NonPublic | BindingFlags.Instance);
                    UILabel label = (UILabel)labelInfo!.GetValue(deathState)!;
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

        private string RandomizeSprite()
        {
            Dictionary<string, Texture2D>? textures = (Dictionary<string, Texture2D>)typeof(ResourceManager).GetField("_textures", BindingFlags.NonPublic | BindingFlags.Static)!.GetValue(null)!;

            List<string>? sprites = [];

            int seed = Util.StringToIntVal(GetSeed()) + Util.StringToIntVal(GetCurrentPlayerName());

            if (seed < 0)
            {
                seed *= -1;
            }

            foreach (var sprite in textures.Keys)
            {
                if (sprite.EndsWith("_cell") && !sprite.StartsWith("broom"))
                {
                    sprites.Add(sprite.Replace("_cell", ""));
                }
            }

            return sprites[seed % sprites.Count];
        }

        private static void PatchPlayerTextures(string playerSpriteName)
        {
            PatchPlayerTextures(ResourceManager.GetTexture(playerSpriteName), ResourceManager.GetTexture(playerSpriteName + "_cell"), ResourceManager.GetTexture(playerSpriteName + "_reflection"));
        }

        private static void PatchPlayerTextures(Texture2D? texture, Texture2D? textureCell, Texture2D? textureReflection)
        {
            Dictionary<string, Texture2D>? textures = (Dictionary<string, Texture2D>)typeof(ResourceManager).GetField("_textures", BindingFlags.NonPublic | BindingFlags.Static)!.GetValue(null)!;
            textures["young_player"] = texture ?? textures["young_player"];
            textures["young_player_cell"] = textureCell ?? textures["young_player_cell"];
            textures["young_player_reflection"] = textureReflection ?? textures["young_player_reflection"];
        }

        private static (Texture2D texture, Texture2D textureCell, Texture2D textureReflection) GetPlayerTextures()
        {
            Dictionary<string, Texture2D>? textures = (Dictionary<string, Texture2D>)typeof(ResourceManager).GetField("_textures", BindingFlags.NonPublic | BindingFlags.Static)!.GetValue(null)!;
            return (textures["young_player"], textures["young_player_cell"], textures["young_player_reflection"]);
        }

        private static T GetSlotData<T>(string valueName, T nullResult, LoginSuccessful login)
        {
            if (!login.SlotData.ContainsKey(valueName))
            {
                DebugLogger.AddWarning($"SlotData is missing '{valueName}'. Defaulting to '{nullResult}'.");
                return nullResult;
            }

            try
            {
                if (typeof(T).IsEnum)
                {
                    var converter = TypeDescriptor.GetConverter(typeof(T));

                    return (T)converter.ConvertFromString(login.SlotData.GetValueOrDefault(valueName, (int)(object)nullResult!)!.ToString()!)!;
                }

                return (T)login.SlotData.GetValueOrDefault(valueName, nullResult);
            }
            catch (Exception ex)
            {
                DebugLogger.AddError($"SlotData: found unexpected type for {valueName}", showStack: false);
                DebugLogger.AddException(ex);
                return nullResult;
            }
        }

        private static T[] GetSlotDataArray<T>(string valueName, LoginSuccessful login)
        {
            if (!login.SlotData.ContainsKey(valueName))
            {
                DebugLogger.AddWarning($"SlotData is missing '{valueName}'. Defaulting to empty list.");
                return [];
            }

            return ((JArray)login.SlotData[valueName]).ToObject<T[]>()!;
        }
    }
}
