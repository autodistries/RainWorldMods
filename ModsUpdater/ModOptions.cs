using System;
using System.Collections.Generic;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using BepInEx;
using BepInEx.Logging;
using Menu;
using Menu.Remix.MixedUI;
using UnityEngine;
namespace ModsUpdater;

public class ModOptions : OptionInterface
{
    ModsUpdater parent;
    bool WasInitialized = false;
    static ManualLogSource lls;
    private ModOptions.ModsContainer modsContainer;

    private static OpLabel infoLabel;

    public ModsContainer localModsContainer
    {
        get => modsContainer;
    }





    UIelement[] UIArrPlayerOptions;
    public ModOptions(ModsUpdater parent, BepInEx.Logging.ManualLogSource logSource)
    {
        lls = logSource;
        this.parent = parent;

    }

    public static void SetInfoLabel(string text, Color? color = null)
    {
        infoLabel.text = text;
        if (color.HasValue)
        {
            infoLabel.color = color.Value;
        }
    }


    public override async void Initialize()
    {
        base.Initialize();
        WasInitialized = true;

        lls.LogInfo("TemplateModOptions INITIALIZED");

        var opTab = new Menu.Remix.MixedUI.OpTab(this, "Mod Options");
        this.Tabs = new[]
        {
            opTab
        };

        infoLabel = new OpLabel(10f, 530f, "text");



        UIArrPlayerOptions = new UIelement[]
        {
            new OpLabel(10f, 550f, "Mods Updater", true),
            infoLabel
        };


        opTab.AddItems(UIArrPlayerOptions);



        modsContainer = await ModOptions.ModsContainer.CreateAsync(new Vector2(10f, 50f), new Vector2(350f, 470f), 0f, false, true);
        opTab.AddItems(modsContainer);







    }



    internal void SetUpOptionsBindings()
    {
        //Binds the available options
        //Adds them to the global options list so that they can be initiated
        // IsLoggingEnabled = Options.config.Bind<bool>("IsLoggingEnabled", true);
        // Configurables.Add(IsLoggingEnabled, ToggleCustomLogReplacements);

        // pauseButtonConfigurable = Options.config.Bind<KeyCode>("pauseButtonConfigurable", KeyCode.P);
        // pauseButtonConfigurable.info.description = "Key pressed to enter and exit meta-pause";
        // pauseButtonConfigurable.OnChange += () => { keys[0] = pauseButtonConfigurable.Value; };


        // stepButtonConfigurable = Options.config.Bind<KeyCode>("stepButtonConfigurable", KeyCode.L);
        // stepButtonConfigurable.info.description = "Key pressed to step forwards once";
        // stepButtonConfigurable.OnChange += () => { keys[1] = stepButtonConfigurable.Value; };


        // chainStepSpeedConfigurable = Options.config.Bind<float>("chainStepSpeedConfigurable", 0.20f, new ConfigAcceptableRange<float>(0f, 1f));
        // chainStepSpeedConfigurable.info.description = "Speed at which steps chain when holding the step key. 1 = 1/60 updates = 1 update/sec";
        // chainStepSpeedConfigurable.OnChange += () =>
        // {
        //     chainSpeedStep = chainStepSpeedConfigurable.Value;
        // };

    }




    public void AddButtonOption(int num, string text, Menu.Remix.MixedUI.OnSignalHandler updateFunction)
    {
        lls.LogDebug(6);

        if (this.Tabs != default(OpTab[]))
        {
            lls.LogDebug(7);

            float num2 = 380;
            float num3 = 550f;
            OpSimpleButton opSimpleButton = new OpSimpleButton(new Vector2(num2, num3 - num), new Vector2(170f, 36f), text)
            {
                description = text,
            };
            lls.LogDebug(8);

            opSimpleButton.OnClick += updateFunction;

            Tabs[0].AddItems(opSimpleButton);
            lls.LogDebug(9);

        }
        else
        {
            lls.LogError("We tried to add elements to our settings page, but there was no settings page");
        }
    }


