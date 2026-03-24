using AnodyneArchipelago.Effects;
using AnodyneSharp;
using AnodyneSharp.Entities;
using AnodyneSharp.Entities.Enemy;
using AnodyneSharp.Input;
using AnodyneSharp.Registry;
using AnodyneSharp.Sounds;
using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Helpers;
using Archipelago.MultiClient.Net.Packets;
using Microsoft.Xna.Framework;
using System.Net.Sockets;
using System.Reflection;
using static AnodyneSharp.Registry.GlobalState;
using static AnodyneSharp.States.CutsceneState;

namespace AnodyneArchipelago.Helpers
{
    public enum TrapType
    {
        Person = 0,
        Gas,
        Chaos,
        Extreme_Chaos,
        Grayscale,
        Phone,
        Pixelate,
        Stun,
        Damage,
        One_Hit_KO,
        Invisibile,
        FlipHorizontal,
        FlipVertical,
        Disarm,
        ToolSwap,
        Home
    }

    public class TrapHelper(ArchipelagoManager manager)
    {
        private const int TrapCountLimit = 5;

        private const float ChaosIncrease = 3f;
        private const float ExtremeChaosIncrease = 5f;
        private const float StunIncrease = 0.5f;
        private const float InvisibleIncrease = 1.5f;

        private float _chaosModeTimer = 0;
        private float _extremeChaosTimer = 0;
        private float _stunTimer = 0;
        private float _invisibleTimer = 0;

        private List<string> _phoneTraps = [];
        private TrapType[] _enabledTraps = [];

        private List<BaseTrapEffect> _effects = [];

        private FieldInfo? _gasTimer = typeof(Player).GetField("_revTimer", BindingFlags.NonPublic | BindingFlags.Instance)!;
        private FieldInfo? _playerInvincible = typeof(Player).GetField("invincible", BindingFlags.NonPublic | BindingFlags.Instance);
        private MethodInfo? _hurtMethod = typeof(Player).GetMethod("ReceiveDamage", BindingFlags.NonPublic | BindingFlags.Instance);
        private List<ArchipelagoPacketBase> _trapPackages = [];

        private List<(TrapType? trapType, string[] trapLinks)> _trapLinks = [
            (TrapType.Gas, [
                "Poison",
                "Reverse Controls",
                "Reverse",
                "Reversal",
                "Confuse",
                "Confusion",
                "Fuzzy",
                "Radiation"
                ]),
            (TrapType.Grayscale, [
                "Greyscale"
                ]),
            (TrapType.Stun, [
                "Paralize" ,
                "Paralysis",
                "Freeze" ,
                "Frozen" ,
                "Chaos Control" ,
                "Sleep",
                "Electrocutation"
                ]),
            (TrapType.Pixelate, [
                "Pixellation",
                "144p",
                "Fake Transition"
                ]),
            (TrapType.Phone, [
                "Text",
                "Omo",
                "Literature",
                "Cutscene",
                "Exposition"
                ]),
            (TrapType.Person, [
                "Army"
                ]),
            (TrapType.Damage, [
                "Explosion",
                "Poison Mushroom",
                "TNT Barrel",
                "TNT"
                ]),
            (TrapType.One_Hit_KO, [
                "Instant Death"
                ]),
            (TrapType.FlipHorizontal, [
                "Flip Horizontal",
                "Flip",
                "Screen Flip"
                ]),
            (TrapType.FlipVertical, [
                "Flip Vertical",
                "Mirror"
                ]),
            (TrapType.Disarm, [
                "Dry",
                "Eject Ability",
                "Empty Item Box",
                "Depletion",
                "Undo",
                "Whoops!",
                "Whoops"
                ]
            ),
            (TrapType.ToolSwap, [
                "Tool Swap",
                "Gadget Shuffle"
                ]
            ),
            (TrapType.Home, [
                "Teleport",
                "Get Out",
                "Clear Image"
                ])
        ];

        private string[] _randomTraps = [
            "Random Status"
            ];

        public void Connect(TrapType[] enabledTraps)
        {
            _enabledTraps = enabledTraps;

            LoadPhoneTrapMessages();

            _effects = [
                new APGrayScale(),
                new APPIxelate(),
                new ScreenFlip(false),
                new ScreenFlip(true),
                ];

            foreach (BaseTrapEffect effect in _effects)
            {
                effect.Initialize();
            }
        }

        public void Disconnect()
        {
            foreach (BaseTrapEffect effect in _effects)
            {
                effect.Remove();
            }
        }

