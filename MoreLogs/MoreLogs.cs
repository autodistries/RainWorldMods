using BepInEx;
using System.Reflection;
using MonoMod.RuntimeDetour;

namespace MoreLogs;

[BepInPlugin(PLUGIN_GUID, PLUGIN_NAME, PLUGIN_VERSION)]

public class MoreLogger : BaseUnityPlugin
{
    public const string PLUGIN_GUID = "nope.alllogstoconsole";
    public const string PLUGIN_NAME = "All Logs To Console";
    public const string PLUGIN_VERSION = "0.0.1";



    BindingFlags theFlags = BindingFlags.Static | BindingFlags.Public;
    private void OnEnable()
    {
        Hook showLogsHook = new Hook(
     typeof(RainWorld).GetProperty("ShowLogs", theFlags).GetGetMethod(),
     typeof(MoreLogger).GetMethod("RainWorld_ShowLogs_get", theFlags));
     UnityEngine.Debug.Log("[MoreLogger] Hooked RainWorld.ShowLogs with success");
    }

   
    public delegate bool orig_ShowLogs();

    public static bool RainWorld_ShowLogs_get(orig_ShowLogs orig)
    {
        return true;
    }

}




