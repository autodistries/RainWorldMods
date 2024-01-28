using BepInEx;
using System.Reflection;
using MonoMod.RuntimeDetour;
using System.IO;


namespace MoreLogs;

[BepInPlugin(PLUGIN_GUID, PLUGIN_NAME, PLUGIN_VERSION)]

public class MoreLogger : BaseUnityPlugin
{
    public const string PLUGIN_GUID = "nope.alllogstoconsole";
    public const string PLUGIN_NAME = "All Logs To Console";
    public const string PLUGIN_VERSION = "1.0.0";



    BindingFlags theFlags = BindingFlags.Static | BindingFlags.Public;
    private void Awake()
    {
        Hook showLogsHook = new Hook(
     typeof(RainWorld).GetProperty("ShowLogs", theFlags).GetGetMethod(),
     typeof(MoreLogger).GetMethod("RainWorld_ShowLogs_get", theFlags));
        log("Hooked RainWorld.ShowLogs with success");

        string targetPath = UnityEngine.Application.streamingAssetsPath + "/mods/MoreLogs/ononce";
        if (!File.Exists(targetPath))
        {
            log("First launch ! Enabling BepInEx console. User should restart !");

            // drop the presence file in our directory
            FileStream fs = new FileStream(targetPath, FileMode.CreateNew);
            fs.Close();

            analyzeBepConfigFile();
        }

    }

    

    private void analyzeBepConfigFile()
    {
        int targetIndex = -1;
        string section = string.Empty;
        string[] array = File.ReadAllLines(Paths.BepInExConfigPath);
        for (int i = 0; i < array.Length; i++)
        {

            string text = array[i].Trim();
            if (text.StartsWith("#"))
            {
                continue;
            }

            if (text.StartsWith("[") && text.EndsWith("]"))
            {
                section = text.Substring(1, text.Length - 2);
                continue;
            }

            if (section != "Logging.Console")
            {
                continue;
            }

            string[] array2 = text.Split(new char[1] { '=' }, 2);
            if (array2.Length == 2)
            {
                string key = array2[0].Trim();
                string text2 = array2[1].Trim();
                if (key == "Enabled")
                {
                    if (text2 == "false")
                    {
                        targetIndex = i;
                    }
                    else
                    {
                        targetIndex = -2;
                    }
                    break;
                }
            }

        }
        switch (targetIndex)
        {
            case -1:
                {
                    log("Config file might be damaged, because the option was not found - bepinex is supposed to regenerate it all the time");
                    break;
                }
            case -2:
                {
                    log("Console already on, nothing to do");
                    break;
                }
            default:
                {

                    log("OK, switching Enabled to true: " + array[targetIndex]);
                    array[targetIndex] = "Enabled = true";
                    File.WriteAllLines(Paths.BepInExConfigPath, array);
                   // notifyRestartNeeded();
                    break;
                }
        }
    }

   

    public delegate bool orig_ShowLogs();

    public static bool RainWorld_ShowLogs_get(orig_ShowLogs orig)
    {
        return true;
    }

    private static void log(object msg)
    {
        UnityEngine.Debug.Log("[MoreLogger] " + msg);
    }

}