    public class ModsContainer : OpScrollBox
    {
        public static List<ServerMod> serverMods = new();
        public static List<ServerMod> localMods = new();
        public static List<ServerMod> updatableMods = new();


        static List<ServerMod> shownMods = new();
        List<OpModButton> internalElements = new();
        ContainerStatus containerStatus = ContainerStatus.None;

        const float BTN_HEIGHT = 25f;
        const float INBETWEEN_PADDING = 2f;
        float currentBtnPosY;
        public float localContentSize = 0f;

        public bool listIsDirty = false;


        public enum ContainerStatus
        {
            None,
            Update,
            Download
        }

        public ContainerStatus CurrentContainerStatus
        {
            get => containerStatus;
            set
            {
                if (value == ContainerStatus.None)
                {
                    lls.LogError("ModsContainer ContainerStatus should never be set to none");
                }
                containerStatus = value;
                listIsDirty = true;
            }
        }

        public static async Task<ModsContainer> CreateAsync(Vector2 pos, Vector2 size, float localContentSize, bool hasBack = true, bool hasSlideBar = true)
        {
            var instance = new ModsContainer(pos, size, localContentSize, hasBack, hasSlideBar);
            await instance.ModsContainerAsync();
            return instance;
        }


        // please create using ModsContainer.CreateAsync
        public ModsContainer(Vector2 pos, Vector2 size, float localContentSize, bool hasBack = true, bool hasSlideBar = true) : base(pos, size, localContentSize, false, hasBack, hasSlideBar)
        {

        }

