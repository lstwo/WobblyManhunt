using HawkNetworking;
using ShadowLib;
using ShadowLib.ChatLog;
using ShadowLib.Networking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace WobblyManhunt
{
    public class GameManager : ShadowNetworkSingleton
    {
        private byte RPC_UPDATE_PLAYER_TYPE;
        private byte RPC_UPDATE_PLAYER_READY;
        private byte RPC_START_GAME;
        private byte RPC_FINISH_GAME;

        protected override void RegisterRPCs(HawkNetworkObject networkObject)
        {
            base.RegisterRPCs(networkObject);

            RPC_UPDATE_PLAYER_TYPE = networkObject.RegisterRPC(ClientUpdatePlayerType);
            RPC_UPDATE_PLAYER_READY = networkObject.RegisterRPC(ClientUpdatePlayerReady);
        }

        public void ServerUpdatePlayerType(PlayerController controller, PlayerType newPlayerType)
        {
            if (networkObject == null) return;

            if (Plugin.currentGame.hasStarted)
            {
                ChatLogManager.Instance.ClientSendLogMessage("Can't join team. Game already started!", new Color(1, .5f, .5f));
                return;
            }

            networkObject.SendRPC(RPC_UPDATE_PLAYER_TYPE, RPCRecievers.All, (int)newPlayerType, controller.networkObject.GetNetworkID());
            ChatLogManager.Instance.ServerSendLogMessage("Player <b> \"" + WobblyPlayerTags.Plugin.actualNames[PlayerUtils.steamIds[controller]] + "\" </b> switched to " + Plugin.GetPlayerTypeName(newPlayerType), new Color(.5f, .5f, 1));
        }

        private void ClientUpdatePlayerType(HawkNetReader reader, HawkRPCInfo info)
        {
            PlayerType newPlayerType = (PlayerType)reader.ReadInt32();
            PlayerController controller = GameInstance.Instance.GetPlayerControllerByNetworkID(reader.ReadUInt32());
            
            Plugin.SetClientPlayerType(controller, newPlayerType);
        }

        public void ServerUpdatePlayerReady(PlayerController controller, bool ready)
        {
            if (networkObject == null) return;
            networkObject.SendRPC(RPC_UPDATE_PLAYER_READY, RPCRecievers.All, ready, controller.networkObject.GetNetworkID());

            if (Plugin.currentGame.hasStarted)
            {
                ChatLogManager.Instance.ClientSendLogMessage("Can't set ready. Game already started!", new Color(1, .5f, .5f));
                return;
            }

            if (ready)
            {
                ChatLogManager.Instance.ServerSendLogMessage("Player <b> \"" + WobblyPlayerTags.Plugin.actualNames[PlayerUtils.steamIds[controller]] + "\" </b> is now Ready!", new Color(.5f, 1f, .5f));
            } else
            {
                ChatLogManager.Instance.ServerSendLogMessage("Player <b> \"" + WobblyPlayerTags.Plugin.actualNames[PlayerUtils.steamIds[controller]] + "\" </b> is not Ready!", new Color(1, .5f, .5f));
            }
        }

        private void ClientUpdatePlayerReady(HawkNetReader reader, HawkRPCInfo info)
        {
            bool ready = reader.ReadBoolean();
            PlayerController controller = GameInstance.Instance.GetPlayerControllerByNetworkID(reader.ReadUInt32());

            Plugin.SetClientPlayerReady(controller, ready);
        }

        public void ServerStartGame()
        {
            if (networkObject == null && !networkObject.IsServer()) return;
            if(Plugin.currentGame.canStart)
            {
                Plugin.StartGame();
            } else
            {
                bool canStart = true;

                foreach (bool _ready in Plugin.currentGame.playerReady.Values)
                {
                    if (!_ready)
                    {
                        canStart = false;
                    }
                }

                Plugin.currentGame.canStart = canStart;

                if(!canStart)
                {
                    ChatLogManager.Instance.ClientSendLogMessage("Cant Start Game! Not every Player is Ready.", Color.red);
                } else
                {
                    Plugin.StartGame();
                }
            }
        }

        public void ServerFinishGame()
        {

        }
    }
}
