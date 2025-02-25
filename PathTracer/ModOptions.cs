using BepInEx.Logging;
using Menu.Remix.MixedUI;
using UnityEngine;

namespace PathTracer;

public class ModOptions : OptionInterface
{
    private readonly ManualLogSource Logger;
    static public Configurable<bool> doRecordData;
    static public Configurable<bool> doShowData;
    static public Configurable<int> maxRoomsToRememberPerRegion;
    static public ConfigAcceptableRange<int> maxRoomsRange = new(2,30);
    static public Configurable<int> minTicksToRecordPoint;
    static public ConfigAcceptableRange<int> minTicksRange = new(5,80);
    static public Configurable<float> minDistanceToRecordPoint;
    static public ConfigAcceptableRange<float> minDistRange = new(0.05f,1f);

    //  Checkbox enable mod
    // int slider max rooms backwards in each region
    // float slider minimum distance to record point
    // int slider minimum ticks between points

    // button open file
    // textbox with data résumé
    // button delete data


    private float decalage = 492;

    private float Decalage(bool nextLine = false, bool box = false, bool slider = false) {
        float bv = decalage;
        if (box) bv-=2;
        if (slider) bv-=8;
        if (nextLine) decalage-=28;
        return bv;
    }


    public bool WasInitialized = false;
    public ModOptions(ManualLogSource logger)
    {
        Logger = logger;
        Logger.LogInfo("Mod Options instanciated");

        doRecordData = config.Bind("doRecordData", true);
        doShowData = config.Bind("doShowData", true);
        maxRoomsToRememberPerRegion = config.Bind("maxRoomsToRememberPerRegion", 8, maxRoomsRange);
        minTicksToRecordPoint = config.Bind("minTicksToRecordPoint", 20, minTicksRange);
        minDistanceToRecordPoint = config.Bind("minDistanceToRecordPoint", 0.03f, minDistRange);
        Logger.LogInfo("Configurables binded ");
    }
    public override void Initialize()
    {
        WasInitialized = true;
        Logger.LogInfo("Mod Options INITIALIZED");

        var opTab = new Menu.Remix.MixedUI.OpTab(this, "PathTracerOptions");
        this.Tabs = new[]
        {
            opTab
        };

        var recordDataLabel = new OpLabel(43f, Decalage(), "Enable recording data");
        var recordDataBox = new OpCheckBox(doRecordData, new Vector2(10f, Decalage(box: true, nextLine: true)))  {
                description = $"Record data of slugcats when moving"
            };

        var showDataLabel = new OpLabel(43f, Decalage(), "Enable showing data");
        var showDataBox = new OpCheckBox(doShowData, new Vector2(10f, Decalage(box: true, nextLine:true)))  {
            description = $"Show path data when any map is open"
        };

        var maxRoomsPerRegionLabel = new OpLabel(10f, Decalage(), "Max rooms per region");
        var maxRoomsPerRegionSlider = new OpSliderTick(maxRoomsToRememberPerRegion, new Vector2(180f, Decalage(slider: true, nextLine: true)), 240)
        {
            description = $"Maximum number of different rooms to keep data from, per region, per slugcat, and per save slot"
        };

        var minTicksLabel = new OpLabel(10f, Decalage(), "Minimum ticks");
        var minTicksSlider = new OpSlider(minTicksToRecordPoint, new Vector2(180f, Decalage(slider: true, nextLine: true)), 240)
        {
            description = $"Minimum amount of ticks required to save a position"
        };

        var minDistLabel = new OpLabel(10f, Decalage(), "Minimum distance");
        var minDistSlider = new OpFloatSlider(minDistanceToRecordPoint, new Vector2(180f, Decalage(slider: true, nextLine: true)), 240)
        {
            description = $"Minimum distance required for saving a position"
        };



        UIelement[] UIArrPlayerOptions = new UIelement[]
        {
            new OpLabel(10f, 550f, "Path Tracer Options", true),
            recordDataLabel,
            recordDataBox,

            showDataLabel, 
            showDataBox,

            maxRoomsPerRegionLabel,
            maxRoomsPerRegionSlider,

            minTicksLabel,
            minTicksSlider,

            minDistLabel,
            minDistSlider
        };

        opTab.AddItems(UIArrPlayerOptions);
    }

}