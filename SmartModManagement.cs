using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using ReLogic.OS;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;
using Terraria.ModLoader.Core;
using Terraria.ModLoader.UI;
using Terraria.ModLoader.UI.ModBrowser;
using Terraria.Social.Base;
using Terraria.UI;
using Terraria.UI.Chat;

namespace SmartModManagement
{
    public class SmartModManagementConfig : ModConfig
    {
        public override ConfigScope Mode => ConfigScope.ClientSide;
        public override bool NeedsReload(ModConfig pendingConfig)
        {
            return ((SmartModManagementConfig)pendingConfig).DoNotDisableByGlobalButton != DoNotDisableByGlobalButton;
        }
        public override void OnLoaded()
        {
            if (FavouriteMods == null) FavouriteMods = new() { "SmartModManagement" };
        }
        [DefaultValue(true)]
        public bool DoNotDisableByGlobalButton;
        [DefaultValue(null)]
        public List<string> FavouriteMods;
    }
    public class SmartModManagement : Mod
    {
        public static SmartModManagement Instance;
        public static bool UseConciseModList;
        /// <summary>
        /// <see cref="Terraria.ModLoader.UI.UIMods"/> 
        /// <see cref="Terraria.ModLoader.UI.UIModPackItem"/> 
        /// <see cref="Terraria.ModLoader.UI.UIModPacks"/> 
        /// <see cref="Terraria.ModLoader.UI.UIModItem"/> 
        /// </summary>
        public override void Load() // Finding Class
        {
            UIMods = typeof(ModLoader).Assembly.GetType("Terraria.ModLoader.UI.UIMods");
            UIModPacks = typeof(ModLoader).Assembly.GetType("Terraria.ModLoader.UI.UIModPacks");
            UIModPackItem = typeof(ModLoader).Assembly.GetType("Terraria.ModLoader.UI.UIModPackItem");
            UIModItem = typeof(ModLoader).Assembly.GetType("Terraria.ModLoader.UI.UIModItem");
            var cfg = typeof(ModLoader).Assembly.GetType("Terraria.ModLoader.Config.ConfigManager");

            if (UIMods == null) throw new Exception("Cant find class \"Terraria.ModLoader.UI.UIMods\"");
            if (UIModPacks == null) throw new Exception("Cant find class \"Terraria.ModLoader.UI.UIModPacks\"");
            if (UIModPackItem == null) throw new Exception("Cant find class \"Terraria.ModLoader.UI.UIModPackItem\"");
            if (UIModItem == null) throw new Exception("Cant find class \"Terraria.ModLoader.UI.UIModItem\"");
            if (cfg == null) throw new Exception("Cant find class \"Terraria.ModLoader.UI.ConfigManager\"");

            UseConciseModList = ModLoader.HasMod("ConciseModList");

            if (UseConciseModList)
            {
                var assemb = ModLoader.GetMod("ConciseModList").GetType().Assembly;
                UIConciseModItem = assemb.GetType("ConciseModList.ConciseUIModItem");
                UIConciseUIList = assemb.GetType("ConciseModList.ImprovedUIList");
            }

            SaveConfig = cfg.GetMethod("Save", BindingFlags.Static | BindingFlags.NonPublic);

            Injection_ModListItem.Inject();
            Injection_ModList.Inject();

            AssetButtonBackIcon = Assets.Request<Texture2D>("Assets/ButtonBackIcon", AssetRequestMode.ImmediateLoad);
            AssetButtonConfigIcon = Assets.Request<Texture2D>("Assets/ButtonConfigIcon", AssetRequestMode.ImmediateLoad);
            AssetButtonEnableIcon = Assets.Request<Texture2D>("Assets/ButtonEnableIcon", AssetRequestMode.ImmediateLoad);
            AssetButtonReloadIcon = Assets.Request<Texture2D>("Assets/ButtonReloadIcon", AssetRequestMode.ImmediateLoad);
            AssetButtonCancelIcon = Assets.Request<Texture2D>("Assets/ButtonCancelIcon", AssetRequestMode.ImmediateLoad);
            AssetButtonDeleteIcon = Assets.Request<Texture2D>("Assets/ButtonDeleteIcon", AssetRequestMode.ImmediateLoad);
            AssetButtonImportIcon = Assets.Request<Texture2D>("Assets/ButtonImportIcon", AssetRequestMode.ImmediateLoad);
            AssetButtonRemoveIcon = Assets.Request<Texture2D>("Assets/ButtonRemoveIcon", AssetRequestMode.ImmediateLoad);
            AssetButtonDownloadIcon = Assets.Request<Texture2D>("Assets/ButtonDownloadIcon", AssetRequestMode.ImmediateLoad);

            Reload();   // I don't want to duplicate this code
            
            Instance = this;
        }
        public void Reload()
        {
            Interface.modsMenu = new();
            Interface.modPacksMenu = new();
            Interface.modBrowser = new(Interface.modBrowser.SocialBackend);
        }
        public override void Unload()
        {
            On_UIElement.Activate -= Injection_ModList.Activate;
            On_UIElement.Draw -= Injection_ModPacks.Draw;
            On_UIElement.Draw -= Injection_ModPacks.DrawAll;
            On_UIElement.Activate -= Injection_ModListItem.Activate;

            if (UIMods == null || UIModPacks == null) return;
            Injection_ModPacks.Loled = false;
            Reload();
        }
        public override void PostSetupContent() // Injecting
        {
            On_UIElement.Activate += Injection_ModList.Activate;
            On_UIElement.Draw += Injection_ModPacks.Draw;
            On_UIElement.Draw += Injection_ModPacks.DrawAll;
            On_UIElement.Draw += Injection_ModBrowser.DrawAll;
            On_UIElement.Activate += Injection_ModListItem.Activate;
            On_UIList.RecalculateChildren += Injection_ModList.ListRecalculate;

            if (ModContent.GetInstance<SmartModManagementConfig>() == null) new SmartModManagementConfig() { FavouriteMods = new() { "SmartModManagement" } };
        }
        #region Assets
        public static Asset<Texture2D> AssetButtonBackIcon;
        public static Asset<Texture2D> AssetButtonConfigIcon;
        public static Asset<Texture2D> AssetButtonEnableIcon;
        public static Asset<Texture2D> AssetButtonReloadIcon;
        public static Asset<Texture2D> AssetButtonCancelIcon;
        public static Asset<Texture2D> AssetButtonDeleteIcon;
        public static Asset<Texture2D> AssetButtonImportIcon;
        public static Asset<Texture2D> AssetButtonRemoveIcon;
        public static Asset<Texture2D> AssetButtonDownloadIcon;
        #endregion
        #region Fields
        public static Type UIMods;
        public static Type UIModPacks;
        public static Type UIModPackItem;
        public static Type UIModItem;

        public static Type UIConciseModItem;
        public static Type UIConciseUIList;

        public static MethodInfo SaveConfig;
        #endregion

        public static List<string> FavouriteMods()
        {
            var i = ModContent.GetInstance<SmartModManagementConfig>();
            if (i == null) new SmartModManagementConfig();
            if (i.FavouriteMods == null) i.FavouriteMods = new() { "SmartModManagement" };
            return i.FavouriteMods;
        }

