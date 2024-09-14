using ShadowLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UniverseLib.UI;
using UniverseLib.UI.Models;

namespace WobblyManhunt.UI
{
    public class MainPanel : UniverseLib.UI.Panels.PanelBase
    {
        public MainPanel(UIBase owner) : base(owner) { }

        public override string Name => "My Panel";
        public override int MinWidth => 100;
        public override int MinHeight => 200;
        public override Vector2 DefaultAnchorMin => new(0.25f, 0.25f);
        public override Vector2 DefaultAnchorMax => new(0.75f, 0.75f);
        public override bool CanDragAndResize => true;

        private PlayerController controller = null;

        private ButtonRef readyBtn, unreadyBtn, startBtn;

        private List<GameObject> hostOptions = new();

        protected override void ConstructPanelContent()
        {
            var ui = new UIHelper(ContentRoot);

            ui.CreateLabel("<b>PLAYER SETTINGS</b>");

            ui.AddSpacer(5);

            ui.CreateButton("Join Hunters", () => {
                _SetActive();
                if(!Plugin.CheckPlayerType(controller, PlayerType.Hunter))
                {
                    Plugin.GameManager.ServerUpdatePlayerType(controller, PlayerType.Hunter);
                }
            });

            ui.CreateButton("Join Players", () => {
                _SetActive();
                if (!Plugin.CheckPlayerType(controller, PlayerType.Player))
                {
                    Plugin.GameManager.ServerUpdatePlayerType(controller, PlayerType.Player);
                }
            });

            ui.AddSpacer(5);

            readyBtn = ui.CreateButton("Ready", () => {
                _SetActive();
                if (!Plugin.CheckPlayerReady(controller, true))
                {
                    Plugin.GameManager.ServerUpdatePlayerReady(controller, true);
                    unreadyBtn.GameObject.SetActive(true);
                    readyBtn.GameObject.SetActive(false);
                }
            }, color: Color.green);

            unreadyBtn = ui.CreateButton("Unready", () => {
                _SetActive();
                if (!Plugin.CheckPlayerReady(controller, false))
                {
                    Plugin.GameManager.ServerUpdatePlayerReady(controller, false);
                    unreadyBtn.GameObject.SetActive(false);
                    readyBtn.GameObject.SetActive(true);
                }
            }, color: Color.red);

            unreadyBtn.GameObject.SetActive(false);

            ui.AddSpacer(5);

            ui.CreateLabel("<b>HOST SETTINGS</b>");

            ui.AddSpacer(5);

            startBtn = ui.CreateButton("Start Game", StartGame);
            hostOptions.Add(startBtn.GameObject);

            ui.AddSpacer(5);

            var playersToHunters = ui.CreateToggle(label: "Players to Hunters on Death", onValueChanged: (b) => Plugin.currentGame.gameSettings.playersToHunters = b, defaultState: true);
            hostOptions.Add(playersToHunters.gameObject);

            ui.AddSpacer(5);

            var allowPlayerRespawn = ui.CreateToggle(label: "Allow Player Respawning", onValueChanged: (b) => Plugin.currentGame.gameSettings.allowPlayerRespawn = b, defaultState: false);
            hostOptions.Add(allowPlayerRespawn.gameObject);

            ui.AddSpacer(5);

            var allowHunterRespawn = ui.CreateToggle(label: "Allow Hunter Respawning", onValueChanged: (b) => Plugin.currentGame.gameSettings.allowHunterRespawn = b, defaultState: true);
            hostOptions.Add(allowHunterRespawn.gameObject);

            ui.AddSpacer(5);

            var onlyCatchOnePlayer = ui.CreateToggle(label: "Hunters only need to Catch one Player", onValueChanged: (b) => Plugin.currentGame.gameSettings.onlyCatchOnePlayer = b, defaultState: true);
            hostOptions.Add(onlyCatchOnePlayer.gameObject);
        }

        public void _SetActive()
        {
            controller = PlayerUtils.GetMyPlayer();
            bool b;

            try
            {
                b = controller.networkObject.GetNetworkID() == PlayerUtils.GetHostPlayer().networkObject.GetNetworkID();
            } 
            catch
            {
                b = false;
            }

            foreach (GameObject obj in hostOptions)
            {
                obj.SetActive(b);
            }
        }

        protected override void OnClosePanelClicked()
        {
            Plugin.SetUIEnabled(false);
        }

        protected void StartGame()
        {
            _SetActive();

            Plugin.GameManager.ServerStartGame();
        }
    }
}