        public async Task ModsContainerAsync()
        {

            currentBtnPosY = size.y - BTN_HEIGHT;


            foreach (ModManager.Mod mod in ModManager.InstalledMods)
            {
                ModsContainer.localMods.Add(new ServerMod(mod.id, mod.version, mod.path));
            }

            string url = "https://raw.githubusercontent.com/AndrewFM/RainDB/master/raindb.js";
            string targetPath = Path.Combine(ModsUpdater.THISMODPATH, "raindb.js");

            int result = await Utils.FileManager.DownloadFileIfNewerAsync(url, targetPath);
            switch (result)
            {
                case 0:
                    ModOptions.SetInfoLabel("Updated source", Color.gray);
                    break;
                case 1:
                    ModOptions.SetInfoLabel("Local file up-to-date", Color.gray);
                    break;
                case -1:
                    ModOptions.SetInfoLabel("Could not get etag from headers", Color.red);

                    break;
                case -2:
                    ModOptions.SetInfoLabel("Currently offline", Color.red);


                    break;
            }

            string[] lines = File.ReadAllLines(Path.Combine(ModsUpdater.THISMODPATH, "raindb.js"));

            string currentWorkingID = "";
            string currentWorkingVersion = "";
            string currentWorkingLink = "";

            string detectionPattern = @"""(?<key>id|version|url)""\s*:\s*""(?<value>[^""]*)""\s*,?";

            bool skippingThisMod = false;
            foreach (string line in lines.Skip(2))
            {
                if (line == "")
                {
                    if (skippingThisMod) skippingThisMod = false;
                    continue;
                }
                if (skippingThisMod) continue;
                else if (line == "Mods.push({")
                {
                    currentWorkingID = "undefined";
                    currentWorkingLink = "undefined";
                    currentWorkingVersion = "undefined";
                }
                else if (line == "") continue;
                else if (line == "});")
                {
                    //  lls.LogDebug($"adding {currentWorkingID} {currentWorkingVersion} {currentWorkingLink}");
                    serverMods.Add(new ServerMod(currentWorkingID, currentWorkingVersion, currentWorkingLink));
                    if (Utils.VersionManager.IsVersionGreater(localMods.FirstOrDefault((lmod) => lmod.ID == currentWorkingID)?.Version, currentWorkingVersion))
                    {

                        updatableMods.Add(new ServerMod(currentWorkingID, currentWorkingVersion, currentWorkingLink));
                    }
                }
                else
                {
                    var match = System.Text.RegularExpressions.Regex.Match(line, detectionPattern);
                    if (match.Success)
                    {
                        string key = match.Groups["key"].Value;
                        string value = match.Groups["value"].Value;
                        //  lls.LogDebug($"matched {key} {value}");
                        switch (key)
                        {
                            case "id":
                                {
                                    currentWorkingID = value; break;
                                }
                            case "version": currentWorkingVersion = value; break;
                            case "url": currentWorkingLink = value; break;
                        }
                    }
                }
            }
        }

        public void Add(ServerMod serverMod)
        {
            if (isModShown(serverMod.ID))
            {
                ModOptions.lls.LogWarning("Tried to add duplicate mod entry :" + serverMod.ID);
                return;
            }
            shownMods.Add(serverMod);
            var tempModOpBtn = new OpModButton(this, serverMod, new(base.pos.x, currentBtnPosY), new(base.size.x - 5 * 6f, BTN_HEIGHT));
            if (CurrentContainerStatus == ContainerStatus.Update)
            {
                tempModOpBtn.OnClick += (btn) =>
                {
                    Utils.FileManager.GetUpdateAndUnzip((btn as OpModButton).ModLink, (btn as OpModButton).ModLink);
                };
            }
            internalElements.Add(tempModOpBtn);

            // base.SetlocalContentSize( localContentSize);


        }

        public void ShowModsList(List<ServerMod> modsList)
        {
            if (localContentSize != 0f)
            {
                ModOptions.lls.LogError("Please do not add elements to the list when its size has not been reset !");
            }
            foreach (var mod in modsList)
            {
                Add(mod);
            }
            SetContentSize(localContentSize);
        }





        public override void Update()
        {
            base.Update();


            if (listIsDirty)
            {
                this.MarkDirty();
                Reset();
                currentBtnPosY = size.y - BTN_HEIGHT;
                localContentSize = 0f;
                SetContentSize(0f);
                shownMods.Clear();
                internalElements.First().tab.RemoveItems(internalElements.ToArray());

                foreach (var el in internalElements)
                {
                    el.Hide();
                    el.Unload();
                }
                internalElements.Clear();
                items.Clear();



                if (containerStatus == ContainerStatus.Update)
                {
                    lls.LogDebug("in upd mode" + updatableMods.Count);
                    ShowModsList(updatableMods);
                }
                else if (containerStatus == ContainerStatus.Download)
                {
                    lls.LogDebug("in dl mode" + serverMods.Count);

                    ShowModsList(serverMods);
                }
                else
                {
                    lls.LogError("ModsList is not in update nor download mods!");
                }

                listIsDirty = false;

            }

        }

        public bool isModLocal(string modID)
        {
            return localMods.Any((mod) => mod.ID == modID);
        }

        public bool isModShown(string modID)
        {
            return shownMods.Any((mod) => mod.ID == modID);
        }

        public bool isModUpdatable(string modID)
        {
            return updatableMods.Any((mod) => mod.ID == modID);
        }

        public bool isModDownloadable(string modID)
        {
            return !isModLocal(modID);
        }

        public class OpModButton : OpSimpleButton
        {
            private readonly ModsContainer parent;
            private readonly ServerMod modRep;
            ModStatus modStatus = ModStatus.None;
            private float MyPos => 275f + parent.size.y - parent.internalElements.Count * 30f;

            public string ModLink
            {
                get => modRep.Link;
            }





            public OpModButton(ModsContainer modsContainer, ServerMod serverMod, Vector2 pos, Vector2 size, string displayText = "aaa") : base(pos, size, displayText)
            {

                parent = modsContainer;
                modRep = serverMod;

                parent.currentBtnPosY -= BTN_HEIGHT + INBETWEEN_PADDING;

                //parent.AddItems(this);
                //base.pos = new Vector2(parent.pos.x, MyPos);
                if (parent.containerStatus == ContainerStatus.Update)
                {
                    modStatus = ModStatus.Updatable;
                }
                else if (parent.containerStatus == ContainerStatus.Download)
                {
                    if (parent.isModLocal(modRep.ID))
                    {
                        modStatus = ModStatus.Downloaded; // compat?
                    }
                    else
                    {
                        modStatus = ModStatus.Downloadable;
                    }
                }
                description = modRep.ID;
                soundClick = SoundID.None;
                base.text = modRep.ID;
                //colorEdge = default;


                base.OnClick += Signal;
                //base.OnUnload += UnloadUI;

                parent.AddItems(this);
                parent.localContentSize += BTN_HEIGHT + INBETWEEN_PADDING;
            }

            private void Signal(UIfocusable trigger)
            {
                Console.WriteLine("hi, you clieked" + (trigger as OpModButton).modRep.ID);
            }

            public enum ModStatus
            {
                None,
                Updatable,
                Updated,
                Downloadable,
                Downloaded
            }




        }
    }





}
/*
internal class ListSlider : OpSlider, IAmPartOfModList
		{
			private static readonly Configurable<int> _dummy = ModButton.RainWorldDummy.config.Bind("_listSlider", 0);

			private const float _SUBSIZE = 10f;

			private readonly MenuModList _list;

			private readonly FSprite _subtleCircle;

			internal float _floatPos;

			protected internal override bool CurrentlyFocusableMouse => base.CurrentlyFocusableMouse;

			protected internal override bool CurrentlyFocusableNonMouse => false;

			protected internal override bool MouseOver => Custom.DistLess(new Vector2(15f, _subtleCircle.y), base.MousePos, 5f);

			public ListSlider(MenuModList list)
				: base(_dummy, new Vector2(list.pos.x - 30f, list.pos.y), 650, vertical: true)
			{
				_list = list;
				_subtleCircle = new FSprite("Menu_Subtle_Slider_Nob")
				{
					anchorX = 0.5f,
					anchorY = 0.5f
				};
				myContainer.AddChild(_subtleCircle);
				_list.MenuTab.AddItems(this);
				description = OptionalText.GetText(OptionalText.ID.MenuModList_ListSlider_Desc);
			}

			public override void GrafUpdate(float dt)
			{
				base.GrafUpdate(dt);
				_rect.Hide();
				_label.isVisible = false;
				_labelGlow.Hide();
				_lineSprites[0].isVisible = false;
				_lineSprites[3].isVisible = false;
				if (base.Span <= 1)
				{
					_subtleCircle.y = -1000f;
					_lineSprites[1].isVisible = true;
					_lineSprites[1].y = -25f;
					_lineSprites[1].scaleY = base.size.y + 50f;
					_lineSprites[2].isVisible = false;
					return;
				}
				_subtleCircle.x = 15f;
				_subtleCircle.y = Mathf.Clamp(base._mul * ((float)max - ((base.MenuMouseMode && held) ? _floatPos : _list._floatScrollPos)), -15f, base.size.y + 15f);
				_subtleCircle.scale = 1f;
				_subtleCircle.color = _rect.colorEdge;
				_lineSprites[1].isVisible = true;
				_lineSprites[1].y = -25f;
				_lineSprites[2].isVisible = true;
				float num = _subtleCircle.y - 5f;
				_lineSprites[1].scaleY = 25f + num;
				_lineSprites[2].y = base.size.y + 25f;
				_lineSprites[2].scaleY = base.size.y + 25f - (num + 10f);
			}

			public override void Update()
			{
				mousewheelTick = (ModButton._boolExpand ? 1 : 5);
				base.Update();
				if (!ConfigContainer.holdElement || held)
				{
					greyedOut = _list.CfgContainer._Mode == ConfigContainer.Mode.ModConfig;
					if (!held)
					{
						this.SetValueInt(max - _list._scrollPos);
					}
					else if (base.MenuMouseMode)
					{
						_floatPos = (float)max - Mathf.Clamp(base.MousePos.y / base._mul, 0f, max);
						_list._scrollPos = Mathf.RoundToInt(_floatPos);
						_list._ClampScrollPos();
					}
				}
			}
		}*/