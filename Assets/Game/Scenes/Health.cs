using FishNet.Object;
using Game.Scripts.Core.Services;
using Game.Scripts.Player;
using UnityEngine;

namespace Game.Combat
{
    public class Health : NetworkBehaviour
    {
        [Header("HP")]
        public int maxHp = 100;
        public int hp;

        private HealthBar _healthBar;     // локальний HUD (тільки для власника)
        public PlayerRoot playerRoot;

        public override void OnStartNetwork()
        {
            base.OnStartNetwork();
            if (IsServer)
                hp = maxHp;
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            // HUD показуємо лише власнику
            if (IsOwner)
            {
                TryBindHealthBar();
                UpdateOwnerHud(maxHp); // старт — повна смуга
            }
        }

        private void TryBindHealthBar()
        {
            if (_healthBar == null)
                _healthBar = Singleton<HealthBar>.Instance; // може бути null у перші кадри — не страшно
        }

        [Server]
        public void ApplyDamage(int amount, Vector3 hitPoint, Vector3 impulse, int attackId, NetworkObject attacker)
        {
            if (hp <= 0) return;

            hp = Mathf.Max(0, hp - amount);

            // широкомовний фідбек (і відправляємо актуальний hp)
            DamageObserversRpc(amount, hitPoint, impulse, attackId, attacker.ObjectId, hp);

            if (hp == 0)
            {
                // TODO: death logic (disable input, ragdoll, respawn, etc.)
            }
        }

        [ObserversRpc]
        private void DamageObserversRpc(int amount, Vector3 hitPoint, Vector3 impulse, int attackId, int attackerObjectId, int newHp)
        {
            // синхронізуємо локальне значення hp для консистентності клієнтів
            hp = newHp;

            // HUD оновлює лише власник; інші клієнти HUD не мають або мають свій
            if (!IsOwner)
                return;

            TryBindHealthBar();
            UpdateOwnerHud(newHp);
        }

        private void UpdateOwnerHud(int currentHp)
        {
            if (_healthBar == null || _healthBar.slider == null)
                return;

            float max = Mathf.Max(1f, maxHp);
            float cur01 = Mathf.Clamp01(currentHp / max); // float-ділення, не інт
            _healthBar.slider.value = cur01;
        }
    }
}
