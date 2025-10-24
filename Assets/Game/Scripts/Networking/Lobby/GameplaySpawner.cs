using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using FishNet.Connection;
using FishNet.Managing.Scened;
using FishNet.Object;
using FishNet.Transporting;
using Game.GameResources;
using Game.Scripts.API.Endpoints;
using Game.Scripts.API.ServerManagers;
using Game.Scripts.Core.Helpers;
using Game.Scripts.Gameplay;
using Game.Scripts.MenuController;
using Game.Scripts.Player;
using Game.Scripts.UI.HUD;
using Game.Scripts.UI.MainMenu;
using Game.Scripts.World.Spawns;
using NewDropDude.Script.API.ServerManagers;
using UnityEngine;
using UnityEngine.SceneManagement;
using UEScene = UnityEngine.SceneManagement.Scene;
using UESceneManager = UnityEngine.SceneManagement.SceneManager;

namespace Game.Scripts.Networking.Lobby
{
    public class GameplaySpawner : NetworkBehaviour
    {
        public static GameplaySpawner In;
        public GameplayTimer gameplayTimerPrefab;

        [SerializeField] private LobbyManager lobbyManager;

        private UEScene _additiveServerScene;
        public int sceneOffsetX;
        private const float SceneValidationTimeout = 10f;

        private void Awake()
        {
            In = this;
        }

        public override void OnStartServer()
        {
            base.OnStartServer();
            UESceneManager.sceneLoaded += HandleServerSceneLoaded;
            SceneManager.OnLoadEnd += HandleServerLoadEnd;
            ServerManager.OnRemoteConnectionState += OnRemoteConnectionState;
        }
        
        private void OnRemoteConnectionState(NetworkConnection conn, RemoteConnectionStateArgs args)
        {
            if (args.ConnectionState == RemoteConnectionState.Stopped)
            {
                ServerRoom serverRoom = LobbyRooms.GetRoomByConnection(conn);
                if (serverRoom == null)
                {
                    return;
                }

                Player player = serverRoom.GetPlayers().Find(x => x.clientId == conn.ClientId);

                if (player == null)
                {
                    return;
                }

                LobbyRooms.RemovePlayerFromRoom(serverRoom.roomId, player.loginName);
            }
        }

        private void HandleServerSceneLoaded(UEScene scene, LoadSceneMode mode)
        {
            if (!IsValidScene(scene))
            {
                return;
            }

            int usedOffset = sceneOffsetX;

            foreach (GameObject go in scene.GetRootGameObjects())
            {
                go.transform.position += Vector3.right * usedOffset;
            }

            sceneOffsetX += 500;
            _additiveServerScene = scene;

            // повідомити клієнтів про зсув
            ApplySceneOffsetClientRpc(scene.handle, usedOffset);
        }

        private void HandleServerLoadEnd(SceneLoadEndEventArgs args)
        {
            foreach (object param in args.QueueData.SceneLoadData.Params.ServerParams)
            {
                if (param is ServerRoom info)
                {
                    ServerRoom serverRoom = LobbyRooms.GetRoomById(info.roomId);

                    foreach (Scene sc in args.LoadedScenes)
                    {
                        if (sc.name == serverRoom.loadedSceneName)
                        {
                            serverRoom.handle = sc.handle;
                        }
                    }
                }
            }
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            UESceneManager.sceneLoaded += HandleClientSceneLoaded;
            SceneManager.OnLoadEnd += HandleClientLoadEnd;
            SceneManager.OnUnloadEnd += SceneManagerOnUnloadEnd;
            GameplayGUI.In.pauseMenu.OnDisconnectPressed += ReturnToMainMenu;
        }

        private void HandleClientSceneLoaded(UEScene scene, LoadSceneMode mode)
        {
            if (!IsValidScene(scene))
            {
                return;
            }

            NotifyServerSceneLoaded(ClientManager.Connection.ClientId);
        }

        private void HandleClientLoadEnd(SceneLoadEndEventArgs args)
        {
            byte[] cp = args.QueueData.SceneLoadData.Params.ClientParams;
            int offset = (cp != null && cp.Length >= 4) ? BitConverter.ToInt32(cp, 0) : 0;

            foreach (Scene scene in args.LoadedScenes)
            {
                if (!IsValidScene(scene))
                {
                    continue;
                }

                foreach (GameObject go in scene.GetRootGameObjects())
                {
                    go.transform.position += Vector3.right * offset;
                }
            }
        }

