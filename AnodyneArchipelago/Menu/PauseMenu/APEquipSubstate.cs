using AnodyneArchipelago;
using AnodyneArchipelago.Helpers;
using AnodyneSharp.Dialogue;
using AnodyneSharp.Entities;
using AnodyneSharp.Input;
using AnodyneSharp.Registry;
using AnodyneSharp.Sounds;
using AnodyneSharp.UI;
using AnodyneSharp.UI.PauseMenu;
using Microsoft.Xna.Framework;

namespace AnodyneSharp.States.MenuSubstates
{
    public class APEquipSubstate : DialogueSubstate
    {
        private enum EquipState
        {
            Broom,
            Extend,
            Widen,
            Transformer,
            Shoes,
            Box,
            BikingShoes,
            Miao,
            Happy,
            Blue,
            RedCave,
            Statue1,
            Statue2,
            Statue3
        }

        private Equipment _broom;
        private Equipment _broomExtend;
        private Equipment _broomWiden;
        private Equipment _transformer;

        private UIEntity _transformerUnlocked;

        private UIEntity _jump;
        private UIEntity _boxItem;
        private UIEntity _shoesItem;
        private UIEntity _redCave;
        private UIEntity _miao;
        private UIEntity[] _dams;
        private UIEntity[] _statues;

        private UILabel _redCaveLabel;

        private bool[] statuesMoved = [];
        private bool splitWindmill = false;

        private EquipState _state;
        private EquipState _lastState;

        List<EquipState> bottom_row_enabled = [EquipState.Shoes, EquipState.Box, EquipState.BikingShoes, EquipState.Miao, EquipState.Happy, EquipState.Blue];
        List<EquipState> very_bottom_row_enabled = [EquipState.RedCave, EquipState.Statue1, EquipState.Statue2, EquipState.Statue3];
        int current_bottom_index = 0;

        public APEquipSubstate()
        {
            float x = 65;
            float y = 25;

            selector.layer = Drawing.DrawOrder.EQUIPPED_ICON;

            _broom = new Equipment(new Vector2(x, y), "none_icon", GlobalState.inventory.HasBroom ? DialogueManager.GetDialogue("misc", "any", "items", 1) : "-");
            _broomExtend = new Equipment(new Vector2(x, y + 24), "long_icon", GlobalState.inventory.HasLengthen ? DialogueManager.GetDialogue("misc", "any", "items", 3) : "-");
            _broomWiden = new Equipment(new Vector2(x, y + 24 * 2), "wide_icon", GlobalState.inventory.HasWiden ? DialogueManager.GetDialogue("misc", "any", "items", 4) : "-");
            _transformer = new Equipment(new Vector2(x, y + 24 * 3), "transformer_icon", GlobalState.inventory.HasTransformer ? DialogueManager.GetDialogue("misc", "any", "items", 2) : "-");

            _transformerUnlocked = new UIEntity(new Vector2(x - 1, y + 24 * 3 - 1), "swapper_unlocked", 0, 9, 9, Drawing.DrawOrder.EQUIPPED_ICON) { visible = GlobalState.events.GetEvent("ExtendedSwap") == 1 };

            _jump = new UIEntity(new Vector2(62, 130), "archipelago_items", 28, 16, 16, Drawing.DrawOrder.EQUIPMENT_ICON);
            _boxItem = new UIEntity(new Vector2(78, 130), "archipelago_items", 26, 16, 16, Drawing.DrawOrder.EQUIPMENT_ICON);
            _shoesItem = new UIEntity(new Vector2(78 + 16, 130), "archipelago_items", 27, 16, 16, Drawing.DrawOrder.EQUIPMENT_ICON);

            _redCave = new UIEntity(new Vector2(62, 150), "archipelago_items", 16, 16, Drawing.DrawOrder.EQUIPMENT_ICON);
            _redCave.SetFrame(3);

            _redCaveLabel = new UILabel(new Vector2(62 + 16, 150 + 2), true, $"x{GlobalState.events.GetEvent("ProgressiveRedGrotto")}");

            if (Plugin.ArchipelagoManager!.SplitWindmill)
            {
                statuesMoved = [
                    GlobalState.events.GetEvent($"StatueMoved_{RegionID.BEDROOM}") != 0,
                    GlobalState.events.GetEvent($"StatueMoved_{RegionID.REDCAVE}") != 0,
                    GlobalState.events.GetEvent($"StatueMoved_{RegionID.CROWD}") != 0
                    ];

                splitWindmill = true;
            }
            else
            {
                statuesMoved = [.. Enumerable.Repeat(GlobalState.events.GetEvent("WindmillOpened") != 0, 3)];
            }

            if (GlobalState.events.GetEvent("ReceivedCardboardBox") != 0)
            {
                _boxItem.sprite.SetTexture("fields_npcs", 16, 16, false, false);
                _boxItem.SetFrame(31);
            }

            if (GlobalState.events.GetEvent("ReceivedBikingShoes") != 0)
            {
                _shoesItem.sprite.SetTexture("fields_npcs", 16, 16, false, false);
                _shoesItem.SetFrame(56);
            }

            if (GlobalState.inventory.CanJump)
            {
                _jump.sprite.SetTexture("item_jump_shoes", 16, 16, false, false);
                _jump.SetFrame(0);
            }

            _miao = new UIEntity(new Vector2(95 + 16, 130), "fields_npcs", 0, 16, 16, Drawing.DrawOrder.EQUIPMENT_ICON);

            if (GlobalState.events.GetEvent("ReceivedMiao") != 1)
            {
                _miao.sprite.SetTexture("archipelago_items", 16, 16, false, false);
                _miao.SetFrame(21);
            }

            _dams =
                [
                    new UIEntity(new Vector2(95 + 16 + 16 , 130), "archipelago_items", GlobalState.events.GetEvent("HappyDone") != 0 ? 23 : 25 , 16, 16, Drawing.DrawOrder.EQUIPMENT_ICON),
                    new UIEntity(new Vector2(95 + 16 + 16 * 2, 130), "archipelago_items", GlobalState.events.GetEvent("BlueDone") != 0 ? 22 : 24 , 16, 16, Drawing.DrawOrder.EQUIPMENT_ICON),

                ];

            _statues =
                [
                    new UIEntity(new Vector2(95 + 16, 150), "archipelago_items", statuesMoved[0] ? 6 : 9 , 16, 16, Drawing.DrawOrder.EQUIPMENT_ICON),
                    new UIEntity(new Vector2(95 + 32, 150), "archipelago_items", statuesMoved[1] ? 7 : 10 , 16, 16, Drawing.DrawOrder.EQUIPMENT_ICON),
                    new UIEntity(new Vector2(95 + 48, 150), "archipelago_items", statuesMoved[2] ? 8 : 11 , 16, 16, Drawing.DrawOrder.EQUIPMENT_ICON)
                    ];

            SetEquipped();
        }

