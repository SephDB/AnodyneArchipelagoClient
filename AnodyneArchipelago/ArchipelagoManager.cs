using System.ComponentModel;
using System.Reflection;
using System.Text;
using AnodyneArchipelago.Helpers;
using AnodyneArchipelago.Patches;
using AnodyneSharp;
using AnodyneSharp.Dialogue;
using AnodyneSharp.Drawing;
using AnodyneSharp.Drawing.Effects;
using AnodyneSharp.Entities;
using AnodyneSharp.Entities.Enemy;
using AnodyneSharp.Entities.Gadget.Treasures;
using AnodyneSharp.Logging;
using AnodyneSharp.MapData;
using AnodyneSharp.MapData.Settings;
using AnodyneSharp.Registry;
using AnodyneSharp.Resources;
using AnodyneSharp.Sounds;
using AnodyneSharp.States;
using AnodyneSharp.UI;
using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.BounceFeatures.DeathLink;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Helpers;
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
        private const float ChaosTimerMax = 3f;
        private const float ExtremeChaosTimerMax = 5f;
        private const float GrayscaleTimerMax = 3f;
        private const float FlipTimerMax = 3f;

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
        private Dictionary<Guid, string> BigGateTypes = [];
        private RegionID[] _unlockedGates = [];
        private Dictionary<RegionID, Guid> _checkGates = [];
        private string? _playerSpriteName;
        private Texture2D? _originalPlayerTexture;
        private Texture2D? _originalCellTexture;
        private Texture2D? _originalReflectionTexture;

        private readonly Queue<string> _messages = new();
        private DeathLink? _pendingDeathLink = null;
        private Task<Dictionary<long, ScoutedItemInfo>>? _scoutTask;

        private ScreenChangeTracker screenTracker = new();

        private List<string> _phoneTraps = [];

        public APGrayScale grayScale = new();

        private float _chaosModeTimer = 0;
        private float _extremeChaosTimer = 0;
        private float _grayscaleTimer = 0;
        private float _flipTimer = 0;

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
        public SwapData? SwapData { get; private set; } = null;

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
                _session.DataStorage[Scope.Game, "roomSeed"].Initialize(RNG.Next(0, 1000000));
                _seedName += $"_{await _session.DataStorage[Scope.Game, "roomSeed"].GetAsync<int>():D6}";
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

            ApVersion = new(GetSlotData("version", "0.0.0", login).ToString());

            if (!ApVersion.IsNewer(0, 5, 0))
            {
                return new LoginFailure("AP World is generated with an older version.\n\nPlease downgrade the Client!");
            }

            IncludeBlueHappy = GetSlotData("include_blue_happy", false, login);

            foreach (var (key, (value, id)) in Locations.Gates)
            {
                BigGateTypes[id] = (string)login.SlotData.GetValueOrDefault(key, value);
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

            _unlockedGates = GetSlotDataArray<RegionID>("nexus_gates_unlocked", login);

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

            if (login.SlotData.ContainsKey("swap_areas"))
            {
                SwapData = new((JArray)login.SlotData["swap_areas"]);
            }

            (_originalPlayerTexture, _originalCellTexture, _originalReflectionTexture) = GetPlayerTextures();

            _scoutTask = Task.Run(ScoutAllLocations);

            _eventTracker = new(_session);

            if (!fullScreenEffects.Contains(grayScale))
            {
                fullScreenEffects.Add(grayScale);
            }

            LoadPhoneTrapMessages();

            return result;
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
            int map_id = (int)Enum.Parse<RegionID>(screenTracker.Tracker.mapName);
            _session.DataStorage[Scope.Slot, "MapIndex"] = map_id;
            _session.DataStorage[Scope.Slot, "MapLocation"] = JObject.FromObject(new { Map = map_id, screenTracker.Tracker.location.X, screenTracker.Tracker.location.Y });
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
                foreach (RegionID gate in _unlockedGates)
                {
                    events.ActivatedNexusPortals.Add(gate.ToString());
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

            fullScreenEffects.Remove(grayScale);
        }

        private async Task<Dictionary<long, ScoutedItemInfo>> ScoutAllLocations()
        {
            Dictionary<long, ScoutedItemInfo>? locationInfo = await _session!.Locations.ScoutLocationsAsync([.. _session.Locations.AllLocations]);

            return locationInfo;
        }

        public ScoutedItemInfo? GetScoutedLocation(long location_id)
        {
            if (_scoutTask == null || !_scoutTask.IsCompleted || !_scoutTask.Result.ContainsKey(location_id))
            {
                return null;
            }

            return _scoutTask.Result[location_id];
        }

        public string GetItemName(long id)
        {
            return _session!.Items.GetItemName(id) ?? "Something";
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

        public PlayerInfo? GetPlayerOfGame(string game, bool excludeCurrent = true)
        {
            PlayerInfo[] players = [.. _session!.Players.AllPlayers.Where(p => string.Equals(p.Game, game, StringComparison.InvariantCultureIgnoreCase))];

            if (excludeCurrent)
            {
                players = [.. players.Where(p => p.Slot != GetPlayer())];
            }

            if (!players.Any())
            {
                return null;
            }

            return players[RNG.Next(0, players.Length)];
        }

        public string GetLocationName(long locationId, string game)
        {
            return _session!.Locations.GetLocationNameFromId(locationId, game);
        }

        public string GetPlayerLocationName(long locationId, int player)
        {
            return GetLocationName(locationId, _session!.Players.GetPlayerInfo(player).Game);
        }

        public void SendHint(int player, long locationId)
        {
            _session!.Hints.CreateHints(player, HintStatus.Unspecified, locationId);
        }

        public bool IsChecked(long location)
        {
            return Checked.Contains(location);
        }

        public void SendLocation(long location)
        {
            if (_session == null)
            {
                //Plugin.Instance.Log.LogError("Attempted to send location while disconnected");
                return;
            }

            events.IncEvent($"ArchipelagoLoc-{location}");
            if (Checked.Add(location))
            {
                Task.Run(() => _session.Locations.CompleteLocationChecksAsync([.. Checked.Except(_session.Locations.AllLocationsChecked)])).ConfigureAwait(false);

                ScoutedItemInfo item = Plugin.ArchipelagoManager!.GetScoutedLocation(location)!;
                long itemId = item.ItemId;
                string itemName = GetItemName(itemId, GetPlayer());
                int player = item.Player;

                DebugLogger.AddInfo($"Sent {itemName} ({itemId}) found at {GetLocationName(location, GetGameName(player))} ({location}) to {GetPlayerName(player)} ({player}).");

                if (!item.IsReceiverRelatedToActivePlayer)
                {
                    BaseTreasure treasure = SpriteTreasure.Get(Plugin.Player.Position - new Vector2(4, 4), item.ItemId, item.Player, item.IsReceiverRelatedToActivePlayer);
                    SpawnEntity(treasure);
                    treasure.GetTreasure();
                }
            }
            else
            {
                _messages.Enqueue($"{_session.Locations.GetLocationNameFromId(location) ?? "This location"} was already checked.");
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
                else
                {
                    HandleTrapTimers();
                }
            }


            _eventTracker?.Update();
        }

        private void HandleTrapTimers()
        {
            if (!string.IsNullOrEmpty(Dialogue))
            {
                return;
            }

            //Extreme chaos overrides normal chaos
            if (_extremeChaosTimer > 0)
            {
                _extremeChaosTimer -= GameTimes.DeltaTime;

                if (_extremeChaosTimer <= 0)
                {
                    if (_chaosModeTimer > 0)
                    {
                        GlobalState.GameMode = AnodyneSharp.Registry.GameMode.Chaos;
                    }
                    else
                    {
                        GlobalState.GameMode = AnodyneSharp.Registry.GameMode.Normal;
                        ForceTextureReload = true;
                    }
                }
            }
            else if (_chaosModeTimer > 0)
            {
                _chaosModeTimer -= GameTimes.DeltaTime;

                if (_chaosModeTimer <= 0)
                {
                    GlobalState.GameMode = AnodyneSharp.Registry.GameMode.Normal;
                    ForceTextureReload = true;
                }
            }

            if (_grayscaleTimer > 0)
            {
                _grayscaleTimer -= GameTimes.DeltaTime;

                if (_grayscaleTimer <= 0)
                {
                    grayScale.Deactivate();
                }
            }
        }

        private IEnumerator<CutsceneEvent?> GetItemsAndMessages()
        {
            Queue<BaseTreasure>? treasures = new();

            while (ItemIndex < _session!.Items.Index)
            {
                IEnumerator<CutsceneEvent?> e = HandleItem(_session.Items.AllItemsReceived[ItemIndex]);
                events.IncEvent("ArchipelagoItemIndex");
                while (e.MoveNext())
                {
                    if (e.Current is EntityEvent ee)
                    {
                        treasures.Enqueue((BaseTreasure)ee.NewEntities.First());
                    }

                    yield return e.Current;
                }
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

        private IEnumerator<CutsceneEvent?> HandleItem(ItemInfo item)
        {
            BaseTreasure? treasure = null;

            long itemId = item.ItemId;
            string itemName = GetItemName(itemId, GetPlayer());
            long location = item.LocationId;
            int player = item.Player;

            Item itemInfo = Item.Create(item.ItemId);

            DebugLogger.AddInfo($"Recieved {itemName} ({itemId}) found at {GetLocationName(location, GetGameName(player))} ({location}) from {GetPlayerName(player)} ({player}).");

            bool handled = true;
            bool skipMessage = !item.Flags.HasFlag(ItemFlags.Advancement) || item.Flags.HasFlag(ItemFlags.Trap);

            switch (itemInfo.Type)
            {
                case ItemType.Keys when itemInfo.SubType == 0 && SmallkeyMode == SmallKeyMode.SmallKeys:
                    inventory.AddMapKey(itemInfo.Region.ToString(), 1);
                    break;
                case ItemType.Keys when itemInfo.SubType == 1 && SmallkeyMode == SmallKeyMode.KeyRings:
                    events.SetEvent($"{itemInfo.Region}_KeyRing_Obtained", 1);
                    inventory.AddMapKey(itemInfo.Region.ToString(), 9);
                    break;
                case ItemType.BigKey when (int)BigKeyShuffle >= 3:
                    inventory.BigKeyStatus[itemInfo.SubType] = true;
                    break;
                case ItemType.Cicada when !VanillaHealthCicadas:
                    MAX_HEALTH += 1;
                    CUR_HEALTH = MAX_HEALTH;
                    break;
                case ItemType.Heal:
                    CUR_HEALTH += itemInfo.SubType == 0 ? 1 : 3;
                    break;
                case ItemType.StatueUnlocks when SplitWindmill:
                    events.SetEvent($"StatueMoved_{itemInfo.Region}", 1);
                    break;
                case ItemType.RedCaveUnlock when !VanillaRedCave:
                    events.IncEvent("ProgressiveRedGrotto");

                    (char r, int amount) = events.GetEvent("ProgressiveRedGrotto") switch
                    {
                        1 => ('l', 1),
                        2 => ('r', 1),
                        _ => ('n', 2)
                    };

                    events.SetEvent($"red_cave_{r}_ss", amount);
                    break;
                case ItemType.Card:
                    treasure = new CardTreasure(Plugin.Player.Position, (int)itemInfo.SubType);
                    break;
                case ItemType.Nexus when _checkGates.ContainsKey(itemInfo.Region):
                    events.ActivatedNexusPortals.Add(itemInfo.Region.ToString());
                    break;
                case ItemType.Trap when itemInfo.SubType == 0:
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
                    break;
                case ItemType.Trap when itemInfo.SubType == 1:
                    if (!Plugin.Player.reversed)
                    {
                        Plugin.Player.reversed = true;
                        wave.active = true;
                    }
                    else
                    {
                        var field = typeof(Player).GetField("_revTimer", BindingFlags.NonPublic | BindingFlags.Instance)!;
                        field.SetValue(Plugin.Player, (float)field.GetValue(Plugin.Player)! - 0.9f);
                    }
                    break;
                case ItemType.Trap when itemInfo.SubType == 2:
                    _chaosModeTimer += ChaosTimerMax;
                    if (GlobalState.GameMode != AnodyneSharp.Registry.GameMode.Chaos)
                    {
                        GlobalState.GameMode = AnodyneSharp.Registry.GameMode.Chaos;
                        ForceTextureReload = true;
                    }

                    SoundManager.PlaySoundEffect("shieldy-hit");
                    SoundManager.PlaySoundEffect("small_wave");
                    SoundManager.PlaySoundEffect("big_door_locked");
                    break;
                case ItemType.Trap when itemInfo.SubType == 3:
                    _extremeChaosTimer += ExtremeChaosTimerMax;
                    if (GlobalState.GameMode != AnodyneSharp.Registry.GameMode.EXTREME_CHAOS)
                    {
                        GlobalState.GameMode = AnodyneSharp.Registry.GameMode.EXTREME_CHAOS;
                        ForceTextureReload = true;
                    }

                    SoundManager.PlaySoundEffect("shieldy-hit");
                    SoundManager.PlaySoundEffect("small_wave");
                    SoundManager.PlaySoundEffect("big_door_locked");
                    SoundManager.PlaySoundEffect("fall_in_hole");
                    break;
                case ItemType.Trap when itemInfo.SubType == 4:
                    _grayscaleTimer += GrayscaleTimerMax;
                    grayScale.active = true;
                    break;
                case ItemType.Trap when itemInfo.SubType == 5:
                    float timer = 0;

                    SoundManager.PlaySoundEffect("open");

                    while (timer < 2f)
                    {
                        timer += GameTimes.DeltaTime;
                        yield return null;
                    }

                    timer = 0;

                    SoundManager.PlaySoundEffect("open");

                    while (timer < 2f)
                    {
                        timer += GameTimes.DeltaTime;
                        yield return null;
                    }

                    timer = 0;

                    SoundManager.PlaySoundEffect("dash_pad_2");

                    while (timer < 0.2f)
                    {
                        timer += GameTimes.DeltaTime;
                        yield return null;
                    }

                    bool fastText = settings.fast_text;
                    settings.fast_text = false;

                    yield return new DialogueEvent(GetPhoneTrapMessage(player, location));

                    timer = 0;

                    SoundManager.PlaySoundEffect("dash_pad_1");

                    while (timer < 0.6f)
                    {
                        timer += GameTimes.DeltaTime;
                        yield return null;
                    }

                    timer = 0;

                    SoundManager.PlaySoundEffect("dustpoof");

                    while (timer < 0.8f)
                    {
                        timer += GameTimes.DeltaTime;
                        yield return null;
                    }

                    settings.fast_text = fastText;
                    yield break;
                case ItemType.Secret:
                    treasure = new SecretTreasure(Plugin.Player.Position, (int)itemInfo.SubType, -1);
                    skipMessage = false;
                    break;
                case ItemType.TradingQuest when itemInfo.SubType <= 2:
                    string name = itemInfo.SubType switch
                    {
                        0 => "Miao",
                        1 => "CardboardBox",
                        _ => "BikingShoes"
                    };
                    events.SetEvent($"Received{name}", 1);
                    break;
                case ItemType.Inventory when itemInfo.SubType <= 2:
                    EquipBroomIfEmpty(itemInfo.SubType switch
                    {
                        0 => BroomType.Normal,
                        1 => BroomType.Wide,
                        _ => BroomType.Long
                    });
                    break;
                case ItemType.Inventory when itemInfo.SubType == 3:
                    inventory.CanJump = true;
                    break;
                case ItemType.Inventory when itemInfo.SubType == 4 && PostgameMode != PostgameMode.Progression:
                    EquipBroomIfEmpty(BroomType.Transformer);
                    if ((events.GetEvent("DefeatedBriar") > 0 && PostgameMode != PostgameMode.Disabled) || PostgameMode == PostgameMode.Unlocked)
                    {
                        EnableExtendedSwap();
                    }
                    break;
                case ItemType.Inventory when itemInfo.SubType == 5 && PostgameMode == PostgameMode.Progression:
                    EquipBroomIfEmpty(BroomType.Transformer);

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
                    break;
                case ItemType.Dam when itemInfo.Region == RegionID.BLUE:
                    events.SetEvent("BlueDone", 1);
                    break;
                case ItemType.Dam when itemInfo.Region == RegionID.HAPPY:
                    events.SetEvent("HappyDone", 1);
                    break;
                default:
                    handled = false;
                    break;
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

            treasure ??= SpriteTreasure.Get(Plugin.Player.Position - new Vector2(4, 4), itemInfo.ID, GetPlayer(), true);

            treasure.GetTreasure();
            yield return new EntityEvent(Enumerable.Repeat(treasure, 1));

            if (!skipMessage)
            {
                yield return new DialogueEvent(message); //This pauses until dialogue is finished
            }

            yield break;
        }

        private static void EquipBroomIfEmpty(BroomType type)
        {
            switch (type)
            {
                case BroomType.Transformer:
                    inventory.HasTransformer = true;
                    break;
                case BroomType.Normal:
                    inventory.HasBroom = true;
                    break;
                case BroomType.Long:
                    inventory.HasLengthen = true;
                    break;
                case BroomType.Wide:
                    inventory.HasWiden = true;
                    break;
            }
            if (inventory.EquippedBroom == BroomType.NONE)
            {
                inventory.EquippedBroom = type;
            }
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
                _patches = new(stream);

                _patches.RemoveNexusBlockers();
                _patches.RemoveMitraCutscenes();
                _patches.FixHotelSoftlock();
                _patches.FixHappyNexusPad();
                _patches.LockMiao();

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
                    Location location = Location.Create(location_id);

                    switch (location.Type)
                    {
                        case LocationType.Dust:
                            _patches.SetDust(location);
                            break;
                        case LocationType.BigKey:
                            _patches.SetBigKey(location);
                            break;
                        case LocationType.Tentacle:
                            _patches.SetTentacle(location);
                            break;
                        case LocationType.Cicada:
                            _patches.SetCicada(location);
                            break;
                        case LocationType.Chest:
                            _patches.SetTreasureChest(location);
                            break;
                        case LocationType.Rock:
                            _patches.SetRockLocation(location);
                            break;
                        case LocationType.AreaEvent when location.Region == RegionID.WINDMILL && location.Index == 0:
                            _patches.SetWindmillCheck(location);
                            break;
                        case LocationType.Nexus:
                            {
                                Guid guid = _patches.SetNexusPad(location.Region, location_id);
                                _checkGates.Add(location.Region, guid); //Need to set them not alive when loading save for the first time
                                break;
                            }
                        case LocationType.AreaEvent when location.Region == RegionID.FIELDS:
                            switch (location.Index)
                            {
                                case 0:
                                    _patches.SetBoxTradeCheck(location_id);
                                    break;
                                case 1:
                                    _patches.SetShopkeepTradeCheck();
                                    break;
                                case 2:
                                    _patches.SetMitraTradeCheck();
                                    break;
                                default:
                                    DebugLogger.AddError($"Unknown Fields area event {location.Index}", false);
                                    break;
                            }
                            break;
                        case LocationType.AreaEvent when location.Region == RegionID.GO && location.Index == 0:
                            break;
                        case LocationType.AreaEvent when location.Region == RegionID.BLUE || location.Region == RegionID.HAPPY:
                            _patches.SetBlueHappyReward(location);
                            break;
                        default:
                            DebugLogger.AddError($"Missing location patch: {_session.Locations.GetLocationNameFromId(location_id) ?? location_id.ToString()}", false);
                            break;
                    }
                }

                stream = _patches.Get();
            }
            else if (path.EndsWith("Swapper.dat"))
            {
                stream?.Close();
                RegionID region = Enum.Parse<RegionID>(path.Split('.')[^3]);

                string? newContents = string.Join("\n",
                    SwapData!.GetRectanglesForMap(region, events.GetEvent("ExtendedSwap") == 1)
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
                SendLocation(new Location(RegionID.GO, LocationType.AreaEvent, 0).ID);
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

        private void LoadPhoneTrapMessages()
        {
            _phoneTraps = [];

            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = "AnodyneArchipelago.MonoGame.Content.PhoneTraps.txt";

            using Stream stream = assembly.GetManifestResourceStream(resourceName)!;
            using StreamReader reader = new(stream);

            while (!reader.EndOfStream)
            {
                string line = reader.ReadLine()!;

                if (!string.IsNullOrEmpty(line) && !line.StartsWith("//"))
                {
                    if (line.StartsWith('('))
                    {
                        string gameName = line.Split('(', ')')[1];

                        if (gameName == "MitraHint")
                        {
                            if (MitraHints.Length == 0)
                            {
                                continue;
                            }

                            var hint = MitraHints.Last();
                            string item = GetItemName(hint.itemID);
                            string location = GetPlayerLocationName(hint.locationID, hint.playerSlot);
                            string world = hint.playerSlot == GetPlayer() ? "this world" : $"{GetPlayerName(hint.playerSlot)}'s world";

                            line = line[(line.IndexOf(')') + 1)..].Replace("[item]", item).Replace("[location]", location).Replace("[world]", world);
                        }
                        else
                        {
                            PlayerInfo? player = GetPlayerOfGame(gameName);

                            if (player == null)
                            {
                                continue;
                            }

                            line = line[(line.IndexOf(')') + 1)..].Replace("[player]", player.Name);
                        }

                    }
                    _phoneTraps.Add(line);
                }
            }
        }

        private string GetPhoneTrapMessage(int slot, long location)
        {
            int staleCount = 5;
            List<int> lastStaleMessages = [];
            List<int> validMessages = [];

            for (int i = 0; i < staleCount; i++)
            {
                lastStaleMessages.Add(events.GetEvent($"PhoneTrap{i}") - 1);
            }

            for (int i = 0; i < _phoneTraps.Count; i++)
            {
                if (!lastStaleMessages.Contains(i))
                {
                    validMessages.Add(i);
                }
            }

            long rngVal = slot * location;

            //Cheat console sent item
            if (slot == 0)
            {
                rngVal += RNG.Next();
            }

            long seed = Util.StringToIntVal(Plugin.ArchipelagoManager!.GetSeed()) + rngVal;

            if (seed < 0)
            {
                seed *= -1;
            }

            int messageId = validMessages[(int)(seed % validMessages.Count)];

            for (int i = staleCount - 1; i > 0; i--)
            {
                events.SetEvent($"PhoneTrap{i}", events.GetEvent($"PhoneTrap{i - 1}"));
            }

            events.SetEvent($"PhoneTrap0", messageId + 1);

            //Mitra hint
            if (messageId == 0 && MitraHintType == MitraHintType.PreciseHint)
            {
                var hint = MitraHints.Last();
                SendHint(hint.playerSlot, hint.locationID);
            }

            return _phoneTraps[messageId];
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
