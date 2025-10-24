using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using Game.GameResources;
using Game.Scripts.Gameplay.Robots;
using Game.Scripts.Networking.Lobby;
using Game.Scripts.Player;
using UnityEngine;
using UEScene = UnityEngine.SceneManagement.Scene;

namespace Game.Scripts.Gameplay
{
    public enum PlayerType
    {
        None,
        Player,
        Bot,
    }

    public class CharacterInit : NetworkBehaviour
    {
        public PlayerRoot playerRoot;
        public readonly SyncVar<string> LoginName = new ();
        public readonly SyncVar<string> MeshCode = new ();
        public readonly SyncVar<PlayerType> PlayerType = new(Gameplay.PlayerType.None);
        public readonly SyncVar<int> AmountPlayersInRoom = new ();
        
        public UEScene currentScene; //for server
        public ServerRoom serverRoom; //for server
        
        [Server]
        public void ServerInit(PlayerType playerType, UEScene scene, string meshCode, int clientId)
        {
            serverRoom = LobbyRooms.GetRoomByClientId(clientId);
            currentScene = scene;
            PlayerType.Value = playerType;
            LoginName.Value = serverRoom.GetPlayerBuyClientId(clientId).loginName;
            MeshCode.Value = meshCode;
            AmountPlayersInRoom.Value = serverRoom.maxPlayers;
            
            MeshPack mesh = ResourceManager.GetMesh(meshCode);
            playerRoot.InitMesh(mesh);
            
            if (playerType == Gameplay.PlayerType.Bot)
            {
                playerRoot.bot.Init();
                IAmLoaded(clientId);
            }
        }

        public override void OnStartClient()
        {
            if (IsOwner)
            {
                IAmLoadedServerRpc(OwnerId);
            }
        }

        [ServerRpc]
        private void IAmLoadedServerRpc(int clientId)
        {
            IAmLoaded(clientId);
        }

        private void IAmLoaded(int clientId)
        {
            Networking.Lobby.Player player = serverRoom.GetPlayerBuyClientId(clientId);
            player.isLoaded = true;

            if (serverRoom.players.All(x=> x.isLoaded))
            {
                foreach (Networking.Lobby.Player unit in serverRoom.players)
                {
                    unit.playerRoot.characterInit.InitClient();
                }
            }
        }

        [ObserversRpc]
        public void InitClient()
        {
            InitClientAsync();
        }

        private async void InitClientAsync()
        {
            if (IsOwner) //only owner
            {
                Camera cam = CameraSync.In.gameplayCamera;
                playerRoot.InitOwner(cam);
                playerRoot.InitMesh(ResourceManager.GetMesh(MeshCode.Value));

                bool processInit = true;
                
                while (processInit)
                {
                    List<PlayerRoot> players = FindObjectsOfType<PlayerRoot>().ToList();

                    if (AmountPlayersInRoom.Value == players.Count)
                    {
                        players.Remove(playerRoot);
                        foreach (PlayerRoot player in players)
                        {
                            CharacterInit carInit = player.characterInit;
                            player.playerHUD.SetCamera(cam);
                            player.playerHUD.SetNick(carInit.LoginName.Value);
                            player.InitTeamView();
                            player.InitMesh(ResourceManager.GetMesh(carInit.MeshCode.Value));
                        }

                        processInit = false;
                    }
                    
                    await UniTask.Delay(300);
                }
            }
        }
    }
}
