using System;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using Game.Combat;
using Game.Script.Player.UI;
using Game.Scripts.Gameplay.Robots;
using UnityEngine;

namespace Game.Scripts.Player
{
    public class PlayerRoot : NetworkBehaviour
    {
        public NetworkObject networkObject;
        public CharacterMovement characterMovement;
        public NickDisplay nickDisplay;
        public CharacterAnimationController animationController;
        public Animator animator;
        public UnityEngine.Camera playerCamera;
        public CharacterInput characterInput;
        public CharacterController characterController;
        public SmoothLookAtCamera smoothLookAtCamera;
        public CharacterCameraController characterCameraController;
        public GroundChecker groundChecker;
        public CharacterInit characterInit;
        public Health health;
        public MeleeWeapon meleeWeapon;
        public CharacterParticles characterParticles;
        public PlayerHUD playerHUD;
        public readonly SyncVar<bool> Dead = new(false);
        public Collider playerCollider;

        public override void OnStartClient()
        {
            Dead.OnChange += OnDeadChanged;
        }

        public override void OnStopClient()
        {
            Dead.OnChange -= OnDeadChanged;
        }

        private void Update()
        {
        }

        public void Init()
        {
            playerCamera = CameraSync.In.gameplayCamera;
        }

        [Server]
        public void SetDeadServer(bool value)
        {
            if (Dead.Value == value)
            {
                return;
            }
            Dead.Value = value;
        }

        private void OnDeadChanged(bool prev, bool next, bool asServer)
        {
            ApplyDeadState(next);
        }

        private void ApplyDeadState(bool isDead)
        {
            if (isDead)
            {
                animator.ResetTrigger("Attack");
                animator.ResetTrigger("Jump");
                animator.SetFloat("Locomotion", 0f);
                animator.SetTrigger("Die");
                
                playerCollider.enabled = false;
                characterController.enabled = false;
            }
        }
    }
}
