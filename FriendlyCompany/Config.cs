using System.Runtime.Serialization;
using BepInEx.Configuration;
using CSync.Lib;
using CSync.Util;
using Unity.Netcode;

namespace ravingdead.FriendlyCompany;

[DataContract]
public class ModConfig : SyncedConfig<ModConfig>
{
    [DataMember] public SyncedEntry<bool> ConfigEnableBees { get; private set; }

    internal BepInEx.Logging.LogLevel DebugLevel = BepInEx.Logging.LogLevel.Info;

    public ModConfig(ConfigFile cfg) : base(Plugin.GUID)
    {
        // Register to sync config files between host and clients.
        ConfigManager.Register(this);

        // Bind config entries to config file.
        ConfigEnableBees = cfg.BindSyncedEntry(
            "Toggles",
            "EnableBees",
            true,
            "Whether FriendlyBees should be enabled. Setting this to false will also disable any other features related to bees."
            );
        Plugin.Log.Log(DebugLevel, $"{ConfigEnableBees.Key} = {ConfigEnableBees.Value}");

        // Function to run once config is synced.
        SyncComplete += new((_, _) =>
        {
            // Check if the local client running is the server host.
            if (!IsHost && !NetworkManager.Singleton.IsServer)
            {
                Plugin.Log.LogInfo("Config synced!");
            }
        });
    }
}