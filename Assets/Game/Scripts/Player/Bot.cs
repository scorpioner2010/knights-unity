using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Game.Scripts.Core.Utils;
using Game.Scripts.Networking.Lobby;
using UnityEngine;
using UnityEngine.AI;

namespace Game.Scripts.Player
{
    public class Bot : MonoBehaviour
    {
        public PlayerRoot playerRoot;
        public NavMeshAgent navMeshAgent;
        public BotController botController;

        public int initialDelayMin = 1500;
        public int initialDelayMax = 2000;
        public int attackDelayMin = 200;
        public int attackDelayMax = 400;
        public float pathTimeoutSeconds = 4f;
        public float fightTimeoutSeconds = 5f;

        private TimerBlocker _attackSpeed = new (1f);
        public float moveStartTime;

        public void Init()
        {
            navMeshAgent.enabled = true;
            if (playerRoot.characterInit.PlayerType.Value == Gameplay.PlayerType.Bot)
            {
                playerRoot.characterMovement.SetUseRootMotion(false);
            }
            botController.Init(playerRoot);
            playerRoot.characterInput.enabled = false;
        }

        public float GetFightStopDistance()
        {
            return 2.3f;
        }

        public List<PlayerRoot> GetTargets()
        {
            List<PlayerRoot> players = GameplaySpawner.FindObjectsInScene<PlayerRoot>(playerRoot.characterInit.currentScene);
            for (int i = players.Count - 1; i >= 0; i--)
            {
                if (players[i] == playerRoot)
                {
                    players.RemoveAt(i);
                }
            }
            return players;
        }

        public void Attack()
        {
            if (_attackSpeed.IsBlock() == false)
            {
                playerRoot.animationController.ServerAttack();
                _attackSpeed.Block();
            }
        }

        public async UniTask<bool> MoveToAsync(MonoBehaviour target, float stoppingDistance, Func<bool> shouldCancel, float arrivalSlack = 0.3f)
        {
            if (target == null)
            {
                return false;
            }
            moveStartTime = Time.time;
            navMeshAgent.isStopped = false;
            navMeshAgent.stoppingDistance = stoppingDistance;
            navMeshAgent.SetDestination(target.transform.position);
            while (navMeshAgent.pathPending || navMeshAgent.remainingDistance > (stoppingDistance + arrivalSlack))
            {
                if (shouldCancel != null && shouldCancel())
                {
                    navMeshAgent.ResetPath();
                    return false;
                }
                await UniTask.Yield();
            }
            navMeshAgent.isStopped = true;
            return true;
        }

        public void FaceTarget(Transform target)
        {
            Vector3 dir = target.position - navMeshAgent.transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.0001f)
            {
                return;
            }
            float yAngle = Quaternion.LookRotation(dir).eulerAngles.y;
            navMeshAgent.transform.DORotate(new Vector3(0f, yAngle, 0f), 0.2f).SetUpdate(true);
        }
    }
}
