using System;
using System.Data.SqlTypes;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using BepInEx.Logging;
using Menu.Remix.MixedUI;
using UnityEngine;

namespace PathTracer;

public class ModOptions : OptionInterface
{
    private readonly ManualLogSource Logger;
    static public Configurable<bool> doRecordData;
    static public Configurable<bool> doShowData;

    public static Configurable<KeyCode> toggleShowDataKeybind;

    // static public Configurable<bool> doWriteData;
    static public Configurable<bool> doSpeedColorData;
    static public Configurable<bool> doRedColor;
    // static public Configurable<bool> doRecordSlugpupData;
    // static public Configurable<int> maxRoomsToRememberPerRegion;
    // static ConfigAcceptableRange<int> maxRoomsRange = new(0, 30);
    static ConfigAcceptableRange<float> lineWidthRange = new(0.5f, 3.5f);
    static public Configurable<float> lineWidth;

    static public Configurable<int> maxCyclesToRemember;
    static ConfigAcceptableRange<int> maxCyclesRange = new(0, 6);
    static public Configurable<int> minTicksToRecordPoint;
    static ConfigAcceptableRange<int> minTicksRange = new(5, 80);
    static public Configurable<int> minDistanceToRecordPointTimes100;
    static ConfigAcceptableRange<int> minDistRange = new(1, 100);
    static public Configurable<int> positionCullingPrecisionTimes1000;
    static ConfigAcceptableRange<int> precisionRange = new(1, 2000);

        static public Configurable<int> minQtyOfAccuratePositions;
    static ConfigAcceptableRange<int> qtyPosRange = new(10, 2000);





    // button open file
    // textbox with data résumé
    // button delete data


    private float decalage;

    private float Decalage(bool nextLine = false, bool box = false, bool slider = false, bool nextBtn = false)
    {
        float bv = decalage;
        if (box) bv -= 2;
        if (slider) bv -= 8;
        if (nextLine) decalage -= 28;
        if (nextBtn) decalage -= 40;
        return bv;
    }


