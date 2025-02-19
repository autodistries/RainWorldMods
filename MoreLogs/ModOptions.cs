using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using Menu.Remix.MixedUI;
using UnityEngine;

namespace MoreLogs;

public class ModOptions : OptionInterface
{
    private readonly ManualLogSource LogSource;

    public MoreLogger localMoreLogsInstance;
    float decalage;




    public Dictionary<Configurable<bool>, SomeUpdateFunction> Configurables;

    public OpLabel statusInfoLabel = null;


    private Menu.Remix.MixedUI.UIelement[] UIArrPlayerOptions;

    public bool WasInitialized = false;
    public ModOptions(MoreLogger modInstance, ManualLogSource loggerSource)
    {
        loggerSource.LogInfo("TemplateModOptions instanciated");
        LogSource = loggerSource;

    }
    public override void Initialize()
    {
        WasInitialized = true;

        LogSource.LogInfo("TemplateModOptions INITIALIZED");
        decalage = 492f;

        var opTab = new Menu.Remix.MixedUI.OpTab(this, "More Logs Options");
        this.Tabs = new[]
        {
            opTab
        };



        UIArrPlayerOptions = new UIelement[]
        {
            new OpLabel(10f, 550f, "More Logs Options", true),
                   };

        opTab.AddItems(UIArrPlayerOptions);
        localMoreLogsInstance.SetUpOptionsSettings();
    }

    public void AddBoolOption(Configurable<bool> configurable, string text, SomeUpdateFunction callback = null, string desc = null)
    {

        if (this.Tabs != default(OpTab[]))
        {
            callback = callback ?? TestCallback;
            OpLabel localLabel = new OpLabel(43f, decalage, text);
            OpCheckBox localCheckBox = new OpCheckBox(configurable, new Vector2(10f, decalage - 2f)) { description = desc };
            configurable.OnChange += () =>
            {
                callback(configurable);
            };



            UIelement[] newUiElement = new UIelement[]{
                localLabel,
                localCheckBox
            };


            this.Tabs[0].AddItems(newUiElement);
            decalage -= 27f; // Move "cursor" for next option

        }
        else
        {
            LogSource.LogError("We tried to add elements to our settings page, but there was no settings page");
        }
    }


        public void SeyupBepinCfg()
    {

        if (this.Tabs != default(OpTab[]))
        {
            OpLabel titleLabel = new OpLabel(2f, decalage, "BepInEx Config Options",true);
            decalage -= 27f; // Move "cursor" for next option
            OpLabel subtitleLabel = new OpLabel(2f, decalage, "Modifications will require a game restart to show any effects",false);
            decalage -= 27f; // Move "cursor" for next option



            OpLabel localLabel = new OpLabel(43+80*2f, decalage, "Open console on game start");
            OpSimpleButton localCheckBox = new OpSimpleButton(new Vector2(10f, decalage - 2f), new(70,25), "ENABLE") { description = "Make BepInEx open a console with live logs" };
            OpSimpleButton localCheckBox2 = new OpSimpleButton(new Vector2(90f, decalage - 2f), new(70,25), "DISABLE") { description = "Do not make BepInEx open a console with live logs" };

            localCheckBox.OnClick += (_) => {localMoreLogsInstance.changeBepInConfig("Logging.Console", "Enabled", "true");};
            localCheckBox2.OnClick += (_) => {localMoreLogsInstance.changeBepInConfig("Logging.Console", "Enabled", "false");};
            decalage -= 27f; // Move "cursor" for next option
            

            OpLabel writeUnityLogs = new OpLabel(43+80*2f, decalage, "Write unity logs to LogOutput.log");
            OpSimpleButton writeUnityLogsboxyes = new OpSimpleButton(new Vector2(10f, decalage - 2f), new(70,25), "ENABLE") { description = "Include unity loggings to LogOutput.log" };
            OpSimpleButton writeUnityLogsboxno = new OpSimpleButton(new Vector2(90f, decalage - 2f), new(70,25), "DISABLE") { description = "Do not nclude unity loggings to LogOutput.log" };

            writeUnityLogsboxyes.OnClick += (_) => {localMoreLogsInstance.changeBepInConfig("Logging.Disk", "WriteUnityLog", "true");};
            writeUnityLogsboxno.OnClick += (_) => {localMoreLogsInstance.changeBepInConfig("Logging.Disk", "WriteUnityLog", "false");};
            
            foreach (OpSimpleButton el in (OpSimpleButton[])[localCheckBox, localCheckBox2, writeUnityLogsboxyes, writeUnityLogsboxno]) {
                el.OnClick += (_) => {subtitleLabel.color = Color.red;};
            }
            decalage -= 27f; // Move "cursor" for next option

            statusInfoLabel = new OpLabel(2f, decalage, "",false);

            UIelement[] newUiElement = new UIelement[]{
                titleLabel,
                subtitleLabel,

                localLabel,
                localCheckBox,
                localCheckBox2,

                writeUnityLogs,
                writeUnityLogsboxyes,
                writeUnityLogsboxno,

                statusInfoLabel
            };


            this.Tabs[0].AddItems(newUiElement);
            decalage -= 27f; // Move "cursor" for next option

        }
        else
        {
            LogSource.LogError("We tried to add elements to our settings page, but there was no settings page");
        }
    }

    public void TestCallback(Configurable<bool> configurable)
    {
        LogSource.LogDebug(configurable.key + " was updated to " + configurable.Value + ", but no callback was set on value change.");
    }



    public delegate void SomeUpdateFunction(Configurable<bool> configurable);


}