        private void SceneManagerOnUnloadEnd(SceneUnloadEndEventArgs obj)
        {
        }

        public void ReturnToMainMenu()
        {
            RobotView.GenerateIcons();
            MainMenu.In.SetActive(true);
            MenuManager.CloseMenu(MenuType.GameplayHUD);

            foreach (PlayerRoot root in FindObjectsByType<PlayerRoot>(FindObjectsSortMode.None))
            {
                if (root.OwnerId == ClientManager.Connection.ClientId)
                {
                    //Destroy from gameplay
                }
            }

            RequestPlayerDisconnectServerRpc(ClientManager.Connection.ClientId);
            lobbyManager.RequestGetRoomList();
        }

        [ServerRpc(RequireOwnership = false)]
        public void RequestPlayerDisconnectServerRpc(int clientId)
        {
            if (ServerManager.Clients.TryGetValue(clientId, out NetworkConnection conn) == false)
            {
                return;
            }

            ServerRoom serverRoom = LobbyRooms.GetRoomByConnection(conn);

            if (serverRoom != null)
            {
                Player player = serverRoom.GetPlayerBuyClientId(conn.ClientId);

                if (player != null)
                {
                    SceneManager.UnloadConnectionScenes(conn, new SceneUnloadData(serverRoom.loadedSceneName));
                    LobbyRooms.RemovePlayerFromRoom(serverRoom.roomId, player.loginName);
                    Despawn(player.playerRoot.networkObject);
                }
            }
        }

        private bool IsValidScene(UEScene scene) => scene.IsValid() && RoomController.Maps.Any(k => scene.name.Contains(k.ToString()));

        [ServerRpc(RequireOwnership = false)]
        private void NotifyServerSceneLoaded(int clientId)
        {
            if (!ServerManager.Clients.TryGetValue(clientId, out NetworkConnection conn))
            {
                return;
            }

            ServerRoom serverRoom = LobbyRooms.GetRoomByConnection(conn);
            Player playerByConnection = serverRoom.GetPlayerBuyClientId(conn.ClientId);
            playerByConnection.connected = true;

            List<Player> realPlayers = new();

            foreach (Player player in serverRoom.GetPlayers())
            {
                if (player.IsBot == false)
                {
                    realPlayers.Add(player);
                }
            }

            bool allLoaded = realPlayers.All(p => p.connected);

            if (allLoaded) //виконується тільки тоді коли всі гравці загрузилися
            {
                foreach (Player player in serverRoom.GetPlayers())
                {
                    if (player.IsBot)
                    {
                        SpawnBot(serverRoom, player);
                    }
                    else
                    {
                        SpawnPlayer(serverRoom, player.clientId);
                    }
                }

                LobbyRooms.UpdateRoomStatusInGame(serverRoom.roomId);
                SpawnTimer(serverRoom);
                StartMatch(serverRoom);
            }
        }

        private async void StartMatch(ServerRoom serverRoom)
        {
            await UniTask.Delay(500);
            
            List<PlayerRoot> playerRoots = serverRoom.players.Select(p => p.playerRoot).ToList();
            playerRoots.RemoveAllNull();
            
            List<(string token, PlayerRoot root)> tokens = new();
                
            foreach (PlayerRoot playerRoot in playerRoots)
            {
                string token = RegisterServer.GetToken(playerRoot.OwnerId);

                if (token != string.Empty)
                {
                    tokens.Add((token, playerRoot));
                }
            }
                
            foreach ((string token, PlayerRoot root) token in tokens)
            {
                StartMatchAsync(token, serverRoom);
            }
        }

        private async void StartMatchAsync((string token, PlayerRoot root) info, ServerRoom serverRoom)
        {
           (bool ok, string message, MatchStartResponse data) result = await MatchesManager.StartMatch("default_map", info.token);
           
           if (result.ok == false)
           {
               Debug.LogError("Failed to start match: "+info.root.OwnerId);
           }
           else
           {
               Player owner = serverRoom.players.Where(p => p.clientId == info.root.OwnerId).ToList().FirstOrDefault();
               
               if (owner != null)
               {
                   owner.matchId = result.data.matchId;
               }
           }
        }
        
        private void SpawnTimer(ServerRoom serverRoom)
        {
            GameplayTimer timer = Instantiate(gameplayTimerPrefab, Vector3.zero, Quaternion.identity);
            ServerManager.Spawn(timer.networkObject, LocalConnection, _additiveServerScene);
            serverRoom.gameplayTimer = timer;
            timer.serverRoom = serverRoom;
        }

