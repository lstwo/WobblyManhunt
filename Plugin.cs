using BepInEx;
using System.Collections.Generic;
using System.Net;
using UnityEngine;
using WobblyManhunt.ChatLog;

namespace WobblyManhunt
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        public static AssetBundle bundle { get; private set; }

        private void Awake()
        {
            bundle = Utils.QuickLoadAssetBundle("lstwo.wobblymanhunt");
            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
        }

        private void Start()
        {
            ChatLogManager.Init();
        }

        private void Update()
        {
            if(Input.GetKeyDown(KeyCode.F10))
            {
                ChatLogManager.SendLogMessage("hello");
            }
        }
    }

    public enum PlayerType
    {
        Hunter,
        Player,
        Spectator
    }

    public class ManhuntGame
    {
        public bool hasStarted = false;
        public Dictionary<PlayerController, PlayerType> playerTypes = new Dictionary<PlayerController, PlayerType>();
    }
}
