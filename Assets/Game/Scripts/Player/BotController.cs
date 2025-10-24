using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Game.Scripts.Core.Helpers;
using Game.Scripts.World.Spawns;
using UnityEngine;
using UnityEngine.AI;

namespace Game.Scripts.Player
{
    public class BotController : MonoBehaviour
    {
        private Bot _bot;
        private NavMeshAgent _navMeshAgent;
        private PlayerRoot _playerRoot;
        public bool isActiveBot;
        public BotDisperser botDisperser;
        
        public void Init(PlayerRoot root)
        {
            _playerRoot = root;
            _bot = root.bot;
            _navMeshAgent = _bot.navMeshAgent;
            BehaviorLoop().Forget();
        }

        private async UniTask<bool> EngageNearestTargetAsync()
        {
            Team myTeam = _playerRoot.Team.Value;
            Team enemyTeam = myTeam == Team.Red ? Team.Blue : Team.Red;
            List<PlayerRoot> targets = _bot.GetTargets();
            List<PlayerRoot> alive = new List<PlayerRoot>();
            for (int i = 0; i < targets.Count; i++)
            {
                PlayerRoot t = targets[i];
                if (t.IsDead.Value == false)
                {
                    if (t.Team.Value == enemyTeam)
                    {
                        alive.Add(t);
                    }
                }
            }
            if (alive.Count == 0)
            {
                return false;
            }
            PlayerRoot target = GameplayAssistant.GetNearest(alive, _navMeshAgent.transform.position);
            float stopDist = _bot.GetFightStopDistance();
            bool reached = await _bot.MoveToAsync(target, stopDist,
                () =>
                {
                    if ((Time.time - _bot.moveStartTime) > _bot.pathTimeoutSeconds)
                    {
                        return true;
                    }
                    if (!isActiveBot)
                    {
                        return true;
                    }
                    if (target.IsDead.Value)
                    {
                        return true;
                    }
                    if (_playerRoot.IsDead.Value)
                    {
                        return true;
                    }
                    return false;
                },
                0f
            );
            if (!reached)
            {
                return false;
            }
            _navMeshAgent.isStopped = true;
            float fightStartTime = Time.time;
            while (isActiveBot && target.IsDead.Value == false)
            {
                await UniTask.Yield();
                float dist = Vector3.Distance(_navMeshAgent.transform.position, target.transform.position);
                if (dist > stopDist + 0.02f)
                {
                    break;
                }
                if ((Time.time - fightStartTime) > _bot.fightTimeoutSeconds)
                {
                    break;
                }
                if (!isActiveBot || _playerRoot.IsDead.Value || target.IsDead.Value)
                {
                    break;
                }

                int hp = target.health.CurrentHp;
                
                _bot.FaceTarget(target.transform);
                _bot.Attack();
                await UniTask.Delay(GameplayAssistant.GetRandomInt(_bot.attackDelayMin, _bot.attackDelayMax));
            }
            return true;
        }

        private async UniTask BehaviorLoop()
        {
            await UniTask.Delay(GameplayAssistant.GetRandomInt(_bot.initialDelayMin, _bot.initialDelayMax));
            bool dispersed = await botDisperser.DisperseAsync(_bot);
            isActiveBot = dispersed;
            while (isActiveBot)
            {
                if (_playerRoot.IsDead.Value)
                {
                    await UniTask.Delay(200);
                    continue;
                }
                await UniTask.Delay(200);
                bool engaged = await EngageNearestTargetAsync();
                if (engaged)
                {
                    continue;
                }
                _navMeshAgent.ResetPath();
            }
        }
    }
}
