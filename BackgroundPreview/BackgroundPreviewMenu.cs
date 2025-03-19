using BepInEx.Logging;
using Menu;
using RWCustom;
using System;
using UnityEngine;




// Access private fields and stuff
using System.Threading.Tasks;
using UnityEngine.Profiling;
using System.Threading;

namespace BackgroundPreview; //this needs to be changed accordingly, if you rename ModName.csproj ! Explanations on top of ModName.csproj

public class BackgroundPreviewMenu : Menu.Menu
{

    private bool lastExitButton;
    private readonly FLabel scenesInfoLabel;
    private bool isScenesInfoShown = true;

    public static ProcessManager.ProcessID previousProcessID;
    readonly global::Options.ControlSetup controlSetup = Custom.rainWorld.options.controls[0];
    public static ManualLogSource Logger { get; internal set; }

    int slideChangeHoldLimiter = 0;

    // HashSet<int> preloadedPreviews = new();

    readonly int maxAllowedReservedMB;
    bool flatMode = false;
    private int flipBookDelay;
    private int splitSceneDelay;
    private int showHelpDelay;

    private int soundControlDelay = 0;
    private bool ShowAll
    {
        get => Options.showAll.Value;
        set => Options.showAll.Value = value;
    }

    private bool ShowBackgrounds
    {
        get => Options.showBackgrounds.Value;
        set => Options.showBackgrounds.Value = value;
    }

    private bool ShowIntro
    {
        get => Options.showIntro.Value;
        set => Options.showIntro.Value = value;
    }

    private bool ShowEndings
    {
        get => Options.showEndings.Value;
        set => Options.showEndings.Value = value;
    }

    private bool ShowPassages
    {
        get => Options.showPassages.Value;
        set => Options.showPassages.Value = value;
    }

    private bool ShowSlugcat
    {
        get => Options.showSlugcat.Value;
        set => Options.showSlugcat.Value = value;
    }

    private bool ShowDreams
    {
        get => Options.showDreams.Value;
        set => Options.showDreams.Value = value;
    }

    private int pickerLimit;
    // this.Translate exists
    private string HelpInfo => @$"Viewing {ModMainClass.lastSelectedBkg}
-------------------
Exit preview | Esc
Toggle info | H
Paginate scenes | Left, Right
Next slide ({scene.crossFadeInd + 1}/{scene.crossFades.Count}) | S
Next flipbook ({!(scene.scribbleA == null || scene.scribbleB == null)}) | F
Toggle flatmode ({flatMode}) | T
Reload scene | R

Allow all ({ShowAll}) | 0
Allow backgrounds ({ShowBackgrounds}) | 1
Allow intros ({ShowIntro}) | 2
Allow endings ({ShowEndings}) | 3
Allow passages ({ShowPassages}) | 4
Allow slugcats ({ShowSlugcat}) | 5
Allow dreams ({ShowDreams}) | 6

Toggle background noise | N
Force-free memory | M
Used memory: {Profiler.GetTotalReservedMemoryLong() / 1024 / 1024}MB
Max memory: {maxAllowedReservedMB}MB";



    public BackgroundPreviewMenu(ProcessManager manager) : base(manager, ProcessID.BackgroundPreviewID)
    {
        mySoundLoopID = SoundID.MENU_Main_Menu_LOOP;
        manager.menuesMouseMode = false;

        pages.Add(new Menu.Page(this, null, "main", 0));

        // Logger.LogDebug(string.Join("\n", MenuScene.SceneID.values.entries));

        // if (Environment.Is64BitProcess) maxAllowedReservedMB = 4500;
        // else
        maxAllowedReservedMB = Mathf.Min((int)(Profiler.GetTotalReservedMemoryLong() / 1024 / 1024) + 1000, 2400); ;

        scene = new InteractiveMenuScene(this, pages[0], ModMainClass.lastSelectedBkg);
        pages[0].subObjects.Add(scene);

        // preloadedPreviews.Add(ModMainClass.lastSelectedBkg.Index);
        // PreloadScenesAsync();


        scenesInfoLabel = new FLabel(Custom.GetFont(), HelpInfo)
        {
            x = manager.rainWorld.options.ScreenSize.x - 20f + (1366f - manager.rainWorld.options.ScreenSize.x) / 2f,//manager.rainWorld.screenSize.x / 2f + 0.01f,
            y = manager.rainWorld.options.ScreenSize.y - 160f, //Mathf.Max(0.01f + manager.rainWorld.options.SafeScreenOffset.y, 20.01f),
            //new Vector2(manager.rainWorld.options.ScreenSize.x - 50f + (1366f - manager.rainWorld.options.ScreenSize.x) / 2f, manager.rainWorld.options.ScreenSize.y - 15f)
            alpha = 1f,
            color = new Color(0.8f, 0.8f, 0.8f),
            alignment = FLabelAlignment.Right

        };
        Futile.stage.AddChild(scenesInfoLabel);

    }




