using System.Linq;
using Cysharp.Threading.Tasks;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using Game.Scripts.API.Endpoints;
using Game.Scripts.Networking.Lobby;
using Game.Scripts.Player;
using Game.Scripts.UI.Loading;
using NewDropDude.Script.API.ServerManagers;
using UnityEngine;

namespace Game.Scripts
{
    public class GameplayTimer : NetworkBehaviour
    {
        public float startTime = 100;
        public NetworkObject networkObject;
        public readonly SyncVar<float> Timer = new(0);
        public ServerRoom serverRoom; //server field
        private bool _isClose;
        public override void OnStartServer()
        {
            TimerStart();
        }

        public override void OnStartClient()
        {
            Timer.OnChange += (prev, next, server) =>
            {
                GameplayTimerUI.SetTime(next);
            };
        }

        private async void TimerStart()
        {
            bool isTimerActive = true;
            Timer.Value = startTime;
        
            while (isTimerActive)
            {
                await UniTask.Delay(1000);
                Timer.Value -= 1;

                if (Timer.Value < 0 || _isClose)
                {
                    isTimerActive = false;
                }
            }
            
            TimeFinish();
        }

        public void Close()
        {
            _isClose = true;
        }

        private async void TimeFinish()
        {
            PlayerRoot[] players = serverRoom.players.Select(p => p.playerRoot).ToArray();
            
            foreach (PlayerRoot root in players)
            {
                if (root != null)
                {
                    EndMatchMeRequest body = new();
                    body.damage = root.statisticCounter.UnitStats.Value.damage;
                    body.kills = root.statisticCounter.UnitStats.Value.kills;
                    body.result = root.IsDead.Value ? "lose" : "win";
                    body.team = (int)root.Team.Value;
                    body.warriorCode = root.warriorCode;
                    
                    Networking.Lobby.Player player = serverRoom.GetPlayerBuyClientId(root.OwnerId);

                    if (player == null || player.IsBot)
                    {
                        continue;
                    }
                    
                    string token = RegisterServer.GetToken(root.OwnerId);
                    EndMatch(player.matchId, body, token);
                }
            }
            
            await  UniTask.Delay(3000);
            
            TimeFinishObserversRpc();
        }

        private async void EndMatch(int matchId, EndMatchMeRequest body, string token)
        {
            (bool ok, string message) result = await MatchesManager.EndMatchMe(matchId,  body, token);

            if (result.ok == false)
            {
                Debug.LogError("EndMatchMe failed");
            }
        }

        [ObserversRpc]
        private void TimeFinishObserversRpc()
        {
            //send result
            
            GameplaySpawner.In.ReturnToMainMenu();
            LoadingScreenManager.ShowLoading();
            LoadingScreenManager.HideLoading(); //hide with delay 1 second
        }
    }
}