        public override void GetControl()
        {
            base.GetControl();
            _state = EquipState.Broom;
            _lastState = _state;

            SetSelectorPos();
        }

        public override void Update()
        {
            base.Update();

            if (_lastState != _state)
            {
                _lastState = _state;
                SetSelectorPos();
                SoundManager.PlaySoundEffect("menu_move");
            }

        }

        public override void DrawUI()
        {
            base.DrawUI();

            _broom.Draw();
            _broomExtend.Draw();
            _broomWiden.Draw();
            _transformer.Draw();
            _transformerUnlocked.Draw();

            _boxItem.Draw();
            _shoesItem.Draw();
            _jump.Draw();
            _miao.Draw();

            _redCave.Draw();
            _redCaveLabel.Draw();

            foreach (var statue in _statues)
            {
                statue.Draw();
            }

            foreach (var dam in _dams)
            {
                dam.Draw();
            }

            selector.Draw();
        }

        public override void HandleInput()
        {
            if (InDialogueMode) return;

            if (KeyInput.JustPressedRebindableKey(KeyFunctions.Up))
            {
                if (_state == EquipState.Broom)
                {
                    return;
                }

                if (_state >= EquipState.RedCave)
                {
                    if (_state == EquipState.Statue1)
                    {
                        _state = EquipState.Miao;
                        current_bottom_index = bottom_row_enabled.IndexOf(_state);
                    }
                    else if (_state == EquipState.Statue2)
                    {
                        _state = EquipState.Happy;
                        current_bottom_index = bottom_row_enabled.IndexOf(_state);
                    }
                    else if (_state == EquipState.Statue3)
                    {
                        _state = EquipState.Blue;
                        current_bottom_index = bottom_row_enabled.IndexOf(_state);
                    }
                    else
                    {
                        _state = bottom_row_enabled[0];
                        current_bottom_index = 0;
                    }

                    return;
                }

                if (_state >= EquipState.Shoes)
                {
                    _state = EquipState.Transformer;
                    return;
                }

                _state--;
            }
            else if (KeyInput.JustPressedRebindableKey(KeyFunctions.Down))
            {
                if (_state >= EquipState.RedCave)
                {
                    return;
                }

                if (_state >= EquipState.Box)
                {
                    if (_state == EquipState.Happy)
                    {
                        _state = EquipState.Statue2;
                        current_bottom_index = very_bottom_row_enabled.IndexOf(_state);
                    }
                    else if (_state == EquipState.Blue)
                    {
                        _state = EquipState.Statue3;
                        current_bottom_index = very_bottom_row_enabled.IndexOf(_state);
                    }
                    else if (_state == EquipState.Miao)
                    {
                        _state = EquipState.Statue1;
                        current_bottom_index = very_bottom_row_enabled.IndexOf(_state);
                    }
                    else
                    {
                        _state = very_bottom_row_enabled[0];
                        current_bottom_index = 0;
                    }

                    return;
                }
                else if (_state == EquipState.Transformer)
                {
                    _state = bottom_row_enabled[0];
                    current_bottom_index = 0;
                }
                else if (_state == EquipState.Shoes)
                {
                    _state = very_bottom_row_enabled[0];
                    current_bottom_index = 0;
                }
                else if (_state < EquipState.Transformer)
                {
                    _state++;
                }
            }
            else if (current_bottom_index >= 0 && KeyInput.JustPressedRebindableKey(KeyFunctions.Right))
            {
                if (_state >= EquipState.RedCave)
                {
                    if (current_bottom_index == very_bottom_row_enabled.Count - 1)
                    {
                        return;
                    }

                    _state = very_bottom_row_enabled[++current_bottom_index];
                }
                else
                {
                    if (current_bottom_index == bottom_row_enabled.Count - 1)
                    {
                        return;
                    }

                    _state = bottom_row_enabled[++current_bottom_index];
                }
            }
            else if (current_bottom_index > 0 && KeyInput.JustPressedRebindableKey(KeyFunctions.Left))
            {
                if (current_bottom_index <= 0)
                {
                    return;
                }

                if (_state >= EquipState.RedCave)
                {
                    _state = very_bottom_row_enabled[--current_bottom_index];
                }
                else
                {
                    _state = bottom_row_enabled[--current_bottom_index];
                }

            }
            else if (KeyInput.JustPressedRebindableKey(KeyFunctions.Accept))
            {
                UseItem();
            }
            else
            {
                base.HandleInput();
            }
        }

