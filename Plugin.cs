using BepInEx;
using System.Collections.Generic;
using UnityEngine;
using ShadowLib.ChatLog;
using UnityEngine.SceneManagement;
using ShadowLib;
using UniverseLib.UI;
using UniverseLib.Config;
using UniverseLib;
using WobblyManhunt.UI;
using HawkNetworking;
using WobblyPlayerTags.Object;
using Steamworks;
using System.Runtime.CompilerServices;
using System.Collections;
using System;
using HarmonyLib;

namespace WobblyManhunt
{
    [BepInPlugin("lstwo.wobblymanhunt", PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        public static AssetBundle bundle { get; private set; }
        public static ManhuntGame currentGame { get; private set; }

        public static UIBase UIBase { get; private set; }
        public static MainPanel MainPanel { get; private set; }

        public static GameManager GameManager { get; private set; }

        public static PlayerTag HunterTag { get; private set; } = new("HunterTag", "Hunter", colorTag: "red");
        public static PlayerTag PlayerTag { get; private set; } = new("PlayerTag", "Player", colorTag: "green");
        public static PlayerTag SpectatorTag { get; private set; } = new("SpectatorTag", "Spectator", colorTag: "blue");

        public static Plugin Instance { get; private set; }

        // Main Plugin

        private void Awake()
        {
            Instance = this;

            bundle = AssetUtils.LoadAssetBundleFromPluginsFolder("lstwo.wobblymanhunt");

            SceneManager.sceneLoaded += OnSceneLoaded;

            GameObject obj = new GameObject("WobblyManhunt_GameManager");
            obj.transform.parent = transform; 
            GameManager = obj.AddComponent<GameManager>();

            UniverseLibConfig config = new()
            {
                Force_Unlock_Mouse = true
            };

            UniverseLib.Universe.Init(1f, OnUIInitialized, (x,y) => { }, config);

            Logger.LogInfo($"Plugin \"lstwo.wobblymanhunt\" is loaded!");
        }

        // UI Stuff

        private static void OnUIInitialized()
        {
            UIBase = UniversalUI.RegisterUI("lstwo.manhunt", UIUpdate);

            MainPanel = new MainPanel(UIBase);

            UIBase.Enabled = false;
        }

        private static void UIUpdate()
        {

        }

        public static void SetUIEnabled(bool enabled)
        {
            if(UIBase == null) throw new ArgumentNullException(nameof(UIBase));
            UIBase.Enabled = enabled;
            if(MainPanel == null) throw new ArgumentNullException(nameof(UIBase));
            MainPanel._SetActive();
        }

        private void Update()
        {
            if(Input.GetKeyDown(KeyCode.F10))
            {
                SetUIEnabled(!UIBase.Enabled);
            }
        }

        // Game Mode Stuff

        private static void OnSceneLoaded(Scene scene, LoadSceneMode loadMode)
        {
            if(loadMode == LoadSceneMode.Single && scene.name == "WobblyIsland")
            {
                Debug.Log("asd");
                InitializeManhuntGame();
            } else if(loadMode == LoadSceneMode.Single)
            {
                UninitializeManhuntGame();
            }
        }

        private static void InitializeManhuntGame()
        {
            currentGame = new ManhuntGame();
        }

        private static void UninitializeManhuntGame()
        {
            currentGame = null;
            Instance.StopAllCoroutines();
        }

        public static void SetClientPlayerType(PlayerController controller, PlayerType playerType)
        {

            if (currentGame.playerTypes.ContainsKey(controller))
            {
                currentGame.playerTypes[controller] = playerType;
            } else
            {
                currentGame.playerTypes.Add(controller, playerType);
            }

            if (HawkNetworkManager.DefaultInstance.GetMe().IsHost)
            {
                PlayerTag tag = null;

                if(playerType == PlayerType.Hunter) tag = HunterTag;
                if(playerType == PlayerType.Player) tag = PlayerTag;
                if(playerType == PlayerType.Spectator) tag = SpectatorTag;

                if(tag != null)
                    WobblyPlayerTags.Plugin.ChangePlayerName(controller, tag);
            }
        }

        public static void SetClientPlayerReady(PlayerController controller, bool ready)
        {

            if (currentGame.playerReady.ContainsKey(controller))
            {
                currentGame.playerReady[controller] = ready;
            }
            else
            {
                currentGame.playerReady.Add(controller, ready);
            }

            if(HawkNetworkManager.DefaultInstance.GetMe().IsHost)
            {
                bool canStart = true;

                foreach(bool _ready in currentGame.playerReady.Values)
                {
                    if(!_ready)
                    {
                        canStart = false;
                    }
                }

                currentGame.canStart = canStart;
            }
        }

        public static bool CheckPlayerType(PlayerController controller, PlayerType type)
        {
            if (!currentGame.playerTypes.ContainsKey(controller))
            {
                return false;
            }
            else if (currentGame.playerTypes[controller] != type)
            {
                return false;
            }
            return true;
        }

        public static bool CheckPlayerReady(PlayerController controller, bool ready)
        {
            if (!currentGame.playerReady.ContainsKey(controller))
            {
                return false;
            }
            else if (currentGame.playerReady[controller] != ready)
            {
                return false;
            }
            return true;
        }

        public static string GetPlayerTypeName(PlayerType type)
        {
            if (type == PlayerType.Player) return "Player";
            if (type == PlayerType.Hunter) return "Hunter";
            if (type == PlayerType.Spectator) return "Spectator";
            return "";
        }

        public static void StartGame()
        {
            ChatLogManager.Instance.ServerSendLogMessage("<b> STARTING GAME </b>", Color.green);
            currentGame.hasStarted = true;
            Instance.StartCoroutine(PlayerWinCoroutine());
            Instance.StartCoroutine(HunterGrabCooldown(currentGame.gameSettings.hunterCooldownLength));
            LockRespawning();
        }

        public static void FinishGame(PlayerType winners)
        {
            ChatLogManager.Instance.ServerSendLogMessage("THE <b> " + GetPlayerTypeName(winners).ToUpper() + "S </b> WON THE GAME!", Color.green);
            StopGame();
        }

        public static void HunterCatchPlayer(PlayerController player)
        {
            ChatLogManager.Instance.ServerSendLogMessage("Player <b> \"" + WobblyPlayerTags.Plugin.actualNames[PlayerUtils.steamIds[player]] + "\" </b> has been caught!");

            if(currentGame.gameSettings.onlyCatchOnePlayer)
            {
                FinishGame(PlayerType.Hunter);
                return;
            }

            player.GetPlayerCharacter().Kill();

            currentGame.playerTypes[player] = currentGame.gameSettings.playersToHunters ? PlayerType.Hunter : PlayerType.Spectator;
        }

        private static void StopGame()
        {
            ChatLogManager.Instance.ServerSendLogMessage("<b> STOPPING GAME </b>", new Color(1, .5f, .5f));
            Instance.StopCoroutine(PlayerWinCoroutine());
            UnlockRespawning();
        }

        private static IEnumerator PlayerWinCoroutine()
        {
            yield return new WaitForSeconds(currentGame.gameSettings.timeForHunters);

            FinishGame(PlayerType.Player);
        }

        private static IEnumerator HunterGrabCooldown(float length)
        {
            currentGame.hasHunterCooldownFinished = false;
            yield return new WaitForSeconds(length);
            currentGame.hasHunterCooldownFinished = true;
        }

        private static void LockRespawning()
        {
            if (currentGame.gameSettings.allowPlayerRespawn && currentGame.gameSettings.allowHunterRespawn) return;

            foreach (var player in currentGame.playerTypes.Keys)
            {
                var type = currentGame.playerTypes[player];

                if (type == PlayerType.Player && !currentGame.gameSettings.allowPlayerRespawn)
                {
                    player.SetAllowedToRespawn(Instance, false);
                } 
                
                else if(type == PlayerType.Hunter && !currentGame.gameSettings.allowHunterRespawn)
                {
                    player.SetAllowedToRespawn(Instance, false);
                }
            }
        }

        private static void UnlockRespawning()
        {
            foreach (var player in currentGame.playerTypes.Keys)
            {
                player.SetAllowedToRespawn(Instance, true);
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
        public GameSettings gameSettings = new();
        public bool hasStarted = false;
        public bool canStart = false;
        public bool hasHunterCooldownFinished = false;
        public Dictionary<PlayerController, PlayerType> playerTypes = new Dictionary<PlayerController, PlayerType>();
        public Dictionary<PlayerController, bool> playerReady = new Dictionary<PlayerController, bool>();
    }

    public struct GameSettings
    {
        public bool playersToHunters = true;
        public bool onlyCatchOnePlayer = false;
        public bool allowPlayerRespawn = false;
        public bool allowHunterRespawn = true;
        public float timeForHunters = 30;
        public float hunterCooldownLength = 3;

        public GameSettings()
        {

        }
    }

    public static class RagdollHandJointPatch
    {
        [HarmonyPatch(typeof(RagdollHandJoint), "OnObjectGrabbed")]
        [HarmonyPostfix]
        public static void OnObjectGrabbedPrefix(ref RagdollHandJoint __instance, ref GameObject grabbingObject)
        {
            if (!Plugin.currentGame.hasHunterCooldownFinished) return;

            if(Plugin.currentGame.playerTypes[__instance.GetPlayerCharacter().GetPlayerController()] == PlayerType.Hunter && grabbingObject.TryGetComponent<RagdollPart>(out var part))
            {
                var controller = part.GetPlayerCharacter().GetPlayerController();
                if (Plugin.currentGame.playerTypes[controller] == PlayerType.Player)
                {
                    Plugin.HunterCatchPlayer(controller);
                }
            }
        }
    }
}