        private void SpawnBot(ServerRoom serverRoom, Player player)
        {
            SpawnPoint spawnPoint = SpawnPoint.GetFreePoint(_additiveServerScene, player.team);

            if (spawnPoint == null)
            {
                Debug.LogError("Не знайдено вільної точки спавну.");
                return;
            }

            string warriorCode = "vik_l1_starter";
                
            PlayerRoot root = Instantiate(ResourceManager.GetPrefab(), spawnPoint.transform.position, Quaternion.identity);
            ServerManager.Spawn(root.networkObject, LocalConnection, _additiveServerScene);
            
            WarriorDto info = WarriorsServer.GetWarrior(warriorCode);
            
            root.warriorCode = warriorCode;
            root.health.SetHpServer(info.hp);
            root.meleeWeapon.SetDamage(info.damage);
            root.Team.Value = player.team;
            
            player.playerRoot = root;
            player.playerRoot.characterInit.ServerInit(PlayerType.Bot, _additiveServerScene, warriorCode, player.clientId);
        }

        

        private async void SpawnPlayer(ServerRoom serverRoom, int clientId)
        {
            float elapsedTime = 0f;

            while (!_additiveServerScene.IsValid() && elapsedTime < SceneValidationTimeout)
            {
                elapsedTime += Time.deltaTime;
                await UniTask.DelayFrame(1);
            }

            if (!_additiveServerScene.IsValid())
            {
                Debug.LogError("Не вдалося валідувати адитивну сцену протягом відведеного часу.");
                return;
            }

            Player player = serverRoom.GetPlayerBuyClientId(clientId);

            SpawnPoint spawnPoint = SpawnPoint.GetFreePoint(_additiveServerScene, player.team);
            PlayerProfileDto profile = ProfileServer.GetProfileByClientId(clientId);
            PlayerRoot playerRoot = Instantiate(ResourceManager.GetPrefab(), spawnPoint.transform.position, Quaternion.identity);
            
            NetworkConnection connection = ServerManager.Clients[clientId];
            ServerManager.Spawn(playerRoot.networkObject, connection, _additiveServerScene);
            
            WarriorDto info = WarriorsServer.GetWarrior(profile.activeWarriorCode);
            
            playerRoot.warriorCode = profile.activeWarriorCode;
            playerRoot.health.SetHpServer(info.hp);
            playerRoot.meleeWeapon.SetDamage(info.damage);
            playerRoot.Team.Value = player.team;
            
            player.playerRoot = playerRoot;
            player.playerRoot.characterInit.ServerInit(PlayerType.Player, _additiveServerScene, profile.activeWarriorCode, connection.ClientId);
        }
        
        [ObserversRpc]
        private void ApplySceneOffsetClientRpc(int sceneHandle, int offset)
        {
            UEScene scene = GetSceneByHandleLocal(sceneHandle);

            if (!scene.IsValid())
            {
                return;
            }

            foreach (GameObject go in scene.GetRootGameObjects())
            {
                go.transform.position += Vector3.right * offset;
            }
        }

        private UEScene GetSceneByHandleLocal(int handle)
        {
            for (int i = 0; i < UESceneManager.sceneCount; i++)
            {
                Scene s = UESceneManager.GetSceneAt(i);
                if (s.handle == handle)
                {
                    return s;
                }
            }

            return default;
        }

        public static List<T> FindObjectsInScene<T>(UEScene scene, bool includeInactive = true) where T : Component
        {
            List<T> results = new List<T>();

            if (!scene.IsValid())
            {
                return results;
            }

            foreach (GameObject root in scene.GetRootGameObjects())
            {
                results.AddRange(root.GetComponentsInChildren<T>(includeInactive));
            }

            return results;
        }

        public static List<Component> FindObjectsInScene(GameObject root, Type componentType,
            bool includeInactive = true)
        {
            if (root == null)
            {
                return new List<Component>();
            }

            return root.GetComponentsInChildren(componentType, includeInactive).Cast<Component>().ToList();
        }

        public static List<T> FindObjectsInScene<T>(GameObject root, bool includeInactive = true) where T : Component
        {
            if (root == null)
            {
                return new List<T>();
            }

            return root.GetComponentsInChildren<T>(includeInactive).ToList();
        }
    }
}