        public class Injection_ModListItem
        {
            #region Fields
            public static FieldInfo _moreInfoButton;
            #endregion
            public static void Inject()
            {
                var priv = BindingFlags.Instance | BindingFlags.NonPublic;

                _moreInfoButton = UIModItem.GetField("_moreInfoButton", priv);
            }
            public class UIFavouriteButton : UIImage
            {
                public string mod => (string)Injection_ModList.ModName.Invoke(Parent, null);
                private static readonly Color disabled = new(0.65f, 0.65f, 1.5f, 1f);
                public UIFavouriteButton() : base(TextureAssets.Cursors[3]) { }
                public override void DrawSelf(SpriteBatch spriteBatch)
                {
                    var have = FavouriteMods().Contains(mod);
                    Color = have ? Color.White : disabled;
                    base.DrawSelf(spriteBatch);
                    if (IsMouseHovering)
                    {
                        UICommon.TooltipMouseText(Language.GetTextValue("tModLoader.ModsFavourite" + (have ? "Rem" : "Add")));
                    }
                }
                public override void LeftClick(UIMouseEvent evt)
                {
                    var fav = FavouriteMods();
                    if (fav.Contains(mod)) fav.Remove(mod);
                    else fav.Add(mod);
                    SaveConfig.Invoke(null, [ModContent.GetInstance<SmartModManagementConfig>()]);
                    Interface.modsMenu.updateNeeded = true;
                }
            }
            public static void Activate(On_UIElement.orig_Activate orig, UIElement self)
            {
                orig(self);

                float size = 20;
                StyleDimension left = new(-5, 0);
                StyleDimension top = new(-5, 0);

                if (UseConciseModList && self.GetType().Equals(UIConciseModItem))
                {
                    // Nothing, Yep!
                }
                else if (self.GetType().Equals(UIModItem))
                {
                    var moreInfoButton = _moreInfoButton.GetValue(self) as UIImage;
                    size = (moreInfoButton.Height.Pixels - 5) * moreInfoButton.ImageScale;
                    left = moreInfoButton.Left;
                    left.Pixels -= size * 2.7f - 3;
                    top = moreInfoButton.Top;
                    top.Pixels += 3;
                }
                else return;


                var item = self as UIModItem;
                if (item._modRequiresTooltip != null && item._modReferenceIcon != null)
                {
                    var index = item._modRequiresTooltip.IndexOf("\n");
                    if (index != -1)
                    {
                        item._modRequiresTooltip = "Left Click to View\n" + item._modRequiresTooltip;
                    }
                    item._modReferenceIcon.OnLeftClick += Injection_ModList.ViewRequiresMods;
                }
                UIImage translationModIcon = item._translationModIcon;
                if (translationModIcon != null)
                {
                    string arg = "\n- " + string.Join("\n- ", item._mod.properties.RefNames(includeWeak: true));
                    var tooltip = "Left Click to View\n" + Language.GetTextValue("tModLoader.TranslationModTooltip", arg);
                    translationModIcon.Append(new UIButtonDesc(tooltip, 2));
                    translationModIcon.OnLeftClick += Injection_ModList.ViewTranslateMods;
                    item._translationModIcon = null;
                }

                var star = new UIFavouriteButton()
                {
                    Width = { Pixels = size },
                    Height = { Pixels = size },
                    Left = left,
                    Top = top,
                    ScaleToFit = true
                };
                star.OverrideSamplerState = SamplerState.PointClamp;
                self.Append(star);
            }
        }
        public class Injection_ModBrowser
        {
            public class UITextPanelLOL<T> : UITextPanel<T>
            {
                public int index = -1;
                public UIElement shadowed;
                public UITextPanelLOL(int i, T text, float textScale = 1, bool large = false, UIElement shadow = null) : base(text, textScale, large)
                {
                    index = i;
                    shadowed = shadow;
                    if (shadowed != null)
                    {
                        Elements.AddRange(shadow.Elements);
                        BackgroundColor = BackgroundColor.MultiplyRGB(new(0.7f, 0.7f, 0.7f));
                        BorderColor = BorderColor.MultiplyRGB(new(0.7f, 0.7f, 0.7f));
                    }
                    else
                        this.WithFadedMouseOver();
                }

