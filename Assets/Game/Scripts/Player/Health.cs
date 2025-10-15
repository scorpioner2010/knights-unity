using System;
using FishNet.Object;
using Game.Scripts.Core.Services;
using Game.Scripts.Player;
using UnityEngine;
using UnityEngine.Events;

namespace Game.Combat
{
    public class Health : NetworkBehaviour
    {
        public int maxHp = 100;
        public int hp;

        public Action<int, int, int> OnDamaged;
        public UnityEvent onDeath;

        private HealthBar _healthBar;
        public PlayerRoot playerRoot;

        public override void OnStartNetwork()
        {
            if (IsServer)
            {
                hp = maxHp;
            }
        }

        public override void OnStartClient()
        {
            if (IsOwner)
            {
                TryBindHealthBar();
                UpdateOwnerHud(maxHp);
            }
        }

        private void TryBindHealthBar()
        {
            if (_healthBar == null)
            {
                _healthBar = Singleton<HealthBar>.Instance;
            }
        }

        [Server]
        public void ApplyDamage(int dmg, Vector3 hitPoint, Vector3 impulse, int attackId, NetworkObject attacker)
        {
            if (hp <= 0)
            {
                return;
            }

            int oldHp = hp;
            hp = Mathf.Max(0, hp - dmg);
            DamageObserversRpc(dmg, hitPoint, impulse, attackId, attacker.ObjectId, hp);

            if (hp == 0)
            {
                DeathServer();
            }
        }

        [Server]
        private void DeathServer()
        {
            if (!playerRoot.Dead.Value)
            {
                playerRoot.SetDeadServer(true);
            }
            playerRoot.animationController.TriggerAnimationObserversRpc("Die");
            DiedObserversRpc();
        }

        [ObserversRpc]
        private void DamageObserversRpc(int dmg, Vector3 hitPoint, Vector3 impulse, int attackId, int attackerObjectId, int newHp)
        {
            hp = newHp;
            OnDamaged?.Invoke(dmg, newHp, maxHp);
            if (!IsOwner)
            {
                return;
            }
            TryBindHealthBar();
            UpdateOwnerHud(newHp);
        }

        [ObserversRpc]
        private void DiedObserversRpc()
        {
            onDeath?.Invoke();
        }

        private void UpdateOwnerHud(int currentHp)
        {
            if (_healthBar == null || _healthBar.slider == null)
            {
                return;
            }
            _healthBar.SetHpView(currentHp, maxHp);
        }
    }
}
