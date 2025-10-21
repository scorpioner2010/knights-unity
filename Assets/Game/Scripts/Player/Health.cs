using System;
using System.Linq;
using Cysharp.Threading.Tasks;
using FishNet.Object;
using FishNet.Object.Synchronizing; // SyncVar<T>
using Game.Scripts.Core.Services;
using Game.Scripts.UI.HUD;
using Game.Scripts.World.Spawns; // Team, тощо
using UnityEngine;
using UnityEngine.Events;

namespace Game.Scripts.Player
{
    public class Health : NetworkBehaviour
    {
        [Header("HP")]
        public int maxHp = 100;

        private readonly SyncVar<int> _hp = new();
        public int CurrentHp => _hp.Value;

        public Action<int, int, int> OnDamaged; // (damage, currentHp, maxHp)
        public UnityEvent onDeath;

        private HealthBar _healthBar; // локальний HUD (тільки власник)
        public PlayerRoot playerRoot;

        // ----------------- LIFECYCLE -----------------

        public override void OnStartNetwork()
        {
            if (IsServer)
                _hp.Value = Mathf.Clamp(_hp.Value <= 0 ? maxHp : _hp.Value, 0, maxHp);
        }

        public override void OnStartClient()
        {
            _hp.OnChange += OnHpChanged;

            if (IsOwner)
            {
                TryBindHealthBar();
                UpdateOwnerHud(_hp.Value);
            }
        }

        public override void OnStopClient()
        {
            _hp.OnChange -= OnHpChanged;
        }

        private void TryBindHealthBar()
        {
            if (_healthBar == null)
                _healthBar = Singleton<HealthBar>.Instance;
        }

        // ----------------- SERVER API -----------------

        [Server]
        public void SetHpServer(int newHp)
        {
            int clamped = Mathf.Clamp(newHp, 0, maxHp);
            if (_hp.Value == clamped)
                return;

            _hp.Value = clamped;

            if (_hp.Value == 0)
                DeathServer();
        }

        [Server]
        public void ApplyDamageServer(int dmg, Vector3 hitPoint, Vector3 impulse, NetworkObject attacker)
        {
            if (_hp.Value <= 0) return;

            int clamped = Mathf.Max(0, dmg);
            int newHp = Mathf.Max(0, _hp.Value - clamped);
            _hp.Value = newHp;

            HitFxObserversRpc(hitPoint, impulse);

            if (attacker != null && playerRoot != null && playerRoot.serverRoom != null)
            {
                var attackerRoot = playerRoot.serverRoom.players
                    .Select(p => p.playerRoot)
                    .FirstOrDefault(r => r != null && r.OwnerId == attacker.OwnerId);

                if (attackerRoot != null && dmg > 0)
                {
                    int dealt = Mathf.Min(dmg, maxHp); // статистика (безпечна оцінка)
                    attackerRoot.statisticCounter.AddDamage(dealt);
                    if (newHp == 0) attackerRoot.statisticCounter.AddKill();
                }
            }

            if (_hp.Value == 0)
                DeathServer();
        }

        [Server]
        private void DeathServer()
        {
            if (playerRoot != null && !playerRoot.IsDead.Value)
                playerRoot.SetDeadServer();

            OffColliders();

            if (playerRoot != null)
                playerRoot.animationController.TriggerAnimationObserversRpc("Die");

            DiedObserversRpc();

            // 🔁 ПОВЕРНЕНА ЛОГІКА ЗАВЕРШЕННЯ ГРИ
            if (IsOneTeamLeft(out Team leftTeam))
            {
                if (leftTeam != Team.Draw)
                {
                    playerRoot.serverRoom.gameplayTimer.Close();
                }
            }
        }

        // ----------------- CLIENT REACTIONS -----------------

        private void OnHpChanged(int previous, int current, bool asServer)
        {
            int dmg = Mathf.Max(0, previous - current);

            OnDamaged?.Invoke(dmg, current, maxHp);

            if (IsOwner)
            {
                TryBindHealthBar();
                UpdateOwnerHud(current);
                ShowDamageScreen();
            }

            if (current <= 0)
            {
                onDeath?.Invoke();
            }
        }

        private async void ShowDamageScreen()
        {
            DamageScreen.SetActiveScreen(true);
            await UniTask.Delay(300);
            DamageScreen.SetActiveScreen(false);
        }

        [ObserversRpc]
        private void HitFxObserversRpc(Vector3 hitPoint, Vector3 impulse)
        {
            // Локальні VFX/SFX/камерні шейки (за наявності).
        }

        [ObserversRpc]
        private void DiedObserversRpc()
        {
            onDeath?.Invoke();
            OffColliders();
        }

        // ----------------- UTILS -----------------

        private void OffColliders()
        {
            if (playerRoot == null) return;
            if (playerRoot.playerCollider != null) playerRoot.playerCollider.enabled = false;
            if (playerRoot.characterController != null) playerRoot.characterController.enabled = false;
        }

        private void UpdateOwnerHud(int currentHp)
        {
            if (_healthBar == null || _healthBar.healthImage == null) return;
            _healthBar.SetHpView(currentHp, maxHp);
        }

        // ✅ ПОВЕРНУТО: перевірка «одна команда лишилась»
        private bool IsOneTeamLeft(out Team winner)
        {
            winner = default;

            PlayerRoot[] players = playerRoot.serverRoom.players.Select(p => p.playerRoot).ToArray();

            bool anyAlive = false;
            bool hasRed = false;
            bool hasBlue = false;

            for (int i = 0; i < players.Length; i++)
            {
                PlayerRoot p = players[i];
                if (p == null || !p.gameObject.activeInHierarchy || p.IsDead.Value)
                    continue;

                anyAlive = true;

                if (p.Team.Value == Team.Red) hasRed = true;
                else if (p.Team.Value == Team.Blue) hasBlue = true;

                if (hasRed && hasBlue)
                {
                    winner = default;
                    return false; // обидві команди ще живі
                }
            }

            if (!anyAlive)
            {
                winner = default;
                return false; // нікого не лишилось
            }

            winner = hasRed ? Team.Red : Team.Blue;
            return true; // лишилась одна команда
        }
    }
}