                public override void SetText(T text, float textScale, bool large)
                {
                    _text = text;
                    _textScale = textScale;
                    _isLarge = large;
                }
                public bool parented = true;
                public override void Draw(SpriteBatch spriteBatch)
                {
                    if (shadowed != null)
                    {
                        if (shadowed.Parent == null)
                        {
                            if (parented)
                            {
                                parented = false;
                                (Elements[0] as UIImage).Color = new Color(180, 180, 180, 255);
                            }
                        }
                        else
                        {
                            if (!parented)
                            {
                                parented = true;
                                (Elements[0] as UIImage).Color = Color.White;
                            }
                            return;
                        }
                    }
                    base.Draw(spriteBatch);
                }
                public override void DrawSelf(SpriteBatch spriteBatch)
                {
                    if (_needsTextureLoading)
                    {
                        _needsTextureLoading = false;
                        LoadTextures();
                    }

                    if (_backgroundTexture != null)
                    {
                        DrawPanel(spriteBatch, _backgroundTexture.Value, BackgroundColor);
                    }

                    if (_borderTexture != null)
                    {
                        DrawPanel(spriteBatch, _borderTexture.Value, BorderColor);
                    }
                    CalculatedStyle innerDimensions = GetInnerDimensions();
                    string text = Text;
                    if (HideContents)
                    {
                        if (_asterisks == null || _asterisks.Length != text.Length)
                        {
                            _asterisks = new string('*', text.Length);
                        }

                        text = _asterisks;
                    }
                    var size = ChatManager.GetStringSize(_isLarge ? FontAssets.DeathText.Value : FontAssets.MouseText.Value, text, Vector2.One);
                    var scale = MathF.Min(MathF.Min(innerDimensions.Width / size.X, innerDimensions.Height / size.Y), 1f);
                    if (_isLarge)
                    {
                        Utils.DrawBorderStringBig(spriteBatch, text, innerDimensions.Center() + new Vector2(0,1.5f), _color, scale, 0.5f, 0.5f);
                    }
                    else
                    {
                        Utils.DrawBorderString(spriteBatch, text, innerDimensions.Center() + new Vector2(0, 1.5f), _color, scale, 0.5f, 0.5f);
                    }
                    if (IsMouseHovering)
                    {
                        if (shadowed != null)
                        {
                            UICommon.TooltipMouseText(Language.GetTextValue("tModLoader.NoFiltersInfo"));
                            return;
                        }
                        if (index == 0)
                        {
                            UICommon.TooltipMouseText(Language.GetTextValue("tModLoader.ModClipboardCopyInfo") +
                                (Interface.modBrowser.SpecialModPackFilter != null ? "" : "\n[c/FFAFAF:" + Language.GetTextValue("tModLoader.ModClipboardCopyInfoFail") + "]"));
                        }
                        else if (index == 1)
                        {
                            UICommon.TooltipMouseText(Language.GetTextValue("tModLoader.ModClipboardPasteInfo"));
                        }
                        else if (index == 2)
                        {
                            UICommon.TooltipMouseText(Language.GetTextValue("tModLoader.EnableOnlyThisListInfo"));
                        }
                    }
                }
            }
            public static UITextPanelLOL<L> Rework<L>(UITextPanel<L> kekw, int index = -1, UIElement shadow = null)
            {
                var me = new UITextPanelLOL<L>(index, kekw._text, kekw._textScale, kekw._isLarge, shadow);
                me.Top = kekw.Top;
                me.Left = kekw.Left;
                me.Width = kekw.Width;
                me.Height = kekw.Height;
                me.PaddingBottom = kekw.PaddingBottom;
                me.PaddingLeft = kekw.PaddingLeft;
                me.PaddingRight = kekw.PaddingRight;
                me.PaddingTop = kekw.PaddingTop;
                me.HAlign = kekw.HAlign;
                me.VAlign = kekw.VAlign;
                me.Elements.AddRange(kekw.Elements);
                if (kekw.Parent != null && shadow == null)    // Replacing
                {
                    kekw.Parent.Append(me);
                    var childs = kekw.Parent.Elements;
                    childs.Remove(me);
                    childs.Insert(childs.IndexOf(kekw), me);
                    childs.Remove(kekw);
                }
                return me;
            }
            public static bool Loled;
            public static void DrawAll(On_UIElement.orig_Draw orig, UIElement self, SpriteBatch spriteBatch)
            {
                orig(self, spriteBatch);
                if (!self.GetType().Equals(typeof(UIModBrowser)) || Loled) return;
                Loled = true;

                var bws = self as UIModBrowser;

                var topPanel = ((List<UIElement>)((List<UIElement>)bws.Children)[0].Children)[1];
                topPanel.Top.Pixels = -45;

                bws._backButton = Rework(bws._backButton);
                var buttonBack = bws._backButton;
                {
                    var icon = new UIImage(AssetButtonBackIcon);
                    icon.VAlign = 0.5f;
                    icon.Width.Pixels = icon.Height.Pixels = 28;
                    icon.Left.Set(-34, 0);
                    icon.ScaleToFit = true;
                    icon.OverrideSamplerState = SamplerState.PointClamp;
                    icon.IgnoresMouseInteraction = true;
                    buttonBack.PaddingLeft = 40;
                    buttonBack.Width.Set(-10, 0.275f);
                    buttonBack.Left.Set(0, 0);
                    buttonBack.Top.Set(-20f, 0);
                    buttonBack.Append(icon);
                    buttonBack.HAlign = 0;
                    buttonBack.PaddingTop = buttonBack.PaddingBottom = 1;
                    buttonBack.Height.Pixels += 17.5f;
                    buttonBack.OnLeftClick += delegate
                    {
                        Interface.modBrowser.HandleBackButtonUsage();
                    };
                }
                bws._downloadAllButton = Rework(bws._downloadAllButton);
                var buttonDownload = bws._downloadAllButton;
                {
                    var icon = new UIImage(AssetButtonDownloadIcon);
                    icon.VAlign = 0.5f;
                    icon.Width.Pixels = 28;
                    icon.Height.Pixels = 32;
                    icon.Left.Set(-34, 0);
                    icon.ScaleToFit = true;
                    icon.OverrideSamplerState = SamplerState.PointClamp;
                    icon.IgnoresMouseInteraction = true;
                    buttonDownload.PaddingLeft = 40;
                    buttonDownload.Width.Set(-10, 0.45f);
                    buttonDownload.Left.Set(0, 0.275f);
                    buttonDownload.Top.Set(-20f, 0);
                    buttonDownload.Append(icon);
                    buttonDownload.HAlign = 0;
                    buttonDownload.PaddingTop = buttonDownload.PaddingBottom = 1;
                    buttonDownload.Height.Pixels += 17.5f;
                    buttonDownload.OnLeftClick += Interface.modBrowser.DownloadAllFilteredMods;
                    if (buttonDownload.Parent == null)
                    {
                        buttonDownload.Parent = buttonBack.Parent;
                        buttonDownload.Recalculate();
                        buttonDownload.Draw(spriteBatch);
                        buttonDownload.Parent = null;
                    }
                    buttonBack.Parent.Append(Rework(buttonDownload, -1, buttonDownload));
                }
                bws._reloadButton = Rework(bws._reloadButton);
                var buttonReload = bws._reloadButton;
                {
                    var icon = new UIImage(AssetButtonReloadIcon);
                    icon.VAlign = 0.5f;
                    icon.Width.Pixels = icon.Height.Pixels = 28;
                    icon.Left.Set(-34, 0);
                    icon.ScaleToFit = true;
                    icon.OverrideSamplerState = SamplerState.PointClamp;
                    icon.IgnoresMouseInteraction = true;
                    buttonReload.PaddingLeft = 40;
                    buttonReload.Width.Set(-10, 0.275f);
                    buttonReload.Left.Set(0, 0);
                    buttonReload.Top.Set(-65f, 0);
                    buttonReload.Append(icon);
                    buttonReload.HAlign = 0;
                    buttonReload.PaddingTop = buttonReload.PaddingBottom = 1;
                    buttonReload.Height.Pixels += 17.5f;
                    buttonReload.OnLeftClick += Interface.modBrowser.ReloadList;
                }
                bws._clearButton = Rework(bws._clearButton);
                var buttonClear = bws._clearButton;
                {
                    var icon = new UIImage(AssetButtonCancelIcon);
                    icon.VAlign = 0.5f;
                    icon.Width.Pixels = icon.Height.Pixels = 28;
                    icon.Left.Set(-34, 0);
                    icon.ScaleToFit = true;
                    icon.OverrideSamplerState = SamplerState.PointClamp;
                    icon.IgnoresMouseInteraction = true;
                    buttonClear.PaddingLeft = 40;
                    buttonClear.Width.Set(-10, 0.45f);
                    buttonClear.Left.Set(0, 0.275f);
                    buttonClear.Top.Set(-65f, 0);
                    buttonClear.Append(icon);
                    buttonClear.HAlign = 0;
                    buttonClear.PaddingTop = buttonClear.PaddingBottom = 1;
                    buttonClear.Height.Pixels += 17.5f;
                    buttonClear.OnLeftClick += Interface.modBrowser.ClearTextFilters;
                    if (buttonClear.Parent == null)
                    {
                        buttonClear.Parent = buttonBack.Parent;
                        buttonClear.Recalculate();
                        buttonClear.Draw(spriteBatch);
                        buttonClear.Parent = null;
                    }
                    buttonBack.Parent.Append(Rework(buttonClear, -1, buttonClear));
                }
                var buttonCopy = new UITextPanel<LocalizedText>(Language.GetText("tModLoader.ModClipboardCopy"));
                {
                    var icon = new UIImage(Main.Assets.Request<Texture2D>("Images/UI/CharCreation/Copy"));
                    icon.VAlign = 0.5f;
                    icon.Width.Pixels = icon.Height.Pixels = 28;
                    icon.Left.Set(-34, 0);
                    icon.ScaleToFit = true;
                    icon.OverrideSamplerState = SamplerState.PointClamp;
                    icon.IgnoresMouseInteraction = true;
                    buttonCopy.PaddingLeft = 40;
                    buttonCopy.Width.Set(-10, 0.275f);
                    buttonCopy.Left.Set(0, 0.725f);
                    buttonCopy.Height = buttonBack.Height;
                    buttonCopy.Top.Set(-65f, 0);
                    buttonCopy.Append(icon);
                    buttonCopy.HAlign = 0;
                    buttonCopy.VAlign = 1;
                    buttonCopy.PaddingTop = buttonCopy.PaddingBottom = 1;
                }
                buttonBack.Parent.Append(buttonCopy);
                Rework(buttonCopy, 0).OnLeftClick += delegate
                {
                    var f = Interface.modBrowser.SpecialModPackFilter;
                    if (f != null)
                    {
                        var l = new List<string>();
                        foreach (var item in f)
                        {
                            l.Add(item.m_ModPubId);
                        }
                        SoundEngine.PlaySound(10);
                        Platform.Get<IClipboard>().Value = string.Join("|", l);
                    }
                };
                var buttonPaste = new UITextPanel<LocalizedText>(Language.GetText("tModLoader.ModClipboardPaste"));
                {
                    var icon = new UIImage(Main.Assets.Request<Texture2D>("Images/UI/CharCreation/Paste"));
                    icon.VAlign = 0.5f;
                    icon.Width.Pixels = icon.Height.Pixels = 28;
                    icon.Left.Set(-34, 0);
                    icon.ScaleToFit = true;
                    icon.OverrideSamplerState = SamplerState.PointClamp;
                    icon.IgnoresMouseInteraction = true;
                    buttonPaste.PaddingLeft = 40;
                    buttonPaste.Width.Set(-10, 0.275f);
                    buttonPaste.Height = buttonBack.Height;
                    buttonPaste.Left.Set(0, 0.725f);
                    buttonPaste.Top.Set(-20f, 0);
                    buttonPaste.Append(icon);
                    buttonPaste.HAlign = 0;
                    buttonPaste.VAlign = 1;
                    buttonPaste.PaddingTop = buttonPaste.PaddingBottom = 1;
                }
                bws.HeaderTextPanel.Top.Set(-45, 0);
                bws.FilterTextBox.Width.Pixels += 150;
                bws.FilterTextBox.Left.Pixels -= 150;
                bws._filterTextBoxBackground.Width.Pixels += 150;
                bws._filterTextBoxBackground.Left.Pixels -= 150;
                bws._browserStatus.Top.Set(-71f, 0);
                bws._browserStatus.Left.Set(0, 1f);
                buttonBack.Parent.Append(buttonPaste);
                Rework(buttonPaste, 1).OnLeftClick += delegate
                {
                    var cpb = Platform.Get<IClipboard>().Value;
                    try
                    {
                        List<ModPubId_t> list = new();
                        var s = cpb.Split("|");
                        foreach (var item in s)
                        {
                            ModPubId_t i = default;
                            i.m_ModPubId = item;
                            list.Add(i);
                        }
                        Interface.modBrowser.FilterTextBox.Text = "";
                        Interface.modBrowser.SpecialModPackFilter = list;
                        Interface.modBrowser.SpecialModPackFilterTitle = Language.GetTextValue("tModLoader.MBFilterModlist");
                        Interface.modBrowser.UpdateFilterMode = UpdateFilter.All;
                        Interface.modBrowser.ModSideFilterMode = ModSideFilter.All;
                        Interface.modBrowser.ResetTagFilters();
                        SoundEngine.PlaySound(10);
                    }
                    catch (Exception)
                    {
                        SoundEngine.PlaySound(11);
                    }
                };
                var buttonToggle = new UITextPanel<LocalizedText>(Language.GetText("tModLoader.ModPackEnableOnlyThisList"));
                {
                    var icon = new UIImage(AssetButtonReloadIcon);
                    icon.VAlign = 0.5f;
                    icon.Width.Pixels = icon.Height.Pixels = 28;
                    icon.Left.Set(-34, 0);
                    icon.ScaleToFit = true;
                    icon.OverrideSamplerState = SamplerState.PointClamp;
                    icon.IgnoresMouseInteraction = true;
                    buttonToggle.PaddingLeft = 40;

                    var dim = buttonBack.Parent.GetOuterDimensions();

                    buttonToggle.Width.Set(dim.Width * 0.275f - 10, 0);

                    buttonToggle.Left.Set(dim.X + dim.Width, 0);
                    buttonToggle.Top.Set(dim.Y + dim.Height - 20f - buttonBack.Height.Pixels, 0);
                    buttonToggle.Height = buttonBack.Height;
                    buttonToggle.Append(icon);
                    buttonToggle.HAlign = 0;
                    buttonToggle.VAlign = 0;
                    buttonToggle.PaddingTop = 3;
                    buttonToggle.PaddingBottom = 1;
                }
                buttonBack.Parent.Parent.Append(buttonToggle);
                Rework(buttonToggle, 2).OnLeftClick += delegate
                {
                    if (Interface.modBrowser.SpecialModPackFilter == null) return;
                    var l = Interface.modBrowser.ModList.ReceivedItems;
                    var names = new List<string>();
                    ModLoader.EnabledMods.Clear();
                    if (l != null)
                    {
                        foreach (var item in l)
                        {
                            ModLoader.EnabledMods.Add(item.ModDownload.ModName);
                        }
                    }
                    ModOrganizer.SaveEnabledMods();
                    ModLoader.Reload();
                };
                self.Recalculate();

                bws._updateAllButton = Rework(bws._updateAllButton);
                var buttonUpdate = bws._updateAllButton;
                {
                    var icon = new UIImage(AssetButtonDownloadIcon);
                    icon.VAlign = 0.5f;
                    icon.Width.Pixels = 28;
                    icon.Height.Pixels = 32;
                    icon.Left.Set(-34, 0);
                    icon.ScaleToFit = true;
                    icon.OverrideSamplerState = SamplerState.PointClamp;
                    icon.IgnoresMouseInteraction = true;
                    buttonUpdate.PaddingLeft = 40;


                    buttonUpdate.Left = buttonToggle.Left;
                    buttonUpdate.Top = buttonToggle.Top;
                    buttonUpdate.Top.Pixels -= 45;
                    buttonUpdate.Width = buttonToggle.Width;
                    buttonUpdate.Height = buttonToggle.Height;

                    buttonUpdate.Append(icon);
                    buttonUpdate.HAlign = 0;
                    buttonUpdate.VAlign = 0;
                    buttonUpdate.PaddingTop = 3;
                    buttonUpdate.PaddingBottom = 1;
                    buttonUpdate.OnLeftClick += bws.UpdateAllMods;
                    if (buttonUpdate.Parent == null)
                    {
                        buttonUpdate.Parent = buttonBack.Parent;
                        buttonUpdate.Recalculate();
                        buttonUpdate.Draw(spriteBatch);
                        buttonUpdate.Parent = null;
                    }
                }

                var l = Interface.modBrowser.SpecialModPackFilter;
                Interface.modBrowser.SpecialModPackFilter = null;
                Interface.modBrowser._firstLoad = false;
                Interface.modBrowser.SpecialModPackFilter = l;
                Interface.modBrowser.SetHeading(Language.GetText("tModLoader.MenuModBrowser"));
                Interface.modBrowser.ModList.SetEnumerable(Interface.modBrowser.SocialBackend.QueryBrowser(Interface.modBrowser.FilterParameters));
            }
        }
        public class Injection_ModPacks
        {
            public static bool Loled;
            public static void DrawAll(On_UIElement.orig_Draw orig, UIElement self, SpriteBatch spriteBatch)
            {
                orig(self, spriteBatch);
                if (!self.GetType().Equals(UIModPacks) || Loled) return;
                Loled = true;

                var pck = self as UIModPacks;

                var topPanel = ((List<UIElement>)((List<UIElement>)pck.Children)[0].Children)[1];
                topPanel.Top.Pixels = -45;

                var elem = (self.Children as List<UIElement>)[0];
                var child = elem.Children as List<UIElement>;

                var c = child[0].Children as List<UIElement>;
                c[0].Height.Set(0, 1);
                c[1].Height.Set(0, 1);
                ((UIList)c[0]).ListPadding = 10;

                elem.Height.Pixels += 80;
                elem.Top.Pixels -= 20;


                var buttonFolder = child[2] as UIAutoScaleTextTextPanel<LocalizedText>;
                {
                    buttonFolder.PaddingTop = buttonFolder.PaddingBottom = 1;
                    var icon = new UIImage(TextureAssets.Camera[6]);
                    icon.VAlign = 0.5f;
                    icon.Width.Pixels = icon.Height.Pixels = 28;
                    icon.Left.Set(-34, 0);
                    icon.ScaleToFit = true;
                    icon.OverrideSamplerState = SamplerState.PointClamp;
                    icon.IgnoresMouseInteraction = true;
                    buttonFolder.PaddingLeft = 40;
                    buttonFolder.UseInnerDimensions = true;
                    buttonFolder.Width.Set(-10, 0.3333f);
                    buttonFolder.Left.Set(0, 0.3333f);
                    buttonFolder.HAlign = 0;
                    buttonFolder.VAlign = 0.9f;
                    buttonFolder.Top.Pixels = -20;
                    buttonFolder.Append(icon);
                }
                var buttonBack = child[3] as UIAutoScaleTextTextPanel<LocalizedText>;
                {
                    buttonBack.PaddingTop = buttonBack.PaddingBottom = 1;
                    var icon = new UIImage(AssetButtonBackIcon);
                    icon.VAlign = 0.5f;
                    icon.Width.Pixels = icon.Height.Pixels = 28;
                    icon.Left.Set(-34, 0);
                    icon.ScaleToFit = true;
                    icon.OverrideSamplerState = SamplerState.PointClamp;
                    icon.IgnoresMouseInteraction = true;
                    buttonBack.PaddingLeft = 40;
                    buttonBack.UseInnerDimensions = true;
                    buttonBack.Width.Percent = 0.3333f;
                    buttonBack.VAlign = 0.9f;
                    buttonBack.Append(icon);
                }
                var buttonCreate = child[4] as UIAutoScaleTextTextPanel<LocalizedText>;
                {
                    buttonCreate.PaddingTop = buttonCreate.PaddingBottom = 1;
                    var icon = new UIImage(TextureAssets.Cursors[3]);
                    icon.VAlign = 0.5f;
                    icon.Width.Pixels = icon.Height.Pixels = 28;
                    icon.Left.Set(-34, 0);
                    icon.ScaleToFit = true;
                    icon.OverrideSamplerState = SamplerState.PointClamp;
                    icon.IgnoresMouseInteraction = true;
                    buttonCreate.TextColor = Color.White;
                    buttonCreate.VAlign = 0.9f;
                    buttonCreate.PaddingLeft = 40;
                    buttonCreate.UseInnerDimensions = true;
                    buttonCreate.Width.Set(-10, 0.3333f);
                    buttonCreate.Left.Set(0, 0.6666f);
                    buttonCreate.HAlign = 0;
                    buttonCreate.Append(icon);
                }

                self.Recalculate();
            }
            public static void Draw(On_UIElement.orig_Draw orig, UIElement self, SpriteBatch spriteBatch)
            {
                orig(self, spriteBatch);
                if (!self.GetType().Equals(UIModPackItem)) return;
                var childs = self.Children as List<UIElement>;

                var name = (UIText)childs[0];
                if (name.Text.EndsWith("   ")) return;
                name.SetText(name.Text + "   ", 1, false);

                var buttonViewList = childs[1] as UIAutoScaleTextTextPanel<string>;
                {
                    buttonViewList.PaddingTop = buttonViewList.PaddingBottom = 1;
                    var icon = new UIImage(TextureAssets.Cursors[2]);
                    icon.VAlign = 0.5f;
                    icon.Width.Pixels = icon.Height.Pixels = 28;
                    icon.Left.Set(-34, 0);
                    icon.ScaleToFit = true;
                    icon.OverrideSamplerState = SamplerState.PointClamp;
                    icon.IgnoresMouseInteraction = true;
                    buttonViewList.PaddingLeft = 40;
                    buttonViewList.UseInnerDimensions = true;
                    buttonViewList.Width.Set(-10, 0.25f);
                    buttonViewList.Left.Set(0, 0.5f);
                    buttonViewList.HAlign = 0;
                    buttonViewList.Append(icon);
                }
                var buttonEnable = childs[2] as UIAutoScaleTextTextPanel<string>;
                {
                    buttonEnable.PaddingTop = buttonEnable.PaddingBottom = 1;
                    var icon = new UIImage(AssetButtonEnableIcon);
                    icon.VAlign = 0.5f;
                    icon.Width.Pixels = icon.Height.Pixels = 30;
                    icon.Left.Set(-35, 0);
                    icon.ScaleToFit = true;
                    icon.OverrideSamplerState = SamplerState.PointClamp;
                    icon.IgnoresMouseInteraction = true;
                    buttonEnable.PaddingLeft = 40;
                    buttonEnable.UseInnerDimensions = true;
                    buttonEnable.Width.Set(-10, 0.25f);
                    buttonEnable.Left.Set(0, 0);
                    buttonEnable.HAlign = 0;
                    buttonEnable.Append(icon);
                }
                var buttonEnableOnly = childs[3] as UIAutoScaleTextTextPanel<string>;
                {
                    buttonEnableOnly.PaddingTop = buttonEnableOnly.PaddingBottom = 3;
                    var icon = new UIImage(AssetButtonReloadIcon);
                    icon.VAlign = 0.5f;
                    icon.Width.Pixels = icon.Height.Pixels = 24;
                    icon.Left.Set(-32, 0);
                    icon.ScaleToFit = true;
                    icon.Color = Color.Yellow;
                    icon.OverrideSamplerState = SamplerState.PointClamp;
                    icon.IgnoresMouseInteraction = true;
                    buttonEnableOnly.PaddingLeft = 40;
                    buttonEnableOnly.UseInnerDimensions = true;
                    buttonEnableOnly.Width.Set(-10, 0.25f);
                    buttonEnableOnly.Left.Set(0, 0.25f);
                    buttonEnableOnly.HAlign = 0;
                    buttonEnableOnly.Append(icon);
                }
                var buttonViewBrowser = childs[4] as UIAutoScaleTextTextPanel<string>;
                {
                    buttonViewBrowser.PaddingTop = buttonViewBrowser.PaddingBottom = 1;
                    var icon = new UIImage(TextureAssets.Cursors[2]);
                    icon.VAlign = 0.5f;
                    icon.Width.Pixels = icon.Height.Pixels = 28;
                    icon.Left.Set(-34, 0);
                    icon.ScaleToFit = true;
                    icon.OverrideSamplerState = SamplerState.PointClamp;
                    icon.IgnoresMouseInteraction = true;
                    buttonViewBrowser.PaddingLeft = 40;
                    buttonViewBrowser.UseInnerDimensions = true;
                    buttonViewBrowser.Width.Set(-10, 0.25f);
                    buttonViewBrowser.Left.Set(0, 0.75f);
                    buttonViewBrowser.Top.Set(40, 0);
                    buttonViewBrowser.HAlign = 0;
                    buttonViewBrowser.Append(icon);
                }
                var buttonUpdate = childs[5] as UIAutoScaleTextTextPanel<string>;
                {
                    buttonUpdate.PaddingTop = buttonUpdate.PaddingBottom = 1;
                    var icon = new UIImage(AssetButtonCancelIcon);
                    icon.VAlign = 0.5f;
                    icon.Width.Pixels = icon.Height.Pixels = 28;
                    icon.Left.Set(-34, 0);
                    icon.ScaleToFit = true;
                    icon.OverrideSamplerState = SamplerState.PointClamp;
                    icon.IgnoresMouseInteraction = true;
                    buttonUpdate.PaddingLeft = 40;
                    buttonUpdate.UseInnerDimensions = true;
                    buttonUpdate.Width.Set(-10, 0.25f);
                    buttonUpdate.Left.Set(0, 0);
                    buttonUpdate.Top.Set(80, 0);
                    buttonUpdate.HAlign = 0;
                    buttonUpdate.Append(icon);
                }
                var buttonDelete = childs[6] as UIImageButton;
                {
                    buttonDelete.SetPadding(0);
                    var del = new UIAutoScaleTextTextPanel<LocalizedText>(Language.GetText("UI.Delete"));
                    del.Width.Set(0, 1); del.Height.Set(0, 1);
                    del.Left.Set(0, 0); del.Top.Set(0, 0);
                    del.WithFadedMouseOver();

                    del.PaddingTop = del.PaddingBottom = 1;
                    var icon = new UIImage(AssetButtonDeleteIcon);
                    icon.VAlign = 0.5f;
                    icon.Width.Pixels = icon.Height.Pixels = 28;
                    icon.Left.Set(-34, 0);
                    icon.ScaleToFit = true;
                    icon.OverrideSamplerState = SamplerState.PointClamp;
                    icon.IgnoresMouseInteraction = true;
                    del.Append(icon);
                    del.UseInnerDimensions = true;
                    del.PaddingLeft = 40;
                    buttonDelete.Width.Set(-10, 0.25f); buttonDelete.Height.Set(32, 0);
                    buttonDelete.Left.Set(0, 0.75f);
                    buttonDelete.Top.Set(80, 0);
                    buttonDelete.HAlign = 0;
                    buttonDelete.Append(del);
                    buttonDelete.SetVisibility(0, 0);
                }
                var buttonImport = childs[7] as UIAutoScaleTextTextPanel<string>;
                {
                    buttonImport.PaddingTop = buttonImport.PaddingBottom = 1;
                    var icon = new UIImage(AssetButtonImportIcon);
                    icon.VAlign = 0.5f;
                    icon.Width.Pixels = icon.Height.Pixels = 28;
                    icon.Left.Set(-34, 0);
                    icon.ScaleToFit = true;
                    icon.OverrideSamplerState = SamplerState.PointClamp;
                    icon.IgnoresMouseInteraction = true;
                    buttonImport.PaddingLeft = 40;
                    buttonImport.UseInnerDimensions = true;
                    buttonImport.Width.Set(-10, 0.25f);
                    buttonImport.Left.Set(0, 0.25f);
                    buttonImport.Top.Set(80, 0);
                    buttonImport.HAlign = 0;
                    buttonImport.Append(icon);
                }
                var buttonRemove = childs[8] as UIAutoScaleTextTextPanel<string>;
                {
                    buttonRemove.PaddingTop = buttonRemove.PaddingBottom = 1;
                    var icon = new UIImage(AssetButtonRemoveIcon);
                    icon.VAlign = 0.5f;
                    icon.Width.Pixels = icon.Height.Pixels = 28;
                    icon.Left.Set(-34, 0);
                    icon.ScaleToFit = true;
                    icon.OverrideSamplerState = SamplerState.PointClamp;
                    icon.IgnoresMouseInteraction = true;
                    buttonRemove.PaddingLeft = 40;
                    buttonRemove.UseInnerDimensions = true;
                    buttonRemove.Width.Set(-10, 0.25f);
                    buttonRemove.Left.Set(0, 0.5f);
                    buttonRemove.Top.Set(80, 0);
                    buttonRemove.HAlign = 0;
                    buttonRemove.Append(icon);
                }
                var buttonExport = childs[9] as UIAutoScaleTextTextPanel<string>;
                {
                    buttonExport.PaddingTop = buttonExport.PaddingBottom = 1;
                    var icon = new UIImage(TextureAssets.Camera[6]);
                    icon.VAlign = 0.5f;
                    icon.Width.Pixels = icon.Height.Pixels = 28;
                    icon.Left.Set(-34, 0);
                    icon.ScaleToFit = true;
                    icon.OverrideSamplerState = SamplerState.PointClamp;
                    icon.IgnoresMouseInteraction = true;
                    buttonExport.PaddingLeft = 40;
                    buttonExport.UseInnerDimensions = true;
                    buttonExport.Width.Set(-10, 0.5f);
                    buttonExport.Left.Set(0, 0);
                    buttonExport.Top.Set(120, 0);
                    buttonExport.HAlign = 0;
                    buttonExport.Append(icon);
                }
                if (childs.Count >= 11)
                {
                    var buttonDeleteInstance = childs[10] as UIAutoScaleTextTextPanel<string>;
                    {
                        buttonDeleteInstance.PaddingTop = buttonDeleteInstance.PaddingBottom = 1;
                        var icon = new UIImage(AssetButtonDeleteIcon);
                        icon.VAlign = 0.5f;
                        icon.Width.Pixels = icon.Height.Pixels = 28;
                        icon.Left.Set(-34, 0);
                        icon.ScaleToFit = true;
                        icon.OverrideSamplerState = SamplerState.PointClamp;
                        icon.IgnoresMouseInteraction = true;
                        buttonDeleteInstance.PaddingLeft = 40;
                        buttonDeleteInstance.UseInnerDimensions = true;
                        buttonDeleteInstance.Width.Set(-10, 0.5f);
                        buttonDeleteInstance.Left.Set(0, 0.5f);
                        buttonDeleteInstance.Top.Set(120, 0);
                        buttonDeleteInstance.HAlign = 0;
                        buttonDeleteInstance.Append(icon);
                    }
                }
                self.Height.Pixels -= 40;
            }
        }
        public class Injection_ModList
        {
            #region Fields
            public static FieldInfo buttonB;
            public static FieldInfo buttonOMF;
            public static FieldInfo buttonRM;
            public static FieldInfo buttonCL;
            public static FieldInfo buttonEA;
            public static FieldInfo buttonDA;
            public static FieldInfo uIElement;
            public static FieldInfo ramUsage;
            public static FieldInfo items;
            public static FieldInfo modList;
            public static FieldInfo updateNeeded;