        public void ReceiveTraplink(string player, string trapName)
        {
            TrapType? trapType;

            if (player == manager.GetCurrentPlayerName())
            {
                return;
            }

            if (Enum.TryParse(trapName, out TrapType t))
            {
                trapType = t;
            }
            else if (_randomTraps.Any(tl => tl.Equals(trapName, StringComparison.InvariantCultureIgnoreCase)))
            {
                trapType = _enabledTraps[RNG.Next(0, _enabledTraps.Length)];
            }
            else
            {
                trapType = _trapLinks.FirstOrDefault(t => t.trapLinks.Any(tl => tl.Equals(trapName, StringComparison.InvariantCultureIgnoreCase))).trapType;
            }

            if (trapType != null && _enabledTraps.Contains(trapType.Value))
            {
                SetSubstate(new APItemCutscene(HandleTrap(trapType.Value, manager.GetPlayerSlot(player))));

                if(trapType == TrapType.Phone)
                {
                    return;
                }

                Item item = new(ItemType.Trap, (int)trapType.Value);
                SpriteTreasure treasure = SpriteTreasure.Get(Plugin.Player.Position - new Vector2(4, 4), item.ID, manager.GetPlayer(), true, Archipelago.MultiClient.Net.Enums.ItemFlags.Trap);

                treasure.GetTreasure();
                SpawnEntity(treasure);
            }
        }

        public void Update(IArchipelagoSocketHelper socket)
        {
            if (KeyInput.JustPressedKey(Microsoft.Xna.Framework.Input.Keys.P))
            {
                SetSubstate(new APItemCutscene(HandleTrap(TrapType.Home, 0)));
            }

            HandleTrapTimers();

            SendTrapLinks(socket);
        }

        public void SendTrapLinks(IArchipelagoSocketHelper socket)
        {
            if (_trapPackages.Any())
            {
                socket.SendMultiplePackets(_trapPackages);
                _trapPackages.Clear();
            }
        }

        public IEnumerator<CutsceneEvent?> HandleTrap(TrapType trapType, int sender)
        {
            switch (trapType)
            {
                case TrapType.Person:
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
                case TrapType.Gas:
                    if (!Plugin.Player.reversed)
                    {
                        Plugin.Player.reversed = true;
                        wave.active = true;
                    }
                    else
                    {
                        _gasTimer?.SetValue(Plugin.Player, (float)_gasTimer.GetValue(Plugin.Player)! - 0.9f);
                    }
                    break;
                case TrapType.Chaos:
                    AddTimer(ref _chaosModeTimer, ChaosIncrease, TrapCountLimit);

                    if (GlobalState.GameMode != AnodyneSharp.Registry.GameMode.Chaos)
                    {
                        GlobalState.GameMode = AnodyneSharp.Registry.GameMode.Chaos;
                        ForceTextureReload = true;
                    }

                    SoundManager.PlaySoundEffect("shieldy-hit");
                    SoundManager.PlaySoundEffect("small_wave");
                    SoundManager.PlaySoundEffect("big_door_locked");
                    break;
                case TrapType.Extreme_Chaos:
                    AddTimer(ref _extremeChaosTimer, ExtremeChaosIncrease, TrapCountLimit);

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
                case TrapType.Stun:
                    AddTimer(ref _stunTimer, StunIncrease, TrapCountLimit);
                    break;
                case TrapType.Damage:
                    PlayerDamage(1, sender);
                    break;
                case TrapType.One_Hit_KO:
                    PlayerDamage(CUR_HEALTH - 1, sender);
                    break;
                case TrapType.Invisibile:
                    AddTimer(ref _invisibleTimer, InvisibleIncrease, TrapCountLimit);
                    Plugin.Player.visible = false;
                    break;
                case TrapType.Grayscale:
                    _effects.First(e => e is APGrayScale).AddTimer();
                    break;
                case TrapType.Pixelate:
                    _effects.First(e => e is APPIxelate).AddTimer();
                    break;
                case TrapType.FlipHorizontal:
                    _effects.First(e => e is ScreenFlip sf && sf.IsHorizontal).AddTimer();
                    break;
                case TrapType.FlipVertical:
                    _effects.First(e => e is ScreenFlip sf && !sf.IsHorizontal).AddTimer();
                    break;
                case TrapType.Disarm:
                    inventory.EquippedBroomChanged = true;
                    inventory._equippedBroom = BroomType.NONE;
                    SoundManager.PlaySoundEffect("menu_move");
                    break;
                case TrapType.ToolSwap:
                    if (!inventory.HasAnyBroom)
                    {
                        break;
                    }

                    List<BroomType> allowedTypes = [];

                    foreach (BroomType broomType in Enum.GetValues<BroomType>())
                    {
                        if (inventory.HasBroomType(broomType))
                        {
                            allowedTypes.Add(broomType);
                        }
                    }

                    allowedTypes.Remove(inventory._equippedBroom);

                    if (allowedTypes.Count == 0)
                    {
                        break;
                    }

                    inventory.EquippedBroomChanged = true;
                    inventory._equippedBroom = allowedTypes[RNG.Next(0, allowedTypes.Count)];
                    SoundManager.PlaySoundEffect("menu_move");
                    break;
                case TrapType.Home:
                    DoorMapPair gate = EntityManager.GetNexusGateForCurrentMap();
                    ReturnTarget = (gate != null) ? new(gate) : null;

                    ReturnTarget?.Warp(new Vector2(10, 34));
                    break;
                case TrapType.Phone:
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

                    yield return new DialogueEvent(GetPhoneTrapMessage(sender, 0));

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
                    break;
                default:
                    yield break;
            }
        }

