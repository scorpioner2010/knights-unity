using FishNet.Object;
using UnityEngine;

namespace Game.Scripts.Player
{
    [DefaultExecutionOrder(-40)]
    public class CharacterAnimationController : NetworkBehaviour
    {
        private bool _isJumping;
        public PlayerRoot playerRoot;

        // throttle для відправки бігового параметра
        [SerializeField] private float locomotionSendInterval = 0.05f; // 20 раз/с
        [SerializeField] private float locomotionDeltaEpsilon = 0.01f;  // поріг зміни
        private float _nextSendTime;
        private float _lastSentLocomotion;

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
            if (!IsOwner)
                return;

            // Стрибок
            if (playerRoot.characterInput.jumpPressed &&
                playerRoot.groundChecker.isGrounded &&
                !_isJumping)
            {
                _isJumping = true;
                JumpServerRpc();
            }

            // 🔥 Атака — тригер "Attack"
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
                    _isJumping = false;
            }
            else if (IsServer)
            {
                // Логіка ботів (за потреби)
            }
        }

        public float GetLocomotion()
        {
            return playerRoot.animator.GetFloat("Locomotion");
        }

        /// <summary>
        /// Власник викликає це щофікс-кадр (див. CharacterMovement.FixedUpdate).
        /// Локально робимо плавний лерп і, якщо змінились суттєво — шлемо на сервер для ретрансляції.
        /// </summary>
        public void SetLocomotion(float normalizedSpeed01, float lerpParameter)
        {
            float target = Mathf.Clamp01(normalizedSpeed01);
            float value = Mathf.Lerp(GetLocomotion(), target, Time.fixedDeltaTime * lerpParameter);
            playerRoot.animator.SetFloat("Locomotion", value);

            if (IsOwner)
                MaybeSendLocomotion(value);
        }

        private void MaybeSendLocomotion(float value)
        {
            if (Time.time < _nextSendTime)
                return;

            if (Mathf.Abs(value - _lastSentLocomotion) < locomotionDeltaEpsilon)
                return;

            _nextSendTime = Time.time + locomotionSendInterval;
            _lastSentLocomotion = value;
            LocomotionServerRpc(value);
        }

        // Сервер приймає значення від власника і розсилає спостерігачам
        [ServerRpc(RequireOwnership = true)]
        private void LocomotionServerRpc(float value)
        {
            LocomotionObserversRpc(value);
        }

        // На віддалених клієнтах ставимо значення напряму (без додаткового лерпу)
        [ObserversRpc]
        private void LocomotionObserversRpc(float value)
        {
            if (IsOwner)
                return; // власник уже оновив локально

            playerRoot.animator.SetFloat("Locomotion", Mathf.Clamp01(value));
        }

        [ServerRpc(RequireOwnership = true)]
        private void JumpServerRpc()
        {
            TriggerAnimation("Jump");
            TriggerAnimationObserversRpc("Jump");
        }

        // ✅ Атака по мережі
        [ServerRpc(RequireOwnership = true)]
        private void AttackServerRpc()
        {
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
