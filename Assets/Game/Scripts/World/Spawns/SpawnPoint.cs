using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using Game.Scripts.Core.Helpers;
using Game.Scripts.Networking.Lobby;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Scripts.World.Spawns
{
    public class SpawnPoint : NetworkBehaviour
    {
        public readonly SyncVar<bool> IsNotFree = new(false);
        public Team team;

        private async void MarkPoint()
        {
            IsNotFree.Value = true;
            await UniTask.Delay(20000);
            IsNotFree.Value = false;
        }

        private void Awake()
        {
            MeshRenderer mesh = GetComponent<MeshRenderer>();
            
            if (mesh != null)
            {
                mesh.enabled = false;
            }
        }

        public static SpawnPoint GetFreePoint(Scene scene, Team team)
        {
            List<SpawnPoint> allPoints = GameplaySpawner.FindObjectsInScene<SpawnPoint>(scene);
            List<SpawnPoint> freeBySide = new();

            for (int i = 0; i < allPoints.Count; i++)
            {
                SpawnPoint p = allPoints[i];
                
                if (p.team == team)
                {
                    if (p.IsNotFree.Value == false)
                    {
                        freeBySide.Add(p);
                    }
                }
            }

            SpawnPoint pick = freeBySide.RandomElement();
            pick.MarkPoint();
            
            return pick;
        }
    }

    public enum Team
    {
        Draw = 0,
        Red = 1,
        Blue = 2,
    }
}