            public static FieldInfo _loaded;
            public static MethodInfo ModName;
            public static MethodInfo enable;
            public static MethodInfo disable;
            #endregion

            public static void Inject()
            {
                var priv = BindingFlags.Instance | BindingFlags.NonPublic;

                _loaded = UIModItem.GetField("_loaded", priv);
                ModName = UIModItem.GetMethod("get_ModName");
                enable = UIModItem.GetMethod("Enable", priv);
                disable = UIModItem.GetMethod("Disable", priv);

                updateNeeded = UIMods.GetField("updateNeeded", priv);
                modList = UIMods.GetField("modList", priv);
                buttonB = UIMods.GetField("buttonB", priv);
                buttonOMF = UIMods.GetField("buttonOMF", priv);
                buttonRM = UIMods.GetField("buttonRM", priv);
                buttonCL = UIMods.GetField("buttonCL", priv);
                buttonEA = UIMods.GetField("buttonEA", priv);
                buttonDA = UIMods.GetField("buttonDA", priv);
                uIElement = UIMods.GetField("uIElement", priv);
                items = UIMods.GetField("items", priv);

                ramUsage = UIMods.GetField("ramUsage", priv);
            }

            #region Methods
            public static int Compare(UIElement left, UIElement right)
            {
                if (!right.GetType().Equals(UIModItem)) return 1;
                if (!left.GetType().Equals(UIModItem))
                {
                    left.Recalculate();
                    left.Height.Set(((List<UIElement>)left.Children)[0].GetOuterDimensions().Height + left.PaddingTop * 2, 0);
                    return -1;
                }

                if (FavouriteMods().Contains((string)ModName.Invoke(right, null))) return 1;
                if (FavouriteMods().Contains((string)ModName.Invoke(left, null))) return -1;

                return left.CompareTo(right);
            }
            public static void Activate(On_UIElement.orig_Activate orig, UIElement self)
            {
                orig(self);
                if (!self.GetType().Equals(UIMods)) return;
                var _ramUsage = ramUsage.GetValue(self) as UIElement;

                self.OnUpdate += OnUpdate;

                var list = modList.GetValue(self) as UIList;

                list.ManualSortMethod = (l) => l.Sort(Compare);

                var buttonEnable = buttonEA.GetValue(self) as UIAutoScaleTextTextPanel<LocalizedText>;
                var buttonDisable = buttonDA.GetValue(self) as UIAutoScaleTextTextPanel<LocalizedText>;
                buttonEnable.Width.Percent = 0.25f;
                buttonDisable.Width.Percent = 0.25f;
                buttonDisable.HAlign = 0;

                // WHY???

                if (_ramUsage.Top.Pixels == 45) return;                 // Nice check)
                _ramUsage.Top.Pixels = 45;

                var topPanel = ((List<UIElement>)((UIMods)self).uIElement.Children)[1];
                topPanel.Top.Pixels = -45;

                var buttonBack = buttonB.GetValue(self) as UIAutoScaleTextTextPanel<LocalizedText>;
                {
                    buttonBack.PaddingTop = buttonBack.PaddingBottom = 1;
                    var icon = new UIImage(AssetButtonBackIcon);
                    icon.VAlign = 0.5f;
                    icon.Width.Pixels = icon.Height.Pixels = 28;
                    icon.Left.Set(-34, 0);
                    icon.ScaleToFit = true;
                    icon.OverrideSamplerState = SamplerState.PointClamp;
                    icon.IgnoresMouseInteraction = true;
                    buttonBack.PaddingLeft = 40;
                    buttonBack.UseInnerDimensions = true;
                    buttonBack.Width.Percent = 0.25f;
                    buttonBack.Append(icon);
                }
                {
                    var search = ((UIMods)self).filterTextBox.Parent;
                    search.Width.Pixels += 150;
                    search.Left.Pixels -= 150;
                }
                var buttonFolder = buttonOMF.GetValue(self) as UIAutoScaleTextTextPanel<LocalizedText>;
                {
                    buttonFolder.PaddingTop = buttonFolder.PaddingBottom = 1;
                    var icon = new UIImage(TextureAssets.Camera[6]);
                    icon.VAlign = 0.5f;
                    icon.Width.Pixels = icon.Height.Pixels = 28;
                    icon.Left.Set(-34, 0);
                    icon.ScaleToFit = true;
                    icon.OverrideSamplerState = SamplerState.PointClamp;
                    icon.IgnoresMouseInteraction = true;
                    buttonFolder.PaddingLeft = 40;
                    buttonFolder.UseInnerDimensions = true;
                    buttonFolder.Width.Percent = 0.25f;
                    buttonFolder.Left.Percent = 0.25f;
                    buttonFolder.HAlign = 0;
                    buttonFolder.Append(icon);
                }
                var viewModBrowser = new UIAutoScaleTextTextPanel<LocalizedText>(Language.GetText("tModLoader.ModsViewInBrowser"));
                {
                    var icon = new UIImage(TextureAssets.Cursors[2]);
                    icon.VAlign = 0.5f;
                    icon.Width.Pixels = icon.Height.Pixels = 28;
                    icon.Left.Set(-34, 0);
                    icon.ScaleToFit = true;
                    icon.OverrideSamplerState = SamplerState.PointClamp;
                    icon.IgnoresMouseInteraction = true;
                    viewModBrowser.PaddingLeft = 40;
                    viewModBrowser.UseInnerDimensions = true;
                    viewModBrowser.Width.Set(-10, 0.25f);
                    viewModBrowser.Left.Percent = 0.75f;
                    viewModBrowser.Top = buttonFolder.Top;
                    viewModBrowser.Height = buttonFolder.Height;
                    viewModBrowser.HAlign = 0;
                    viewModBrowser.VAlign = 1;
                    viewModBrowser.PaddingTop = viewModBrowser.PaddingBottom = 1;
                    viewModBrowser.WithFadedMouseOver();
                    viewModBrowser.OnLeftClick += ViewEnabledMods;
                    viewModBrowser.Append(icon);
                    viewModBrowser.Append(new UIButtonDesc("tModLoader.ModsViewInBrowserInfo", 0));
                    buttonFolder.Parent.Append(viewModBrowser);
                }
                var buttonReload = buttonRM.GetValue(self) as UIAutoScaleTextTextPanel<LocalizedText>;
                {
                    buttonReload.PaddingTop = buttonReload.PaddingBottom = 1;
                    var icon = new UIImage(AssetButtonReloadIcon);
                    icon.VAlign = 0.5f;
                    icon.Width.Pixels = icon.Height.Pixels = 28;
                    icon.Left.Set(-34, 0);
                    icon.ScaleToFit = true;
                    icon.OverrideSamplerState = SamplerState.PointClamp;
                    icon.IgnoresMouseInteraction = true;
                    buttonReload.PaddingLeft = 40;
                    buttonReload.UseInnerDimensions = true;
                    buttonReload.Width.Percent = 0.25f;
                    buttonReload.Left.Percent = 0.25f;
                    buttonReload.HAlign = 0;
                    buttonReload.Top.Set(-65, 0);
                    buttonReload.Append(icon);
                }
                var buttonConfig = buttonCL.GetValue(self) as UIAutoScaleTextTextPanel<LocalizedText>;
                {
                    buttonConfig.PaddingTop = buttonConfig.PaddingBottom = 1;
                    var icon = new UIImage(AssetButtonConfigIcon);
                    icon.VAlign = 0.5f;
                    icon.Width.Pixels = icon.Height.Pixels = 28;
                    icon.Left.Set(-34, 0);
                    icon.ScaleToFit = true;
                    icon.OverrideSamplerState = SamplerState.PointClamp;
                    icon.IgnoresMouseInteraction = true;
                    buttonConfig.PaddingLeft = 40;
                    buttonConfig.UseInnerDimensions = true;
                    buttonConfig.Width.Percent = 0.25f;
                    buttonConfig.Left.Percent = 0;
                    buttonConfig.HAlign = 0;
                    buttonConfig.Top.Set(-65, 0);
                    buttonConfig.Append(icon);
                }
                {
                    buttonEnable.PaddingTop = buttonEnable.PaddingBottom = 1;
                    var icon = new UIImage(AssetButtonEnableIcon);
                    icon.VAlign = 0.5f;
                    icon.Width.Pixels = icon.Height.Pixels = 30;
                    icon.Left.Set(-35, 0);
                    icon.ScaleToFit = true;
                    icon.OverrideSamplerState = SamplerState.PointClamp;
                    icon.IgnoresMouseInteraction = true;
                    buttonEnable.PaddingLeft = 40;
                    buttonEnable.UseInnerDimensions = true;
                    buttonEnable.Width.Percent = 0.25f;
                    buttonEnable.Left.Percent = 0.5f;
                    buttonEnable.HAlign = 0;
                    buttonEnable.Top.Set(-65, 0);
                    buttonEnable.Append(icon);
                    buttonEnable.TextColor = Color.White;
                }
                {
                    buttonDisable.PaddingTop = buttonDisable.PaddingBottom = 1;
                    var icon = new UIImage(TextureAssets.Camera[5]);
                    icon.VAlign = 0.5f;
                    icon.Width.Pixels = icon.Height.Pixels = 28;
                    icon.Left.Set(-34, 0);
                    icon.ScaleToFit = true;
                    icon.OverrideSamplerState = SamplerState.PointClamp;
                    icon.IgnoresMouseInteraction = true;
                    buttonDisable.PaddingLeft = 40;
                    buttonDisable.UseInnerDimensions = true;
                    buttonDisable.Width.Percent = 0.25f;
                    buttonDisable.Left.Percent = 0.75f;
                    buttonDisable.HAlign = 0;
                    buttonDisable.Top.Set(-65, 0);
                    buttonDisable.Append(icon);
                    buttonDisable.TextColor = Color.White;
                }
                var i = ModContent.GetInstance<SmartModManagementConfig>();
                if (i == null) i = new();
                if (i.DoNotDisableByGlobalButton)
                {
                    buttonDisable.OnLeftClick += DoNotDisableMyMod;
                    buttonDisable.Append(new UIButtonDesc("tModLoader.ModsNoDisableSmartModManagement", 0));
                }

                var uiElement = uIElement.GetValue(self) as UIElement;

                var buttonCancel = new UIAutoScaleTextTextPanel<LocalizedText>(Language.GetText("tModLoader.ModsCancel")) { Height = { Pixels = 40 } };
                buttonCancel.WithFadedMouseOver();
                {
                    buttonCancel.PaddingTop = buttonCancel.PaddingBottom = 1;
                    var icon = new UIImage(AssetButtonCancelIcon);
                    icon.VAlign = 0.5f;
                    icon.Width.Pixels = icon.Height.Pixels = 28;
                    icon.Left.Set(-34, 0);
                    icon.ScaleToFit = true;
                    icon.OverrideSamplerState = SamplerState.PointClamp;
                    icon.IgnoresMouseInteraction = true;
                    buttonCancel.PaddingLeft = 40;
                    buttonCancel.UseInnerDimensions = true;
                    buttonCancel.Width.Set(-10, 0.25f);
                    buttonCancel.Left.Percent = 0.5f;
                    buttonCancel.VAlign = 1;
                    buttonCancel.HAlign = 0;
                    buttonCancel.Top.Set(-20, 0);
                    buttonCancel.Append(icon);
                    buttonCancel.Append(new UIButtonDesc("tModLoader.ModsCancelInfo", 1));
                }
                buttonCancel.OnLeftClick += CancelAll;
                uiElement.Append(buttonCancel);

                self.Recalculate();
            }

