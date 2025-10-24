using FishNet.Object;
using UnityEngine;

namespace Game.Scripts.Player
{
    [DefaultExecutionOrder(-40)]
    public class CharacterAnimationController : NetworkBehaviour
    {
        public PlayerRoot playerRoot;

        public float locomotionSendInterval = 0.05f;
        public float locomotionDeltaEpsilon = 0.01f;
        
        private float _nextSendTime;
        private float _lastSentLocomotion;
        private bool _shieldLocal;

        private void OnEnable()
        {
            playerRoot.animator.applyRootMotion = false;
            playerRoot.characterInput.OnUpdateInput += InputUpdated;
        }

        private void OnDisable()
        {
            playerRoot.characterInput.OnUpdateInput -= InputUpdated;
        }

        private void Update()
        {
            if (!IsServer)
            {
                return;
            }
            if (playerRoot.IsDead.Value)
            {
                return;
            }
            if (playerRoot.characterInit.PlayerType.Value == Gameplay.PlayerType.Bot)
            {
                float agentSpeed = playerRoot.bot.navMeshAgent.speed;
                float vel = playerRoot.bot.navMeshAgent.velocity.magnitude;
                float denom = Mathf.Max(0.0001f, agentSpeed);
                float value01 = Mathf.Clamp01(vel / denom);
                MaybeSendLocomotionServer(value01);
            }
        }

        private void InputUpdated()
        {
            if (!IsOwner)
            {
                return;
            }
            if (playerRoot.IsDead.Value)
            {
                return;
            }
            bool shield = playerRoot.characterInput.shieldHeld;
            if (shield != _shieldLocal)
            {
                _shieldLocal = shield;
                SetShieldLocal(_shieldLocal);
                ShieldServerRpc(_shieldLocal);
            }
            if (playerRoot.characterInput.attackPressed)
            {
                AttackServerRpc();
            }
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
            if (IsOwner)
            {
                MaybeSendLocomotionOwner(value);
            }
        }

        private void MaybeSendLocomotionOwner(float value)
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

        private void MaybeSendLocomotionServer(float value)
        {
            if (!IsServer)
            {
                return;
            }
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
            LocomotionObserversRpc(value);
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
        private void ShieldServerRpc(bool state)
        {
            ShieldObserversRpc(state);
        }

        [ObserversRpc]
        private void ShieldObserversRpc(bool state)
        {
            if (IsOwner)
            {
                return;
            }
            SetShieldLocal(state);
        }

        private void SetShieldLocal(bool state)
        {
            playerRoot.animator.SetBool("Shield", state);
        }

        [ServerRpc(RequireOwnership = true)]
        private void AttackServerRpc()
        {
            ServerAttack();
        }

        public void ServerAttack()
        {
            if (playerRoot.IsDead.Value)
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