    public bool WasInitialized = false;
    public ModOptions(ManualLogSource logger)
    {
        Logger = logger;
        Logger.LogInfo("Mod Options instanciated");

        doRecordData = config.Bind("doRecordData", true);
        doShowData = config.Bind("doShowData", true); 
        toggleShowDataKeybind = config.Bind("toggleShowDataKeybind", KeyCode.T); 
        // doWriteData = config.Bind("doWriteData", true);
        // doRecordSlugpupData = config.Bind("doClearDataOnNewCycle", true);
        doSpeedColorData = config.Bind("doSpeedColorData", true);
        doRedColor = config.Bind("doRedColor", false);
        lineWidth = config.Bind("lineWidth", 2f, lineWidthRange);
        // maxRoomsToRememberPerRegion = config.Bind("maxRoomsToRememberPerRegion", 8, maxRoomsRange);
        maxCyclesToRemember = config.Bind("maxCyclesToRemember", 2, maxCyclesRange);
        minTicksToRecordPoint = config.Bind("minTicksToRecordPoint", 20, minTicksRange);
        minDistanceToRecordPointTimes100 = config.Bind("minDistanceToRecordPointTimes100", 8, minDistRange);
        positionCullingPrecisionTimes1000 = config.Bind("positionCullingPrecisionTimes1000", 1500, precisionRange);
        minQtyOfAccuratePositions  = config.Bind("minQtyOfAccuratePositions", 400, qtyPosRange);
        Logger.LogInfo("Configurables binded ");

    }
    public override void Initialize()
    {
        decalage = 492;
        WasInitialized = true;
        Logger.LogInfo("Mod Options INITIALIZED");

        var opTab = new Menu.Remix.MixedUI.OpTab(this, "PathTracerOptions");
        this.Tabs = new[]
        {
            opTab
        };

        var recordDataLabel = new OpLabel(43f, Decalage(), "Enable recording data from slugcat");
        var recordDataBox = new OpCheckBox(doRecordData, new Vector2(10f, Decalage(box: true, nextLine: true)))
        {
            description = $"Record data of slugcats' position when moving"
        };

        var showDataLabel = new OpLabel(43f, Decalage(), "Enable showing data on maps");
        var showDataBox = new OpCheckBox(doShowData, new Vector2(10f, Decalage()))
        {
            description = $"Show path data when any map is open. Also linked to the keybind"
        };

        OpKeyBinder opKeyBinder = new OpKeyBinder(toggleShowDataKeybind, new Vector2(230, Decalage(box: true, nextLine: true)-4), new Vector2(20, 20))
        {
            description = "The keybind which will toggle showing data on map"
        };


        var speedColorLabel = new OpLabel(43f, Decalage(), "Color path according to speed");
        var speedColorBox = new OpCheckBox(doSpeedColorData, new Vector2(10f, Decalage(box: true, nextLine: true)))
        {
            description = $"When there is only one player, show the current cycle's path with a color gradient green (slow) -> red (fast)\nOverrides the Red Color"
        };

        var redColorLabel = new OpLabel(43f, Decalage(), "Color path in red only, if alone");
        var redColorBox = new OpCheckBox(doRedColor, new Vector2(10f, Decalage(box: true, nextLine: true)))
        {
            description = $"When there is only one player, every path in red only for visibility. "
        };

        // var singleCycleDataLabel = new OpLabel(43f, Decalage(), "Enable to record data for slugpups");
        // var singleCycleDataBox = new OpCheckBox(doRecordSlugpupData, new Vector2(10f, Decalage(box: true, nextLine: true)))
        // {
        //     description = $"Also record data of your slugpups"
        // };

      



        var lineWidthLabel = new OpLabel(10f, Decalage(), "Line width");
        var lineWidthSlider = new OpFloatSlider(lineWidth, new Vector2(200f, Decalage(slider: true, nextLine: true)), 240)
        {
            description = $"Width of lines drawn on map"
        };


        var maxCyclesToRememberLabel = new OpLabel(10f, Decalage(), "Max cycles to retain positions");
        var maxCyclesToRememberSlider = new OpSlider(maxCyclesToRemember, new Vector2(200f, Decalage(slider: true, nextLine: true)), 240)
        {
            description = $"Maximum number of cycles positions will be remembered for. 0 means clear on new cycle. Their opacity will be reduced over time"
        };

        // var maxRoomsPerRegionLabel = new OpLabel(10f, Decalage(), "Max rooms per region");
        // var maxRoomsPerRegionSlider = new OpSlider(maxRoomsToRememberPerRegion, new Vector2(200f, Decalage(slider: true, nextLine: true)), 240)
        // {
        //     description = $"Maximum number of different rooms to keep data from, per region and per slugcat. 0 to not limit rooms"
        // };


        var minTicksLabel = new OpLabel(10f, Decalage(), "Minimum ticks per pos");
        var minTicksSlider = new OpSlider(minTicksToRecordPoint, new Vector2(200f, Decalage(slider: true, nextLine: true)), 240)
        {
            description = $"Minimum amount of ticks required to save a position"
        };


        var minDistLabel = new OpLabel(10f, Decalage(), "Minimum distance per pos");
        var minDistSlider = new OpSlider(minDistanceToRecordPointTimes100, new Vector2(200f, Decalage(slider: true, nextLine: true)), 240)
        {
            description = $"Minimum distance required for saving a position. Actual value vill be /100",
        };

        var posPrecLabel = new OpLabel(10f, Decalage(), "Linear data culling");
        var posPrecSlider = new OpSlider(positionCullingPrecisionTimes1000, new Vector2(200f, Decalage(slider: true, nextLine: true)), 240)
        {
            description = $"When going in a staight line, intermediate points are culled\nThis decides the precision of what is considered straight.\nDecrease only if you don't like the current result ! /1000",
        };

          
        var posQtyLabel = new OpLabel(10f, Decalage(), "Old sparse data culling");
        var posQtySlider = new OpSlider(minQtyOfAccuratePositions, new Vector2(200f, Decalage(slider: true, nextLine: true)), 240)
        {
            description = $"Regularly, positions created more than this amount of positions will be made sparse\nEach old room will be set to cap on a minimal amount of positions\nOverall trajectory should remain readable",
        };

        // var openFileBtn = new OpSimpleButton(new(10f, Decalage(nextBtn:true)), new(120, 30), "Open folder") {
        //     description = "Open the folder where tracker.json is stored"
        // };
        // openFileBtn.OnClick += (_) =>
        // {
        //     Console.WriteLine("OPen btn cliekced");
        //     if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        //     {
        //         Process.Start(new ProcessStartInfo("explorer.exe", $"/select,{MetaPathStore.targetStorageFile}") { UseShellExecute = true });
        //     }
        //     // these two are never happening huh, when linux port???!,!,,,
        //     else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        //     {
        //         Process.Start(new ProcessStartInfo("xdg-open", MetaPathStore.targetStorageFile) { UseShellExecute = true });
        //     }
        //     else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        //     {
        //         Process.Start(new ProcessStartInfo("open", $"-R {MetaPathStore.targetStorageFile}") { UseShellExecute = true });
        //     }
        // };


        // var deleteFileBtn = new OpHoldButton(new(10f, Decalage(nextBtn: true)), new Vector2(120, 30), "Delete tracker.json", 80) {
        //     description = "Delete the tracker.json. This is irreversible ! Data loaded to memory will also be cleared."
        // };
        // // button open file
        // // textbox with data résumé
        // // button delete data

        // var dataRésuméTextBox = new OpLabelLong(new(10f, 10f), new Vector2(350, 100), "Loading the data overview...") {
        //     autoWrap = false
        // };


        // var dataRésuméScrollBox =  new OpScrollBox(new Vector2(5f, Decalage() - 200f), new Vector2(590f, 200f), dataRésuméTextBox.size.y + 10f);

        // deleteFileBtn.OnPressDone += (_) =>
        // {
        //     if (File.Exists(MetaPathStore.targetStorageFile)) File.Delete(MetaPathStore.targetStorageFile);
        //     MetaPathStore.ResetData();
        //     ModMainClass.path.SetNewPositions(new());
        //     Console.WriteLine("Deleteed tracker.json");
        //     FireUpdateRésuméBox(dataRésuméTextBox, dataRésuméScrollBox);
        // };
        UIelement[] UIArrPlayerOptions = new UIelement[]
        {
            new OpLabel(10f, 550f, "Path Tracer Options", true),
            recordDataLabel,
            recordDataBox,

            showDataLabel,
            showDataBox,

            opKeyBinder,

            speedColorLabel,
            speedColorBox,

            redColorLabel,
            redColorBox,


            lineWidthLabel,
            lineWidthSlider,
            // writeDataLabel,
            // writeDataBox,
            maxCyclesToRememberLabel,
            maxCyclesToRememberSlider,

            // maxRoomsPerRegionLabel,
            // maxRoomsPerRegionSlider,
            posQtyLabel,
posQtySlider,


            // singleCycleDataLabel,
            // singleCycleDataBox,

            minTicksLabel,
            minTicksSlider,

            minDistLabel,
            minDistSlider,

            posPrecLabel,
            posPrecSlider,

            // openFileBtn,
            // deleteFileBtn,

            // dataRésuméScrollBox

        };

        // FireUpdateRésuméBox(dataRésuméTextBox, dataRésuméScrollBox);

        opTab.AddItems(UIArrPlayerOptions);
        // dataRésuméScrollBox.AddItems(dataRésuméTextBox);
    }

}