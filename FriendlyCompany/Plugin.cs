using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using ravingdead.patch;

namespace ravingdead;

[BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin
{
    public static Plugin Instance { get; set; }

    public static ManualLogSource Log => Instance.Logger;

    private readonly Harmony _harmony = new(PluginInfo.PLUGIN_GUID);

    public Plugin()
    {
        Instance = this;
    }

    private void Awake()
    {
        Log.LogInfo($"Applying patches...");
        ApplyPluginPatch();
        Log.LogInfo($"Patches applied");
    }

    /// Applies the patch to the game.
    private void ApplyPluginPatch()
    {
        _harmony.PatchAll(typeof(RedLocustBeesPatch));
    }
}