            private static void OnUpdate(UIElement affectedElement)
            {
                Interface.modsMenu.modListViewPosition = Interface.modsMenu.uIScrollbar.ViewPosition;
            }

            public static void ViewTranslateMods(UIMouseEvent evt, UIElement listeningElement)
            {
                var item = listeningElement.Parent as UIModItem;
                var m = Interface.modsMenu;
                m.modList.Clear();
                var names = item._mod.properties.RefNames(includeWeak: true);
                UIModsFilterResults filterResults = new UIModsFilterResults();
                List<UIModItem> list = m.items.Where(
                    (UIModItem i) => names.Contains(i.ModName)
                ).ToList();

                UIPanel uIPanel = new UIPanel();
                uIPanel.Width.Set(0f, 1f);
                m.modList.Add(uIPanel);

                UIText uIText = new UIText(Language.GetTextValue("tModLoader.ClickToViewAllMods"));
                uIText.Width.Set(0f, 1f);
                uIText.Height.Set(0f, 1f);
                uIText.TextOriginX = 0.5f;
                uIText.TextOriginY = 0.5f;
                uIText.WrappedTextBottomPadding = 0f;
                uIText.IgnoresMouseInteraction = true;
                uIText.Recalculate();
                uIPanel.Append(uIText);
                uIPanel.Height.Set(80, 0f);
                uIPanel.OnLeftClick += (evt, elem) =>
                {
                    Interface.modsMenu.updateNeeded = true;
                };
                UICommon.WithFadedMouseOver(uIPanel);

                m.modList.Add(item);
                m.modList.AddRange(list);
                m.Recalculate();
                m.modList.ViewPosition = 0;
            }
            public static void ViewRequiresMods(UIMouseEvent evt, UIElement listeningElement)
            {
                var item = listeningElement.Parent as UIModItem;
                var m = Interface.modsMenu;
                m.modList.Clear();
                UIModsFilterResults filterResults = new UIModsFilterResults();
                List<UIModItem> list = m.items.Where(
                    (UIModItem i) => (item._modDependencies != null ? item._modDependencies.Contains(i.ModName) : false) || (item._modDependents != null ? item._modDependents.Contains(i.ModName) : false)
                ).ToList();


                UIPanel uIPanel = new UIPanel();
                uIPanel.Width.Set(0f, 1f);
                m.modList.Add(uIPanel);

                UIText uIText = new UIText(Language.GetTextValue("tModLoader.ClickToViewAllMods"));
                uIText.Width.Set(0f, 1f);
                uIText.Height.Set(0f, 1f);
                uIText.TextOriginX = 0.5f;
                uIText.TextOriginY = 0.5f;
                uIText.WrappedTextBottomPadding = 0f;
                uIText.IgnoresMouseInteraction = true;
                uIText.Recalculate();
                uIPanel.Append(uIText);
                uIPanel.Height.Set(80, 0f);
                uIPanel.OnLeftClick += (evt, elem) =>
                {
                    Interface.modsMenu.updateNeeded = true;
                };
                UICommon.WithFadedMouseOver(uIPanel);

                m.modList.Add(item);
                m.modList.AddRange(list);
                m.Recalculate();
                m.modList.ViewPosition = 0;
            }
            public static void ViewEnabledMods(UIMouseEvent evt, UIElement listeningElement)
            {
                var list = new List<ModPubId_t>();
                var mods = ModLoader.Mods;
                foreach (Mod mod in mods)
                {
                    if (mod.File != null)
                    {
                        if (ModOrganizer.TryReadManifest(ModOrganizer.GetParentDir(mod.File.path), out var info))
                        {
                            ModPubId_t result = default;
                            result.m_ModPubId = info.workshopEntryId.ToString();
                            list.Add(result);
                        }
                    }
                }
                Interface.modBrowser.Activate();
                Interface.modBrowser.FilterTextBox.Text = "";
                Interface.modBrowser.SpecialModPackFilter = list;
                Interface.modBrowser.SpecialModPackFilterTitle = Language.GetTextValue("tModLoader.MBFilterModlist");
                Interface.modBrowser.UpdateFilterMode = UpdateFilter.All;
                Interface.modBrowser.ModSideFilterMode = ModSideFilter.All;
                Interface.modBrowser.ResetTagFilters();
                SoundEngine.PlaySound(in SoundID.MenuOpen);
                Interface.modBrowser.PreviousUIState = Interface.modsMenu;
                Main.menuMode = 10007;
            }
            public static void DoNotDisableMyMod(UIMouseEvent evt, UIElement listeningElement)
            {
                foreach (dynamic modItem in (IList)items.GetValue(listeningElement.Parent.Parent))
                {
                    if ((string)ModName.Invoke(modItem, null) == Instance.Name)
                        enable.Invoke(modItem, null);
                }
            }

