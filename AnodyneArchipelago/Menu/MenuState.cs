using AnodyneArchipelago.Menu.MenuSubstate;
using AnodyneSharp.Entities;
using AnodyneSharp.Input;
using AnodyneSharp.Registry;
using AnodyneSharp.Sounds;
using AnodyneSharp.States;
using AnodyneSharp.States.MenuSubstates;
using AnodyneSharp.States.MenuSubstates.ConfigSubstates;
using AnodyneSharp.UI;
using AnodyneSharp.UI.PauseMenu;
using AnodyneSharp.UI.PauseMenu.Config;
using Microsoft.Xna.Framework;
using Newtonsoft.Json.Linq;

namespace AnodyneArchipelago.Menu
{
    internal partial class MenuState : ListSubstate
    {
        public static ArchipelagoSettings? ArchipelagoSettings;

        private UILabel? _versionLabel1;
        private UILabel? _versionLabel2;
        private UILabel? _serverValue;
        private UILabel? _portValue;
        private UILabel? _slotValue;
        private UILabel? _passwordValue;
        private TextSelector? _connectionSwitcher;

        private State? _substate = null;
        private bool _hide = false;

        private string _apServer = "";
        private string _apPort = "";
        private string _apSlot = "";
        private string _apPassword = "";

        private int _curPage;

        private bool _fadingOut = false;
        protected bool _isNewGame;

        public MenuState()
        {
            if (Plugin.ArchipelagoManager != null)
            {
                Plugin.ArchipelagoManager.Disconnect();
                Plugin.ArchipelagoManager = null;
            }

            ArchipelagoSettings = ArchipelagoSettings.Load();
            if (ArchipelagoSettings == null)
            {
                ArchipelagoSettings = new();
            }

            string[] selectorValues = new string[ArchipelagoSettings.ConnectionDetails.Count + 1];
            for (int i = 0; i < selectorValues.Length; i++)
            {
                selectorValues[i] = $"{i + 1}/{selectorValues.Length}";
            }

            _connectionSwitcher = new(new Vector2(60f, 115f), 32f, 0, true, selectorValues);
            _connectionSwitcher.noConfirm = true;
            _connectionSwitcher.noLoop = true;
            _connectionSwitcher.ValueChangedEvent = PageValueChanged;

            SetLabels();

            SetPage(ArchipelagoSettings.ConnectionDetails.Count == 0 ? 0 : 1);
        }

        protected override void SetLabels()
        {
            float x = 45 + 8;

            _versionLabel1 = new(new Vector2(10f, 7f), false, "AnodyneArchipelago", new Color(116, 140, 144));
            _versionLabel2 = new(new Vector2(10f, 15f), false, $"v{Plugin.Version}", new Color(116, 140, 144));

            UILabel _serverLabel = new(new Vector2(x, 31f), false, $"Server:", new Color(226, 226, 226));
            _serverValue = new(new Vector2(x + 8, 40f), false, "", new Color());

            UILabel _portLabel = new(new Vector2(x, 51f), false, $"Port:", new Color(226, 226, 226));
            _portValue = new(new Vector2(x + 8, 60f), false, "", new Color());

            UILabel _slotLabel = new(new Vector2(x, 71f), false, $"Slot:", new Color(226, 226, 226));
            _slotValue = new(new Vector2(x + 8, 80f), false, "", new Color());

            UILabel _passwordLabel = new(new Vector2(x, 91f), false, $"Password:", new Color(226, 226, 226));
            _passwordValue = new(new Vector2(x + 8, 100f), false, "", new Color());

            UILabel _settingsLabel = new(new Vector2(60f, 135f), false, $"Settings", new Color(116, 140, 144));
            UILabel _connectLabel = new(new Vector2(60f, 148f), false, $"Connect", new Color(116, 140, 144));
            UILabel _pageLabel = new(new Vector2(60f, 115f), false, "");

            options =
            [
                (_serverLabel, new TextEntry("Server:", () => _apServer, value => {
                    Uri url = GetUri(value.Trim());

                    _apServer = url.Host;

                    if(!url.IsDefaultPort) {
                        _apPort = url.Port.ToString();
                    }

                    UpdateLabels(); 
                })),
                (_portLabel, new TextEntry("Port:", () => _apPort, value => {
                    if(int.TryParse(value, out int port))
                    {
                        _apPort = value.Trim();
                        UpdateLabels();
                    }
                })),
                (_slotLabel, new TextEntry("Slot:", () => _apSlot, value => { _apSlot = value.Trim();; UpdateLabels(); })),
                (_passwordLabel, new TextEntry("Password:", () => _apPassword, value => {_apPassword = value.Trim(); UpdateLabels(); })),
                (_pageLabel, new ActionOption(()=>{ })),
                (_settingsLabel, new SubstateOption<ArchipelagoLocalSettings>()),
                (_connectLabel, new ActionOption(()=>{ _substate = new ConnectionState(GetHost(),_apSlot,_apPassword,OnConnected); }))
            ];
        }

