using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using FishNet.Object;
using UnityEngine;

namespace Game.Scripts.Player
{
    public class MeleeWeapon : NetworkBehaviour
    {
        public PlayerRoot playerRoot;

        public Transform bladeRoot;
        public Transform bladeTip;

        public LayerMask hitMask;
        [Range(0.01f, 0.5f)] public float sweepRadius = 0.12f;

        public int damage = 30;
        public float hitWindow = 0.25f;

        private CancellationTokenSource _cts;

        private static readonly Collider[] VFXBuf = new Collider[8];
        private CancellationTokenSource _vfxCts;
        [SerializeField] private float localVfxWindow = 0.18f;

        public void AE_TryLocalHitVfx()
        {
            if (!IsOwner)
            {
                return;
            }

            _vfxCts?.Cancel();
            _vfxCts = new CancellationTokenSource();
            _ = LocalVfxWindowAsync(_vfxCts.Token);
        }

        private async UniTaskVoid LocalVfxWindowAsync(CancellationToken token)
        {
            float duration = Mathf.Min(hitWindow, localVfxWindow);
            float tEnd = Time.time + duration;

            HashSet<int> hitOnce = new HashSet<int>();

            Vector3 prevTip = bladeTip ? bladeTip.position : transform.position + transform.forward * 1f;
            Vector3 prevRoot = bladeRoot ? bladeRoot.position : transform.position;

            try
            {
                while (Time.time < tEnd)
                {
                    await UniTask.Yield(PlayerLoopTiming.Update, token);

                    Vector3 curTip = bladeTip ? bladeTip.position : prevTip;
                    Vector3 curRoot = bladeRoot ? bladeRoot.position : prevRoot;

                    int count = Physics.OverlapCapsuleNonAlloc(prevTip, curTip, sweepRadius, VFXBuf, hitMask, QueryTriggerInteraction.Ignore);
                    for (int i = 0; i < count; i++)
                    {
                        Collider col = VFXBuf[i];
                        if (col == null)
                        {
                            continue;
                        }
                        
                        PlayerRoot target = col.gameObject.GetComponentInParent<PlayerRoot>();
                        
                        if (target.networkObject == null || (NetworkObject != null && target.networkObject.ObjectId == NetworkObject.ObjectId))
                        {
                            continue;
                        }

                        if (!hitOnce.Add(target.networkObject.ObjectId))
                        {
                            continue;
                        }

                        Vector3 hitPointWorld = col.ClosestPoint(curTip);
                        ShowHitVfxAt(hitPointWorld);
                    }

                    prevTip = curTip;
                    prevRoot = curRoot;
                }
            }
            catch (System.OperationCanceledException)
            {
            }
        }

        private void ShowHitVfxAt(Vector3 hitPointWorld)
        {
            playerRoot.characterParticles.HitEffectPlay(hitPointWorld);
        }

        private void OnEnable()
        {
            _cts = new CancellationTokenSource();
        }

        private void OnDisable()
        {
            CancelTasks();
        }

        public override void OnStopNetwork()
        {
            base.OnStopNetwork();
            CancelTasks();
        }

        private void CancelTasks()
        {
            if (_cts != null)
            {
                _cts.Cancel();
                _cts.Dispose();
                _cts = null;
            }
        }

        public void AE_Hit()
        {
            if (IsServer)
            {
                StartHitWindowOnServer();
            }
        }

        private void StartHitWindowOnServer()
        {
            if (_cts == null)
            {
                _cts = new CancellationTokenSource();
            }
            
            _ = ServerMeleeWindowAsync(damage, hitWindow, _cts.Token);
        }

        private async UniTaskVoid ServerMeleeWindowAsync(int dmg, float window, CancellationToken token)
        {
            if (!IsServer)
            {
                return;
            }

            float tEnd = Time.time + window;

            HashSet<int> hitOnce = new HashSet<int>();
            Vector3 prevRoot = bladeRoot ? bladeRoot.position : transform.position;
            Vector3 prevTip = bladeTip ? bladeTip.position : transform.position + transform.forward * 1f;

            try
            {
                while (Time.time < tEnd)
                {
                    await UniTask.Yield(PlayerLoopTiming.Update, token);

                    Vector3 curRoot = bladeRoot ? bladeRoot.position : prevRoot;
                    Vector3 curTip = bladeTip ? bladeTip.position : prevTip;

                    Collider[] cols = Physics.OverlapCapsule(prevTip, curTip, sweepRadius, hitMask, QueryTriggerInteraction.Ignore);

                    for (int i = 0; i < cols.Length; i++)
                    {
                        Collider c = cols[i];
                        
                        if (c == null)
                        {
                            continue;
                        }
                        
                        PlayerRoot target = c.gameObject.GetComponentInParent<PlayerRoot>();

                        if (target != null && playerRoot != null && target.Team.Value == playerRoot.Team.Value)
                        {
                            continue;
                        }

                        if (target.networkObject.ObjectId == ObjectId)
                        {
                            continue;
                        }

                        if (!hitOnce.Add(target.networkObject.ObjectId))
                        {
                            continue;
                        }

                        Vector3 hitPoint = bladeTip ? bladeTip.position : transform.position + transform.forward * 0.8f;
                        Vector3 impulse = transform.forward * 6f;
                        Vector3 hitPointWorld = c.ClosestPoint(curTip);
                        BroadcastHitVfx(target.networkObject, hitPointWorld);
                        target.health.ApplyDamage(dmg, hitPoint, impulse, NetworkObject);
                    }

                    prevRoot = curRoot;
                    prevTip = curTip;
                }
            }
            catch (System.OperationCanceledException)
            {
            }
        }

        [Server]
        public void BroadcastHitVfx(NetworkObject target, Vector3 hitPointWorld)
        {
            if (target == null)
            {
                return;
            }

            Vector3 localPoint = target.transform.InverseTransformPoint(hitPointWorld);
            HitVfxObserversRpc(target, localPoint);
        }

        [ObserversRpc]
        private void HitVfxObserversRpc(NetworkObject target, Vector3 localHitPoint)
        {
            if (target == null)
            {
                return;
            }

            Vector3 worldPoint = target.transform.TransformPoint(localHitPoint);
            ServerShowHitVfxAt(worldPoint);
        }

        private void ServerShowHitVfxAt(Vector3 worldPoint)
        {
            playerRoot.characterParticles.HitEffectPlayServer(worldPoint);
        }
    }
}
