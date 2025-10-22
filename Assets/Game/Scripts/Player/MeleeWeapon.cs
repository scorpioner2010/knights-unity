using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using Game.Scripts.Core.Helpers;
using UnityEngine;

namespace Game.Scripts.Player
{
    public class MeleeWeapon : NetworkBehaviour
    {
        public PlayerRoot playerRoot;

        private Transform _bladeRoot;
        private Transform _bladeTip;

        public LayerMask hitMask;
        private float _sweepRadius = 0.12f;

        public int damageTest;
        private readonly SyncVar<int> _damage = new();

        private CancellationTokenSource _cts;

        private static readonly Collider[] VFXBuf = new Collider[8];
        private CancellationTokenSource _vfxCts;

        private float _localVfxWindow = 0.18f;
        private float _hitWindow = 0.18f;

        public void InitWeapon(Transform bladeRoot, Transform bladeTip)
        {
            _bladeRoot = bladeRoot;
            _bladeTip = bladeTip;
        }

        public override void OnStartServer()
        {
            playerRoot.animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        }

        private void Awake()
        {
            _cts = new CancellationTokenSource();
        }

        private void OnDestroy()
        {
            CancelTasks();
        }

        private void Update()
        {
            damageTest = _damage.Value;
        }

        public void SetDamage(int damage)
        {
            GameplayAssistant.SetNetworkParameter(_damage, damage);
        }

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
            if (_bladeTip == null || _bladeRoot == null)
            {
                return;
            }

            float duration = Mathf.Min(_hitWindow, _localVfxWindow);
            float tEnd = Time.time + duration;

            HashSet<int> hitOnce = new HashSet<int>();

            Vector3 prevTip = _bladeTip ? _bladeTip.position : transform.position + transform.forward * 1f;
            Vector3 prevRoot = _bladeRoot ? _bladeRoot.position : transform.position;

            try
            {
                while (Time.time < tEnd)
                {
                    await UniTask.Yield(PlayerLoopTiming.Update, token);

                    Vector3 curTip = _bladeTip ? _bladeTip.position : prevTip;
                    Vector3 curRoot = _bladeRoot ? _bladeRoot.position : prevRoot;

                    int count = Physics.OverlapCapsuleNonAlloc(prevTip, curTip, _sweepRadius, VFXBuf, hitMask, QueryTriggerInteraction.Ignore);
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

            _ = ServerMeleeWindowAsync(_damage.Value, _hitWindow, _cts.Token);
        }

        private async UniTaskVoid ServerMeleeWindowAsync(int dmg, float window, CancellationToken token)
        {
            if (_bladeTip == null || _bladeRoot == null)
            {
                return;
            }

            if (!IsServer)
            {
                return;
            }

            float tEnd = Time.time + window;

            HashSet<int> hitOnce = new HashSet<int>();
            Vector3 prevRoot = _bladeRoot ? _bladeRoot.position : transform.position;
            Vector3 prevTip = _bladeTip ? _bladeTip.position : transform.position + transform.forward * 1f;

            try
            {
                while (Time.time < tEnd)
                {
                    await UniTask.Yield(PlayerLoopTiming.Update, token);

                    Vector3 curRoot = _bladeRoot ? _bladeRoot.position : prevRoot;
                    Vector3 curTip = _bladeTip ? _bladeTip.position : prevTip;

                    Collider[] cols = Physics.OverlapCapsule(prevTip, curTip, _sweepRadius, hitMask, QueryTriggerInteraction.Ignore);

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

                        Vector3 hitPoint = _bladeTip ? _bladeTip.position : transform.position + transform.forward * 0.8f;
                        Vector3 impulse = transform.forward * 6f;
                        Vector3 hitPointWorld = c.ClosestPoint(curTip);
                        BroadcastHitVfx(target.networkObject, hitPointWorld);
                        target.health.ApplyDamageServer(dmg, hitPoint, impulse, NetworkObject);
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
