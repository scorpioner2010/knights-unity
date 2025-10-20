using FishNet.Object;
using UnityEngine;

namespace Game.Scripts.Player
{
    [DefaultExecutionOrder(-40)]
    public class CharacterAnimationController : NetworkBehaviour
    {
        public PlayerRoot playerRoot;

        [SerializeField] private float locomotionSendInterval = 0.05f;
        [SerializeField] private float locomotionDeltaEpsilon = 0.01f;
        private float _nextSendTime;
        private float _lastSentLocomotion;

        private bool _shieldLocal;

        private void OnEnable()
        {
            if (playerRoot.characterInput != null)
                playerRoot.characterInput.OnUpdateInput += InputUpdated;
        }

        private void OnDisable()
        {
            if (playerRoot.characterInput != null)
                playerRoot.characterInput.OnUpdateInput -= InputUpdated;
        }

        private void InputUpdated()
        {
            if (!IsOwner) return;
            if (playerRoot.IsDead.Value) return;

            bool shield = playerRoot.characterInput.shieldHeld;
            if (shield != _shieldLocal)
            {
                _shieldLocal = shield;
                SetShieldLocal(_shieldLocal);
                ShieldServerRpc(_shieldLocal);
            }

            if (playerRoot.characterInput.attackPressed)
                AttackServerRpc();
        }

        public float GetLocomotion()
        {
            return playerRoot.animator.GetFloat("Locomotion");
        }

        public void SetLocomotion(float normalizedSpeed01, float lerpParameter)
        {
            float target = Mathf.Clamp01(playerRoot.IsDead.Value ? 0f : normalizedSpeed01);
            float value = Mathf.Lerp(GetLocomotion(), target, Time.fixedDeltaTime * lerpParameter);
            playerRoot.animator.SetFloat("Locomotion", value);
            if (IsOwner) MaybeSendLocomotion(value);
        }

        private void MaybeSendLocomotion(float value)
        {
            if (Time.time < _nextSendTime) return;
            if (Mathf.Abs(value - _lastSentLocomotion) < locomotionDeltaEpsilon) return;
            _nextSendTime = Time.time + locomotionSendInterval;
            _lastSentLocomotion = value;
            LocomotionServerRpc(value);
        }

        [ServerRpc(RequireOwnership = true)]
        private void LocomotionServerRpc(float value)
        {
            LocomotionObserversRpc(value);
        }

        [ObserversRpc]
        private void LocomotionObserversRpc(float value)
        {
            if (IsOwner) return;
            playerRoot.animator.SetFloat("Locomotion", Mathf.Clamp01(value));
        }

        [ServerRpc(RequireOwnership = true)]
        private void ShieldServerRpc(bool state)
        {
            ShieldObserversRpc(state);
        }

        [ObserversRpc]
        private void ShieldObserversRpc(bool state)
        {
            if (IsOwner) return;
            SetShieldLocal(state);
        }

        private void SetShieldLocal(bool state)
        {
            playerRoot.animator.SetBool("Shield", state);
        }

        [ServerRpc(RequireOwnership = true)]
        private void AttackServerRpc()
        {
            if (playerRoot.IsDead.Value) return;
            TriggerAnimation("Attack");
            TriggerAnimationObserversRpc("Attack");
        }

        [ObserversRpc]
        public void TriggerAnimationObserversRpc(string name)
        {
            TriggerAnimation(name);
        }

        private void TriggerAnimation(string name)
        {
            playerRoot.animator.SetTrigger(name);
        }
    }
}
