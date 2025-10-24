using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Game.Scripts.Networking.Lobby;
using Game.Scripts.Player;
using UnityEngine;
using UnityEngine.AI;

namespace Game.Scripts
{
    public class BotDisperser : MonoBehaviour
    {
        public PlayerRoot playerRoot;
        public NavMeshAgent navMeshAgent;
        public float desiredDistance = 10f;
        public float sampleRadius = 12f;
        public float minClearDistance = 2.5f;
        public float reservationRadius = 2f;
        public float settleDelaySeconds = 2f;

        private static readonly List<Vector3> Reserved = new ();

        public async UniTask<bool> DisperseAsync(Bot bot)
        {
            Vector3 origin = navMeshAgent.transform.position;
            Vector3 target;
            bool ok = TryFindPoint(origin, out target);
            if (!ok)
            {
                return false;
            }
            Reserve(target);
            bool reached = await bot.MoveToAsync(target, 0.15f, () => false, 0.2f);
            if (!reached)
            {
                Release(target);
                return false;
            }
            await UniTask.Delay((int)(settleDelaySeconds * 1000f));
            Release(target);
            return true;
        }

        private bool TryFindPoint(Vector3 origin, out Vector3 point)
        {
            point = origin;
            int samples = 24;
            float step = 360f / samples;
            float offset = Random.value * 360f;
            for (int i = 0; i < samples * 2; i++)
            {
                float ang = offset + step * i;
                Vector3 dir = Quaternion.Euler(0f, ang, 0f) * Vector3.forward;
                Vector3 guess = origin + dir * desiredDistance;
                NavMeshHit hit;
                bool onMesh = NavMesh.SamplePosition(guess, out hit, sampleRadius, NavMesh.AllAreas);
                if (!onMesh)
                {
                    continue;
                }
                if (!IsPathReachable(origin, hit.position))
                {
                    continue;
                }
                if (!IsNotReserved(hit.position))
                {
                    continue;
                }
                if (!IsClearOfPlayers(hit.position))
                {
                    continue;
                }
                point = hit.position;
                return true;
            }
            return false;
        }

        private bool IsPathReachable(Vector3 start, Vector3 end)
        {
            NavMeshPath p = new NavMeshPath();
            bool ok = NavMesh.CalculatePath(start, end, NavMesh.AllAreas, p);
            if (!ok)
            {
                return false;
            }
            return p.status == NavMeshPathStatus.PathComplete;
        }

        private bool IsNotReserved(Vector3 p)
        {
            float r2 = reservationRadius * reservationRadius;
            for (int i = 0; i < Reserved.Count; i++)
            {
                if ((Reserved[i] - p).sqrMagnitude < r2)
                {
                    return false;
                }
            }
            return true;
        }

        private bool IsClearOfPlayers(Vector3 p)
        {
            List<PlayerRoot> players = GameplaySpawner.FindObjectsInScene<PlayerRoot>(playerRoot.characterInit.currentScene);
            float r2 = minClearDistance * minClearDistance;
            for (int i = 0; i < players.Count; i++)
            {
                if ((players[i].transform.position - p).sqrMagnitude < r2)
                {
                    return false;
                }
            }
            return true;
        }

        private void Reserve(Vector3 p)
        {
            Reserved.Add(p);
        }

        private void Release(Vector3 p)
        {
            float r2 = reservationRadius * reservationRadius;
            for (int i = Reserved.Count - 1; i >= 0; i--)
            {
                if ((Reserved[i] - p).sqrMagnitude < r2)
                {
                    Reserved.RemoveAt(i);
                }
            }
        }
    }
}