            public static void CancelAll(UIMouseEvent evt, UIElement listeningElement)
            {
                foreach (dynamic modItem in (IList)items.GetValue(listeningElement.Parent.Parent))
                {
                    if ((bool)_loaded.GetValue(modItem))
                        enable.Invoke(modItem, null);
                    else
                        disable.Invoke(modItem, null);
                }
            }

            public static void ListRecalculate(On_UIList.orig_RecalculateChildren orig, UIList self)
            {
                if (UseConciseModList && self.GetType().Equals(UIConciseUIList))
                {
                    bool haveChange = true;
                    int limit = 30;
                    while (haveChange && limit-- > 0)
                    {
                        haveChange = false;
                        var c = self._items.Count - 1;
                        for (int i = 0; i < c; i++)
                        {
                            if (!self._items[i].GetType().Equals(UIConciseModItem)) continue;

                            var item = self._items[i] as UIModItem;
                            var item2 = self._items[i + 1] as UIModItem;
                            bool flag = !self._items[i + 1].GetType().Equals(UIConciseModItem);
                            if (!flag) flag = !FavouriteMods().Contains(item.ModName) && FavouriteMods().Contains(item2.ModName);
                            if (flag)
                            {
                                var prev = self._items[i];
                                self._items[i] = self._items[i + 1];
                                self._items[i + 1] = prev;
                                haveChange = true;
                                continue;
                            }
                            if (FavouriteMods().Contains(item.ModName)) continue;
                            string displayNameClean = item.DisplayNameClean;
                            string displayNameClean2 = item2.DisplayNameClean;
                            var ind = Interface.modsMenu.sortMode switch
                            {
                                ModsMenuSortMode.RecentlyUpdated => -1 * item._mod.lastModified.CompareTo(item2._mod.lastModified),
                                ModsMenuSortMode.DisplayNameAtoZ => string.Compare(displayNameClean, displayNameClean2, StringComparison.Ordinal),
                                ModsMenuSortMode.DisplayNameZtoA => -1 * string.Compare(displayNameClean, displayNameClean2, StringComparison.Ordinal),
                                _ => 0,
                            };
                            if (ind > 0)
                            {
                                self._items[i] = self._items[i + 1];
                                self._items[i + 1] = item;
                                haveChange = true;
                            }
                        }
                    }
                }
                orig(self);
            }