    public override void Update()
    {
        try
        {
            base.Update();
        }
        catch
        {
            Logger.LogError("Could not update, probably becaus of flatmode");
            flatMode = false;
            ChangeScene(0);
        }
        if (manager.menuesMouseMode) manager.menuesMouseMode = false;
        bool flag = RWInput.CheckPauseButton(0);
        if (flag && !lastExitButton && manager.dialog == null)
        {
        Futile.stage.RemoveChild(scenesInfoLabel);

            ModMainClass.options._SaveConfigFile();
            manager.RequestMainProcessSwitch(previousProcessID);
            return;
        }
        else lastExitButton = flag;

        float xAxis = controlSetup.GetAxis(6);
        if (xAxis != 0 && slideChangeHoldLimiter == 0)
        {
            slideChangeHoldLimiter = 15;
            ChangeScene((int)xAxis);
            // right is 1, left is -1
        }
        //controlsetup.getaxis(6) is x axis

        if (Input.GetKey("s") && splitSceneDelay == 0) // 
        {
            if (scene.crossFades.Count > 1 && scene.crossFadeInd + 1 == scene.crossFades.Count)
            {
                ChangeScene(0);
            }
            else scene.TriggerCrossfade(10);

            splitSceneDelay = 15;
        }
        if (Input.GetKey("f") && flipBookDelay == 0) { scene.FlipScribble(); flipBookDelay = 7; }
        if (Input.GetKey("h") && showHelpDelay == 0)
        {
            isScenesInfoShown = !isScenesInfoShown;
            if (isScenesInfoShown)
            {
                scenesInfoLabel.alpha = 1;
            }
            else
            {
                scenesInfoLabel.alpha = 0;
            }
            showHelpDelay = 10;
        }
        if (Input.GetKeyDown("r")) ChangeScene(0);
        if (Input.GetKeyDown("m"))
        {
            AssetManager.HardCleanFutileAssets();
            ChangeScene(0);
            new Task(() => { Thread.Sleep(1000); scenesInfoLabel.text = HelpInfo; });
        }
        if (Input.GetKeyDown("t"))
        {
            flatMode = !flatMode;
            ChangeScene(0);
        }

        if (Input.GetKey("n") && soundControlDelay == 0)
        {

            if (mySoundLoopID == SoundID.MENU_Main_Menu_LOOP)
            {
                manager.musicPlayer?.FadeOutAllSongs(30f);
                soundLoop.Destroy();
                mySoundLoopID = SoundID.None;
                soundLoop = null;
                Logger.LogInfo("Removed background noises");
            }
            else
            {
                mySoundLoopID = SoundID.MENU_Main_Menu_LOOP;
				soundLoop = PlayLoop(mySoundLoopID, 0f, 1f, 1f, isBkgLoop: true);
                Logger.LogInfo("Added background noises");

            }
            soundControlDelay = 10;
        }


        if (pickerLimit == 0)
        {
            if (Input.GetKey("1"))
            {
                ShowBackgrounds = !ShowBackgrounds;
                pickerLimit = 8;
            }
            if (Input.GetKey("2"))
            {
                ShowIntro = !ShowIntro;
                pickerLimit = 8;
            }
            if (Input.GetKey("3"))
            {
                ShowEndings = !ShowEndings;
                pickerLimit = 8;
            }
            if (Input.GetKey("4"))
            {
                ShowPassages = !ShowPassages;
                pickerLimit = 8;
            }
            if (Input.GetKey("5"))
            {
                ShowSlugcat = !ShowSlugcat;
                pickerLimit = 8;
            }
            if (Input.GetKey("6"))
            {
                ShowDreams = !ShowDreams;
                pickerLimit = 8;
            }
            if (Input.GetKey("0"))
            {
                ShowAll = !ShowAll;
                pickerLimit = 8;
            }
            // if (Input.GetKey("1"))
            // {
            //     showBackgrounds = true;
            //     pickerLimit = 8;
            // }
        }
        // support doing the fades
        // also timer > and < 0 does things
        if (xAxis == 0) slideChangeHoldLimiter = 0;
        if (slideChangeHoldLimiter != 0) slideChangeHoldLimiter--;
        if (flipBookDelay != 0) flipBookDelay--;
        if (splitSceneDelay != 0) splitSceneDelay--;
        if (showHelpDelay != 0) showHelpDelay--;
        if (pickerLimit != 0) pickerLimit--;
        if (soundControlDelay != 0) soundControlDelay--;

        if (Input.anyKey && scenesInfoLabel.alpha != 0) scenesInfoLabel.text = HelpInfo;
    }

    // public override void GrafUpdate(float timeStacker)
    // {
    //     base.GrafUpdate(timeStacker);
    //     if (scenesInfoLabel != null)
    //     {
    //         if (scenesInfoLabel.alpha > 0)
    //         {
    //             isScenesInfoShown--;
    //             float newAlpha = Mathf.InverseLerp(0, 100, isScenesInfoShown);
    //             scenesInfoLabel.alpha = Mathf.Lerp(scenesInfoLabel.alpha, newAlpha, timeStacker);
    //         }
    //     }
    // }



