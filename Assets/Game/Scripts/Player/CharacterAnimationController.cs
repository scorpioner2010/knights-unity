using FishNet.Object;
using UnityEngine;

namespace Game.Scripts.Player
{
    [DefaultExecutionOrder(-40)]
    public class CharacterAnimationController : NetworkBehaviour
    {
        private bool _isJumping;
        public PlayerRoot playerRoot;
        
        private float _newSpeedLocomotion;

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

            // Стрибок
            if (playerRoot.characterInput.jumpPressed &&
                playerRoot.groundChecker.isGrounded &&
                !_isJumping)
            {
                _isJumping = true;
                PlayJumpSound();
                JumpServerRpc();
            }

            // Атака
            
            //if (playerRoot.characterInput.attackPressed && !BlockAttack(currentWeapon))
            //{
            //    AttackServerRpc(currentWeapon);
            //}
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
            else if (IsServer)
            {
                // Логіка ботів (за потреби)
            }
        }

        public float GetLocomotion()
        {
            return playerRoot.animator.GetFloat("Locomotion");
        }
        
        public void SetLocomotion(float speed, float lerpParameter)
        {
            float  lerpSpeed = Mathf.Lerp(GetLocomotion(), speed, Time.fixedDeltaTime * lerpParameter);
            playerRoot.animator.SetFloat("Locomotion", lerpSpeed);
        }

        private void PlayJumpSound()
        {
        }

        [ServerRpc(RequireOwnership = true)]
        private void JumpServerRpc()
        {
            TriggerAnimation("Jump");
            TriggerAnimationObserversRpc("Jump");
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