            #endregion
        }
    }
    public class UIButtonDesc : UIElement // Yes, i want
    {
        public string description;
        public int typo;
        public int timerUpdate = 0;
        public int a1 = 0;
        public int a2 = 0;
        public UIButtonDesc(string desc, int typo)
        {
            description = desc;
            this.typo = typo;
        }
        public override void DrawSelf(SpriteBatch spriteBatch)
        {
            if (Parent.IsMouseHovering)
            {
                switch (typo)
                {
                    case 2:
                        UICommon.TooltipMouseText(description);
                        return;
                    case 1:
                        if (timerUpdate <= 0)
                        {
                            a1 = 0;
                            a2 = 0; 
                            foreach (UIModItem modItem in (IList)SmartModManagement.Injection_ModList.items.GetValue(Parent.Parent.Parent))
                            {
                                if (modItem._loaded && !modItem._mod.Enabled)
                                    a1++;
                                else if (!modItem._loaded && modItem._mod.Enabled)
                                    a2++;
                            }
                            timerUpdate = 120;
                        }
                        UICommon.TooltipMouseText(Language.GetTextValue(description, a1, a2));
                        return;
                    default:
                        UICommon.TooltipMouseText(Language.GetTextValue(description));
                        return;
                }
            }
        }
        public override void Update(GameTime gameTime)
        {
            if (timerUpdate > 0) timerUpdate--;
            base.Update(gameTime);
        }
    }
}