        public override void Update()
        {

            if (_fadingOut)
            {
                GlobalState.black_overlay.ChangeAlpha(0.72f);

                if (GlobalState.black_overlay.alpha == 1.0)
                {
                    ChangeState();
                }

                return;
            }


            if (_substate != null)
            {
                _substate.Update();

                if (_substate.Exit)
                {
                    _substate = null;
                    _hide = false;
                }

                return;
            }

            base.Update();
        }

        public override void HandleInput()
        {
            if (_substate != null)
                return;

            int _oldstate = state;

            base.HandleInput();

            if (state == 4)
            {
                selector.visible = false;
                _connectionSwitcher?.GetControl();
                _connectionSwitcher?.Update();
            }
            else
            {
                if (_oldstate == 4)
                    selector.visible = true;
                _connectionSwitcher?.LoseControl();
            }

            BrowseInput();
        }

        public override void DrawUI()
        {
            base.DrawUI();
            if (!_hide)
            {
                _versionLabel1?.Draw();
                _versionLabel2?.Draw();
                _serverValue?.Draw();
                _portValue?.Draw();
                _slotValue?.Draw();
                _passwordValue?.Draw();
                _connectionSwitcher?.Draw();
            }

            _substate?.DrawUI();
        }

        private void UpdateLabels()
        {
            UpdateLabel(_serverValue, _apServer);
            UpdateLabel(_portValue, _apPort);
            UpdateLabel(_slotValue, _apSlot);
            UpdateLabel(_passwordValue, _apPassword);
        }

        private void UpdateLabel(UILabel? label, string text)
        {
            if (label == null)
            {
                return;
            }

            if (text.Length == 0)
            {
                label.SetText("[empty]");
                label.Color = new Color(116, 140, 144);
            }
            else
            {
                if (text.Length > 15)
                {
                    label.SetText(text.Substring(0, 13) + "..");
                }
                else
                {
                    label.SetText(text);
                }

                label.Color = new Color(184, 32, 0);
            }
        }

        private void BrowseInput()
        {
            if (state < 3 && ((TextEntry)options[state].option).Active)
            {
                return;
            }

            if (KeyInput.JustPressedRebindableKey(KeyFunctions.Left))
            {
                if (state < 3 && _curPage > 0)
                {
                    SoundManager.PlaySoundEffect("menu_move");

                    SetPage(_curPage - 1);
                }
            }
            else if (KeyInput.JustPressedRebindableKey(KeyFunctions.Right))
            {
                if (state < 3 && _curPage < ArchipelagoSettings!.ConnectionDetails.Count)
                {
                    SoundManager.PlaySoundEffect("menu_move");

                    SetPage(_curPage + 1);
                }
            }
        }

        private void SetPage(int index)
        {
            _curPage = index;

            if (index == 0)
            {
                _apServer = "";
                _apPort = "";
                _apSlot = "";
                _apPassword = "";
            }
            else
            {
                ConnectionDetails details = ArchipelagoSettings!.ConnectionDetails[index - 1];

                Uri url = GetUri(details.ApServer);

                _apServer = url.Host.Trim();
                _apPort = url.Port.ToString();
                _apSlot = details.ApSlot.Trim();
                _apPassword = details.ApPassword.Trim();
            }

            _connectionSwitcher?.SetValue(index);
            UpdateLabels();
        }

        private void PageValueChanged(string value, int index)
        {
            SetPage(index);
        }

        private void OnConnected(ArchipelagoManager archipelagoManager)
        {
            ArchipelagoSettings!.AddConnection(new(GetHost(), _apSlot, _apPassword));
            ArchipelagoSettings.Save();

            Plugin.ArchipelagoManager = archipelagoManager;

            GlobalState.CurrentSaveGame = $"zzAP{archipelagoManager.GetSeed()}_{archipelagoManager.GetPlayer()}";

            GlobalState.Save? saveFile = GlobalState.Save.GetSave(GlobalState.Save.PathFromId(GlobalState.CurrentSaveGame));

            GlobalState.ResetValues();

            EntityManager.Initialize(); //reload Entities.xml

            if (saveFile != null)
            {
                GlobalState.LoadSave(saveFile);
                _isNewGame = false;
            }
            else
            {
                _isNewGame = true;
            }

            _fadingOut = true;

            archipelagoManager.PostSaveloadInit(
                _isNewGame,
                ArchipelagoSettings.PlayerSprite,
                ArchipelagoSettings.MatchDifferentWorldItem,
                ArchipelagoSettings.HideTrapItems,
                ArchipelagoSettings.ColorPuzzleHelp);
        }
        private Uri GetUri(string url)
        {
            if (url.StartsWith("ws://") || url.StartsWith("wss://") || url.StartsWith("http://") || url.StartsWith("https://"))
            {
                return new Uri(url);
            }
            else
            {
                return new Uri("ws://"+ url);
            }
        }


        private string GetHost()
        {
            UriBuilder b = new(_apServer)
            {
                Port = int.TryParse(_apPort, out int port) ? port : -1
            };

            return b.Uri.Authority;
        }
    }
}
