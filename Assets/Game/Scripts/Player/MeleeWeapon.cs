using FishNet.Object;
using UnityEngine;
using System.Threading;
using Cysharp.Threading.Tasks;
using FishNet;
using Game.Scripts.Player;

namespace Game.Combat
{
    /// <summary>
    /// Сервер відкриває хіт-вікно прямо з Animation Event (AE_Hit) — без очікування RPC.
    /// Капсульний свіп між позиціями леза: OverlapCapsule(prevTip, curTip, sweepRadius, hitMask).
    /// </summary>
    public class MeleeWeapon : NetworkBehaviour
    {
        public PlayerRoot  playerRoot;
        
        public Transform bladeRoot;              // біля руків'я (world)
        public Transform bladeTip;               // кінчик леза (world)
        
        public LayerMask hitMask;                // шари ворогів
        [Range(0.01f, 0.5f)] public float sweepRadius = 0.12f;
        
        public int damage = 30;                  // урон за атаку
        public float hitWindow = 0.25f;          // тривалість активної фази
        
        public bool drawGizmos = true;
        public Color gizmoCurrentColor = new (0.1f, 0.8f, 1f, 0.8f);
        public Color gizmoPrevColor    = new (1f,   0.7f, 0.1f, 0.8f);
        public Color gizmoSweepColor   = new (1f, 0.95f, 0.1f, 0.8f);

        // runtime
        private CancellationTokenSource _cts;
        private int _attackId = 0;

        // для гізмосів
        Vector3 _gPrevTip, _gCurTip, _gPrevRoot, _gCurRoot;

        
        // ===== ДОБАВЬ ці поля у клас =====
        private static readonly Collider[] _vfxBuf = new Collider[8];
        private CancellationTokenSource _vfxCts;
        [SerializeField] private float localVfxWindow = 0.18f; // тривалість локального вікна (<= hitWindow)
        
        // ===== 1) ЛОКАЛЬНЕ ВІКНО VFX: ТОЧНО ТАКИЙ САМИЙ СВІП, ЯК НА СЕРВЕРІ =====
        // Викликається Animation Event'ом на ВЛАСНИКУ (без параметрів).
        public void AE_TryLocalHitVfx()
        {
            if (!IsOwner) return;

            _vfxCts?.Cancel();
            _vfxCts = new CancellationTokenSource();
            _ = LocalVfxWindowAsync(_vfxCts.Token);
        }
        
        private async UniTaskVoid LocalVfxWindowAsync(CancellationToken token)
        {
            // Дзеркалим сервер: свіп між prevTip → curTip упродовж короткого вікна
            float duration = Mathf.Min(hitWindow, localVfxWindow);
            float tEnd = Time.time + duration;

            var hitOnce = new System.Collections.Generic.HashSet<int>();

            Vector3 prevTip  = bladeTip  ? bladeTip.position  : transform.position + transform.forward * 1f;
            Vector3 prevRoot = bladeRoot ? bladeRoot.position : transform.position;

            try
            {
                while (Time.time < tEnd)
                {
                    await UniTask.Yield(PlayerLoopTiming.Update, token);

                    Vector3 curTip  = bladeTip  ? bladeTip.position  : prevTip;
                    Vector3 curRoot = bladeRoot ? bladeRoot.position : prevRoot;

                    // ТОЙ САМИЙ свіп, що й на сервері: капсула по траєкторії кінчика
                    int count = Physics.OverlapCapsuleNonAlloc(prevTip, curTip, sweepRadius, _vfxBuf, hitMask, QueryTriggerInteraction.Ignore);
                    for (int i = 0; i < count; i++)
                    {
                        var col = _vfxBuf[i]; if (col == null) continue;
                        var nob = col.GetComponentInParent<FishNet.Object.NetworkObject>();
                        if (nob == null || (base.NetworkObject != null && nob.ObjectId == base.NetworkObject.ObjectId))
                            continue; // не себе
                        if (!hitOnce.Add(nob.ObjectId))
                            continue; // по кожній цілі — один спарк

                        Vector3 hitPointWorld = col.ClosestPoint(curTip);
                        ShowHitVfxAt(hitPointWorld); // миттєвий prediction VFX у власника
                    }

                    prevTip  = curTip;
                    prevRoot = curRoot;
                }
            }
            catch (System.OperationCanceledException) { /* нормальна відміна */ }
        }

        private void ShowHitVfxAt(Vector3 hitPointWorld)
        {
            playerRoot.characterParticles.HitEffectPlay(hitPointWorld);
        }
        
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
                        Collider c = cols[i]; if (c == null) continue;

                        GameObject go  = c.attachedRigidbody ? c.attachedRigidbody.gameObject : c.gameObject;
                        NetworkObject nob = go.GetComponentInParent<NetworkObject>();
                        if (nob == null || nob.ObjectId == ObjectId) continue;   // не себе
                        if (!hitOnce.Add(nob.ObjectId)) continue;                // одну ціль — один раз

                        Health hp = nob.GetComponentInParent<Health>();
                        if (hp != null)
                        {
                            Vector3 hitPoint = bladeTip ? bladeTip.position : transform.position + transform.forward * 0.8f;
                            Vector3 impulse  = transform.forward * 6f;
                            Vector3 hitPointWorld = c.ClosestPoint(curTip);
                            BroadcastHitVfx(nob, hitPointWorld);
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
        
        [Server]
        public void BroadcastHitVfx(NetworkObject target, Vector3 hitPointWorld)
        {
            if (target == null) return;
            Vector3 localPoint = target.transform.InverseTransformPoint(hitPointWorld);
            HitVfxObserversRpc(target, localPoint); // передаємо сам NetworkObject, НЕ шукаємо через SpawnManager
        }

        [ObserversRpc]
        private void HitVfxObserversRpc(NetworkObject target, Vector3 localHitPoint)
        {
            if (target == null) return; // якщо ціль вже деcпавнена
            Vector3 worldPoint = target.transform.TransformPoint(localHitPoint);
            ServerShowHitVfxAt(worldPoint);
        }
        
        private void ServerShowHitVfxAt(Vector3 worldPoint)
        {
            playerRoot.characterParticles.HitEffectPlayServer(worldPoint);
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
