using System.Linq;
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
            
            MeshPack mesh = ResourceManager.GetMesh(meshCode);
            playerRoot.PutMesh(mesh);
        }

        public override void OnStartClient()
        {
            if (IsOwner)
            {
                IAmLoaded(OwnerId);
            }
        }

        [ServerRpc]
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
            if (IsOwner) //only owner
            {
                playerRoot.Init();
                
                PlayerRoot[] players = FindObjectsByType<PlayerRoot>(FindObjectsSortMode.None);
            
                Camera cam = CameraSync.In.gameplayCamera;

                foreach (PlayerRoot root in players)
                {
                    if (OwnerId != root.OwnerId)
                    {
                        root.playerHUD.SetCamera(cam);
                        root.playerHUD.SetNick(root.characterInit.LoginName.Value);
                    }
                }
            }
            
            //for all clients
            MeshPack mesh = ResourceManager.GetMesh(MeshCode.Value);
            playerRoot.PutMesh(mesh);
        }
    }
}
