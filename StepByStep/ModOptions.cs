using Menu.Remix.MixedUI;
using UnityEngine;
namespace StepByStep;

using System.Collections.Generic;
using BepInEx.Logging;




public class ModOptions : OptionInterface
{
    public Dictionary<Configurable<bool>, SomeUpdateFunction> Configurables;


    private Menu.Remix.MixedUI.UIelement[] UIArrPlayerOptions;

    public bool WasInitialized = false;
    ManualLogSource lls;
    float decalage;
    StepByStep modInstance;

    public ModOptions(StepByStep givenModInstance, BepInEx.Logging.ManualLogSource logSource)
    {
        lls = logSource;
        modInstance = givenModInstance;

    }
    public override void Initialize()
    {
        base.Initialize();
        WasInitialized = true;

        lls.LogInfo("TemplateModOptions INITIALIZED");
        decalage = 492f;

        var opTab = new Menu.Remix.MixedUI.OpTab(this, "Step By Step Options");
        this.Tabs = new[]
        {
            opTab
        };



        UIArrPlayerOptions = new UIelement[]
        {
            new OpLabel(10f, 550f, "Step By Step Options", true),
                   };

        opTab.AddItems(UIArrPlayerOptions);
        modInstance.SetUpOptionsSettings();
    }

    public void AddKeyBindOption(Configurable<KeyCode> configurable, int num, string quickDesc)
    {

        if (this.Tabs != default(OpTab[]))
        {

            float num2 = 20f;
            float num3 = 550f;
            OpKeyBinder opKeyBinder = new OpKeyBinder(configurable, new Vector2(num2, num3 - num), new Vector2(20, 20))
            {
                description = configurable.info.description
            };
            UIfocusable.MutualVerticalFocusableBind(opKeyBinder, opKeyBinder);
            OpLabel opLabel2 = new OpLabel(new Vector2(40 + num2, num3 - num), new Vector2(170f, 36f), quickDesc, FLabelAlignment.Left, bigText: false)
            {
                description = opKeyBinder.description
            };
            Tabs[0].AddItems(opKeyBinder, opLabel2);
        }

        else
        {
            lls.LogError("We tried to add elements to our settings page, but there was no settings page");
        }
    }

    public void AddSliderOption(Configurable<float> configurable, int num, string quickDesc)
    {
        if (this.Tabs != default(OpTab[]))
        {
            float num2 = 20f;
            float num3 = 550f;
            OpFloatSlider opFloatSlider = new OpFloatSlider(configurable, new Vector2(num2, num3 - num), 150)
            {
                description = configurable.info.description
            };
            OpLabel opLabel = new OpLabel(new Vector2(190 + num2, num3 - num), new Vector2(170f, 36f), quickDesc, FLabelAlignment.Left, bigText: false)
            {
                description = opFloatSlider.description
            };
            Tabs[0].AddItems(opFloatSlider, opLabel);
        }
        else
        {
            lls.LogError("We tried to add elements to our settings page, but there was no settings page");
        }
    }
    



    public delegate void SomeUpdateFunction(Configurable<bool> configurable);
}