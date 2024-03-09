using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using ravingdead.FriendlyCompany.patch;
using ravingdead.FriendlyCompany;
using System;

namespace ravingdead.FriendlyCompany;

[BepInPlugin(GUID, PLUGIN_NAME, VERSION)]
[BepInDependency("com.sigurd.csync", BepInDependency.DependencyFlags.HardDependency)]
public class Plugin : BaseUnityPlugin
{
    internal const string GUID = "ravingdead.FriendlyCompany", PLUGIN_NAME = "FriendlyCompany", VERSION = "1.1.0";

    public static Plugin Instance { get; set; }

    public static ModConfig ConfigInstance { get; internal set; }

    public static ManualLogSource Log => Instance.Logger;

    private readonly Harmony _harmony = new(GUID);

    public Plugin()
    {
        Instance = this;
    }

    private void Awake()
    {
        try
        {
            Log.LogInfo("Loading mod...");

            Log.LogInfo("Loading config...");
            ConfigInstance = new(Config);

            Log.LogInfo("Applying Patches...");
            ApplyPluginPatch();

            Log.LogMessage($"Loaded {PLUGIN_NAME} version {VERSION} successfully.");
        }
        catch (Exception e)
        {
            Log.LogError($"Unexpected error occured when initializing: {e.Message}\nSource: {e.Source}");
        }
    }

    /* Patches */
    // Applies the patch to the game.
    private void ApplyPluginPatch()
    {
        if(ConfigInstance.ConfigEnableBees.Value)
            _harmony.PatchAll(typeof(RedLocustBeesPatch));
    }
}