        private void HandleTrapTimers()
        {
            if (!string.IsNullOrEmpty(Dialogue))
            {
                return;
            }

            foreach (BaseTrapEffect effect in _effects)
            {
                effect.Update();
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

            if (_stunTimer > 0)
            {
                _stunTimer -= GameTimes.DeltaTime;

                if (_stunTimer <= 0)
                {
                    Plugin.Player.dontMove = false;
                }
                else
                {
                    Plugin.Player.dontMove = true;
                }
            }

            if (_invisibleTimer > 0)
            {
                _invisibleTimer -= GameTimes.DeltaTime;

                if (_invisibleTimer <= 0)
                {
                    Plugin.Player.visible = true;
                }
                else
                {
                    Plugin.Player.visible = false;
                }
            }

            if (!Plugin.Player.reversed && (float)_gasTimer!.GetValue(Plugin.Player)! != 0)
            {
                Plugin.Player.reversed = true;
            }
        }

        public void AddTrapPackage(TrapType trapType)
        {
            BouncePacket package = new()
            {
                Tags = ["TrapLink"],
                Data = new()
                {
                    ["time"] = DateTime.Now.Ticks,
                    ["source"] = manager.GetCurrentPlayerName(),
                    ["trap_name"] = trapType.ToString() + " Trap"
                }
            };

            _trapPackages.Add(package);
        }

        private static void AddTimer(ref float timer, float addition, int countLimit)
        {
            timer = Math.Min(timer + addition, addition * countLimit);
        }

        private void PlayerDamage(int damage, int sender)
        {
            damage = CUR_HEALTH > 1 ? damage : 0;

            bool invincible = (bool)_playerInvincible?.GetValue(Plugin.Player)!;

            _playerInvincible?.SetValue(Plugin.Player, false);
            _hurtMethod?.Invoke(Plugin.Player, [damage, manager.GetPlayerName(sender), true, true]);
            _playerInvincible?.SetValue(Plugin.Player, invincible);
        }


        private void LoadPhoneTrapMessages()
        {
            _phoneTraps = [];

            Assembly assembly = Assembly.GetExecutingAssembly();
            string resourceName = "AnodyneArchipelago.MonoGame.Content.PhoneTraps.txt";

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
                            if (manager.MitraHints.Length == 0)
                            {
                                continue;
                            }

                            MitraHint hint = manager.MitraHints.Last();
                            string item = manager.GetItemName(hint.itemID);
                            string location = manager.GetPlayerLocationName(hint.locationID, hint.playerSlot);
                            string world = hint.playerSlot == manager.GetPlayer() ? "this world" : $"{manager.GetPlayerName(hint.playerSlot)}'s world";

                            line = line[(line.IndexOf(')') + 1)..].Replace("[item]", item).Replace("[location]", location).Replace("[world]", world);
                        }
                        else
                        {
                            PlayerInfo? player = manager.GetPlayerOfGame(gameName);

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
            if (messageId == 0 && manager.MitraHintType == MitraHintType.PreciseHint)
            {
                MitraHint hint = manager.MitraHints.Last();
                manager.SendHint(hint.playerSlot, hint.locationID);
            }

            return _phoneTraps[messageId];
        }
    }
}