        private void UseItem()
        {
            switch (_state)
            {
                case EquipState.Broom:
                    EquipBroom(BroomType.Normal);
                    break;
                case EquipState.Extend:
                    EquipBroom(BroomType.Long);
                    break;
                case EquipState.Widen:
                    EquipBroom(BroomType.Wide);
                    break;
                case EquipState.Transformer:
                    EquipBroom(BroomType.Transformer);
                    break;
                case EquipState.Shoes:
                    if (GlobalState.inventory.CanJump)
                    {
                        SetDialogue(DialogueManager.GetDialogue("misc", "any", "items", 5));
                    }
                    else
                    {
                        SetDialogue("You have not figured out how to jump yet.");
                    }
                    break;
                case EquipState.Box:
                    if (GlobalState.events.GetEvent("ReceivedCardboardBox") != 0)
                    {
                        SetDialogue(DialogueManager.GetDialogue("misc", "any", "items", 7));
                    }
                    else
                    {
                        SetDialogue("This is where you'd put a box. If you had one!");
                    }
                    break;
                case EquipState.BikingShoes:
                    if (GlobalState.events.GetEvent("ReceivedBikingShoes") != 0)
                    {
                        SetDialogue(DialogueManager.GetDialogue("misc", "any", "items", 6));
                    }
                    else
                    {
                        SetDialogue("No biking shoes yet!");
                    }
                    break;
                case EquipState.Happy:
                    if (GlobalState.events.GetEvent("HappyDone") != 0)
                    {
                        SetDialogue("The Happy Dam is open!");
                    }
                    else
                    {
                        SetDialogue("The Happy Dam remains unopened.");
                    }
                    break;
                case EquipState.Blue:
                    if (GlobalState.events.GetEvent("BlueDone") != 0)
                    {
                        SetDialogue("The Blue Dam is open!");
                    }
                    else
                    {
                        SetDialogue("The Blue Dam remains unopened.");
                    }
                    break;
                case EquipState.Miao:
                    if (GlobalState.events.GetEvent("ReceivedMiao") == 1)
                    {
                        SetDialogue("It's Miao! He's waiting for you in Fields.");
                    }
                    else
                    {
                        SetDialogue("You are sadly still cat-less...");
                    }
                    break;
                case EquipState.RedCave:
                    SetDialogue($"You currently have {GlobalState.events.GetEvent("ProgressiveRedGrotto")} progressive Red Cave Items.");
                    break;
                case EquipState.Statue1:
                    if (statuesMoved[0])
                    {
                        SetDialogue(splitWindmill ? 
                            "The statue in the Temple of the Seeing One has moved." :
                            "How does turning on a windmill even move statues??? Come on game designers!");
                    }
                    else
                    {
                        SetDialogue(splitWindmill ? 
                            "The statue in the Temple of the Seeing One has not yet moved." :
                            "Go do the windmill and maybe I'll move.");
                    }
                    break;
                case EquipState.Statue2:
                    if (statuesMoved[1])
                    {
                        SetDialogue(splitWindmill ? 
                            "The statue in the Red Cave has moved." :
                            "I think it's about abortions or something.");
                    }
                    else
                    {
                        SetDialogue(splitWindmill ?
                            "The statue in the Red Cave has not yet moved." :
                            "Idk man. Go do the windmill or something.");
                    }
                    break;
                case EquipState.Statue3:
                    if (statuesMoved[2])
                    {
                        SetDialogue(splitWindmill ? 
                            "The statue in the Mountain Cavern has moved." :
                            "Here we go! Wahoo!!!!!");
                    }
                    else
                    {
                        SetDialogue(splitWindmill ? 
                            "The statue in the Mountain Cavern has not yet moved." :
                            "Oh no! Mama mia!!!");
                    }
                    break;
            }
        }

