using FishNet.Object;
using UnityEngine;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Game.Combat
{
    /// <summary>
    /// Сервер відкриває хіт-вікно прямо з Animation Event (AE_Hit) — без очікування RPC.
    /// Капсульний свіп між позиціями леза: OverlapCapsule(prevTip, curTip, sweepRadius, hitMask).
    /// </summary>
    public class MeleeWeapon : NetworkBehaviour
    {
        [Header("Refs")]
        public Transform bladeRoot;              // біля руків'я (world)
        public Transform bladeTip;               // кінчик леза (world)

        [Header("Hit Settings")]
        public LayerMask hitMask;                // шари ворогів
        [Range(0.01f, 0.5f)] public float sweepRadius = 0.12f;

        [Header("Balance")]
        public int damage = 30;                  // урон за атаку
        public float hitWindow = 0.25f;          // тривалість активної фази

        [Header("Debug Gizmos")]
        public bool drawGizmos = true;
        public Color gizmoCurrentColor = new Color(0.1f, 0.8f, 1f, 0.8f);
        public Color gizmoPrevColor    = new Color(1f,   0.7f, 0.1f, 0.8f);
        public Color gizmoSweepColor   = new Color(1f, 0.95f, 0.1f, 0.8f);

        // runtime
        private CancellationTokenSource _cts;
        private int _attackId = 0;

        // для гізмосів
        Vector3 _gPrevTip, _gCurTip, _gPrevRoot, _gCurRoot;

        private void OnEnable()
        {
            _cts = new CancellationTokenSource();
            SnapGizmoPositions();
        }

        private void OnDisable() => CancelTasks();
        public override void OnStopNetwork() { base.OnStopNetwork(); CancelTasks(); }

        private void CancelTasks()
        {
            if (_cts != null) { _cts.Cancel(); _cts.Dispose(); _cts = null; }
        }

        void LateUpdate()
        {
            if (bladeRoot) _gCurRoot = bladeRoot.position;
            if (bladeTip)  _gCurTip  = bladeTip.position;
        }

        void SnapGizmoPositions()
        {
            var p = transform.position;
            _gPrevRoot = _gCurRoot = bladeRoot ? bladeRoot.position : p;
            _gPrevTip  = _gCurTip  = bladeTip  ? bladeTip.position  : p + transform.forward * 1f;
        }

        // === ВИКЛИКАЄТЬСЯ ІЗ ANIMATION EVENT НА ОБ'ЄКТІ З ANIMATOR ===
        public void AE_Hit()
        {
            // На клієнтах можеш увімкнути VFX/SFX локально; урон вважає тільки сервер
            if (IsServer)
                StartHitWindowOnServer();
        }

        private void StartHitWindowOnServer()
        {
            if (_cts == null) _cts = new CancellationTokenSource();
            _attackId++;
            _ = ServerMeleeWindowAsync(_attackId, damage, hitWindow, _cts.Token);
        }

        /// <summary>Серверне «вікно» урону: свіп капсулою між позиціями леза протягом hitWindow.</summary>
        private async UniTaskVoid ServerMeleeWindowAsync(int attackId, int dmg, float window, CancellationToken token)
        {
            if (!IsServer) return;

            float tEnd = Time.time + window;

            var hitOnce = new System.Collections.Generic.HashSet<int>();
            Vector3 prevRoot = bladeRoot ? bladeRoot.position : transform.position;
            Vector3 prevTip  = bladeTip  ? bladeTip.position  : transform.position + transform.forward * 1f;

            try
            {
                while (Time.time < tEnd)
                {
                    await UniTask.Yield(PlayerLoopTiming.Update, token);

                    Vector3 curRoot = bladeRoot ? bladeRoot.position : prevRoot;
                    Vector3 curTip  = bladeTip  ? bladeTip.position  : prevTip;

                    // Саме це й «б'є»: капсула по траєкторії кінчика леза
                    var cols = Physics.OverlapCapsule(prevTip, curTip, sweepRadius, hitMask, QueryTriggerInteraction.Ignore);
                    for (int i = 0; i < cols.Length; i++)
                    {
                        var c = cols[i]; if (c == null) continue;

                        var go  = c.attachedRigidbody ? c.attachedRigidbody.gameObject : c.gameObject;
                        var nob = go.GetComponentInParent<NetworkObject>();
                        if (nob == null || nob.ObjectId == ObjectId) continue;   // не себе
                        if (!hitOnce.Add(nob.ObjectId)) continue;                // одну ціль — один раз

                        var hp = nob.GetComponentInParent<Health>();
                        if (hp != null)
                        {
                            Vector3 hitPoint = bladeTip ? bladeTip.position : transform.position + transform.forward * 0.8f;
                            Vector3 impulse  = transform.forward * 6f;
                            hp.ApplyDamage(dmg, hitPoint, impulse, attackId, base.NetworkObject);
                        }
                    }

                    // оновлюємо попередні точки та гізмоси
                    _gPrevRoot = prevRoot = curRoot;
                    _gPrevTip  = prevTip  = curTip;
                    _gCurRoot  = curRoot;
                    _gCurTip   = curTip;
                }
            }
            catch (System.OperationCanceledException) { /* нормальна відміна */ }
        }

        void OnDrawGizmos()
        {
            if (!drawGizmos) return;

            Gizmos.color = gizmoCurrentColor;
            Gizmos.DrawLine(_gCurRoot, _gCurTip);
            Gizmos.DrawWireSphere(_gCurRoot, sweepRadius);
            Gizmos.DrawWireSphere(_gCurTip,  sweepRadius);

            Gizmos.color = gizmoPrevColor;
            Gizmos.DrawLine(_gPrevRoot, _gPrevTip);
            Gizmos.DrawWireSphere(_gPrevRoot, sweepRadius * 0.85f);
            Gizmos.DrawWireSphere(_gPrevTip,  sweepRadius * 0.85f);

            Gizmos.color = gizmoSweepColor;
            Gizmos.DrawLine(_gPrevTip, _gCurTip);
            Gizmos.DrawWireSphere(_gPrevTip, sweepRadius * 0.7f);
            Gizmos.DrawWireSphere(_gCurTip,  sweepRadius * 0.7f);
        }
    }
}
