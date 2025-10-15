using FishNet.Object;
using UnityEngine;

namespace Game.Scripts.Player
{
    [DefaultExecutionOrder(-40)]
    public class CharacterAnimationController : NetworkBehaviour
    {
        private bool _isJumping;
        public PlayerRoot playerRoot;

        [SerializeField] private float locomotionSendInterval = 0.05f;
        [SerializeField] private float locomotionDeltaEpsilon = 0.01f;
        private float _nextSendTime;
        private float _lastSentLocomotion;

        private void OnEnable()
        {
            if (playerRoot.characterInput != null)
            {
                playerRoot.characterInput.OnUpdateInput += InputUpdated;
            }
        }

        private void OnDisable()
        {
            if (playerRoot.characterInput != null)
            {
                playerRoot.characterInput.OnUpdateInput -= InputUpdated;
            }
        }

        private void InputUpdated()
        {
            if (!IsOwner)
            {
                return;
            }

            if (playerRoot.Dead.Value)
            {
                return;
            }

            if (playerRoot.characterInput.jumpPressed && playerRoot.groundChecker.isGrounded && !_isJumping)
            {
                _isJumping = true;
                JumpServerRpc();
            }

            if (playerRoot.characterInput.attackPressed)
            {
                AttackServerRpc();
            }
        }

        private void Update()
        {
            if (IsOwner)
            {
                if (playerRoot.groundChecker.isGrounded && _isJumping)
                {
                    _isJumping = false;
                }
            }
        }

        public float GetLocomotion()
        {
            return playerRoot.animator.GetFloat("Locomotion");
        }

        public void SetLocomotion(float normalizedSpeed01, float lerpParameter)
        {
            float target = Mathf.Clamp01(playerRoot.Dead.Value ? 0f : normalizedSpeed01);
            float value = Mathf.Lerp(GetLocomotion(), target, Time.fixedDeltaTime * lerpParameter);
            playerRoot.animator.SetFloat("Locomotion", value);
            if (IsOwner)
            {
                MaybeSendLocomotion(value);
            }
        }

        private void MaybeSendLocomotion(float value)
        {
            if (Time.time < _nextSendTime)
            {
                return;
            }
            if (Mathf.Abs(value - _lastSentLocomotion) < locomotionDeltaEpsilon)
            {
                return;
            }
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
            if (IsOwner)
            {
                return;
            }
            playerRoot.animator.SetFloat("Locomotion", Mathf.Clamp01(value));
        }

        [ServerRpc(RequireOwnership = true)]
        private void JumpServerRpc()
        {
            TriggerAnimation("Jump");
            TriggerAnimationObserversRpc("Jump");
        }

        [ServerRpc(RequireOwnership = true)]
        private void AttackServerRpc()
        {
            if (playerRoot.Dead.Value)
            {
                return;
            }
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