        private void EquipBroom(BroomType broomType)
        {
            GlobalState.inventory.EquippedBroom = broomType;
            SetEquipped();
            ExitSubState();
        }

        private void SetSelectorPos()
        {
            bool ignoreOffset = false;
            switch (_state)
            {
                case EquipState.Broom:
                    selector.Position = _broom.LabelPos;
                    break;
                case EquipState.Extend:
                    selector.Position = _broomExtend.LabelPos;
                    break;
                case EquipState.Widen:
                    selector.Position = _broomWiden.LabelPos;
                    break;
                case EquipState.Transformer:
                    selector.Position = _transformer.LabelPos;
                    break;
                case EquipState.Shoes:
                    ignoreOffset = true;
                    selector.Position = _jump.Position;
                    break;
                case EquipState.Box:
                    ignoreOffset = true;
                    selector.Position = _boxItem.Position;
                    break;
                case EquipState.BikingShoes:
                    ignoreOffset = true;
                    selector.Position = _shoesItem.Position;
                    break;
                case EquipState.Happy:
                    ignoreOffset = true;
                    selector.Position = _dams[0].Position;
                    break;
                case EquipState.Blue:
                    ignoreOffset = true;
                    selector.Position = _dams[1].Position;
                    break;
                case EquipState.Miao:
                    ignoreOffset = true;
                    selector.Position = _miao.Position;
                    break;
                case EquipState.RedCave:
                    ignoreOffset = true;
                    selector.Position = _redCave.Position;
                    break;
                case EquipState.Statue1:
                    ignoreOffset = true;
                    selector.Position = _statues[0].Position;
                    break;
                case EquipState.Statue2:
                    ignoreOffset = true;
                    selector.Position = _statues[1].Position;
                    break;
                case EquipState.Statue3:
                    ignoreOffset = true;
                    selector.Position = _statues[2].Position;
                    break;
            }


            if (!ignoreOffset)
            {
                selector.Position -= new Vector2(selector.sprite.Width, -2);
                selector.Position.Y += CursorOffset;
            }
        }

        private void SetEquipped()
        {
            _broom.equipped = false;
            _broomExtend.equipped = false;
            _broomWiden.equipped = false;
            _transformer.equipped = false;

            switch (GlobalState.inventory.EquippedBroom)
            {
                case BroomType.Normal:
                    _broom.equipped = true;
                    break;
                case BroomType.Wide:
                    _broomWiden.equipped = true;
                    break;
                case BroomType.Long:
                    _broomExtend.equipped = true;
                    break;
                case BroomType.Transformer:
                    _transformer.equipped = true;
                    break;
            }
        }
    }
}