    public void ChangeScene(int direction)
    {
        var nextSceneStr = FindValidSceneName(direction);


        Logger.LogInfo("Next scene is " + nextSceneStr);
        if (ExtEnumBase.TryParse(typeof(MenuScene.SceneID), nextSceneStr, true, out var result))
        {

            scene.RemoveSprites();
            scene.UnloadImages();
            pages[0].RemoveSubObject(scene);

            float lastUsedMem = Profiler.GetTotalReservedMemoryLong() / 1024 / 1024;

            if (lastUsedMem > maxAllowedReservedMB)
            {
                Logger.LogInfo($"Forcing clearing textures ! RAM is {lastUsedMem} > {maxAllowedReservedMB}");
                AssetManager.HardCleanFutileAssets();
            }

            // Logger.LogInfo("Yippee. FOund that scene somewhere. It is " + result);
            ModMainClass.lastSelectedBkg = (MenuScene.SceneID)result;
            try
            {
                scene = new InteractiveMenuScene(this, pages[0], (MenuScene.SceneID)result)
                {
                    timer = 0,
                    flatMode = flatMode
                };

            }
            catch (Exception ex)
            {
                Logger.LogError("Could not load this scene !\n" + ex);
            }
            pages[0].subObjects.Add(scene);
            scenesInfoLabel.text = HelpInfo;
            // PreloadScenesAsync();
        }
        else
        {
            Logger.LogInfo("No such scene??");
        }

    }

    private string FindValidSceneName(int direction)
    {
        if (direction==0) return ModMainClass.lastSelectedBkg.ToString();
        int nextSceneIndex = ModMainClass.lastSelectedBkg.Index; // current index
        nextSceneIndex = (nextSceneIndex + direction + MenuScene.SceneID.values.Count) % MenuScene.SceneID.values.Count;

        while (true && nextSceneIndex != ModMainClass.lastSelectedBkg.Index)
        {
            string sceneName = MenuScene.SceneID.values.entries[nextSceneIndex];
            if (ShowAll) return sceneName; ;
            if (ShowBackgrounds && (sceneName.ToLower().Contains("mainmenu") || sceneName.ToLower().Contains("landscape"))) return sceneName; ;
            if (ShowEndings && !sceneName.Contains("Endgame") && (sceneName.Contains("End") || sceneName.Contains("Outro"))) return sceneName; ;
            if (ShowIntro && sceneName.ToLower().Contains("intro")) return sceneName; ;
            if (ShowPassages && sceneName.ToLower().Contains("endgame")) return sceneName; ;
            if (ShowPassages && sceneName.ToLower().Contains("endgame")) return sceneName; ;
            if (ShowSlugcat && sceneName.ToLower().Contains("slugcat")) return sceneName; ;
            if (ShowDreams && sceneName.ToLower().Contains("dream")) return sceneName; ;
            nextSceneIndex = (nextSceneIndex + direction + MenuScene.SceneID.values.Count) % MenuScene.SceneID.values.Count;
        }
        return "Empty";// = MenuScene.SceneID.values.entries[nextSceneIndex];
    }

    //     public async Task PreloadScenesAsync()
    // {
    //     int idx = ModMainClass.lastSelectedBkg.Index;
    //     int prevIdx = (idx - 1 + MenuScene.SceneID.values.Count) % MenuScene.SceneID.values.Count;
    //     int nextIdx = (idx + 1) % MenuScene.SceneID.values.Count;

    //     await Task.Run(() =>
    //     {
    //         lock (_preloadLock)
    //         {
    //             if (!preloadedPreviews[prevIdx])
    //             {
    //                 var prevSceneStr = MenuScene.SceneID.values.entries[prevIdx];
    //                 if (ExtEnumBase.TryParse(typeof(MenuScene.SceneID), prevSceneStr, true, out var prevResult))
    //                 {
    //                     Logger.LogInfo($"Preloading previous {prevSceneStr} idx {prevIdx}");
    //                     new InteractiveMenuScene(this, pages[0], (MenuScene.SceneID)prevResult);
    //                     preloadedPreviews[prevIdx] = true;
    //                 }
    //             }

    //             if (!preloadedPreviews[nextIdx])
    //             {
    //                 var nextSceneStr = MenuScene.SceneID.values.entries[nextIdx];
    //                 if (ExtEnumBase.TryParse(typeof(MenuScene.SceneID), nextSceneStr, true, out var nextResult))
    //                 {
    //                     Logger.LogInfo($"Preloading next {nextSceneStr} idx {nextIdx}");
    //                     new InteractiveMenuScene(this, pages[0], (MenuScene.SceneID)nextResult);
    //                     preloadedPreviews[nextIdx] = true;
    //                 }
    //             }
    //         }
    //     });
    // }
    public override void ShutDownProcess()
    {
        base.ShutDownProcess();
    }

    public class ProcessID
    {
        public static ProcessManager.ProcessID BackgroundPreviewID;

        public static void RegisterValues()
        {
            BackgroundPreviewID = new ProcessManager.ProcessID("BackgroundPreviewID", register: true);
        }

        public static void UnregisterValues()
        {
            BackgroundPreviewID?.Unregister();
            BackgroundPreviewID = null;
        }
    }

}