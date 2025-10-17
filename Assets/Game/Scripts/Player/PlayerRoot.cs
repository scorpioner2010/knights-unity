using FishNet.Object;
using FishNet.Object.Synchronizing;
using Game.Script.Player.UI;
using Game.Scripts.Gameplay.Robots;
using Game.Scripts.Networking.Lobby;
using Game.Scripts.World.Spawns;
using UnityEngine;

namespace Game.Scripts.Player
{
    public class PlayerRoot : NetworkBehaviour
    {
        public NetworkObject networkObject;
        public CharacterMovement characterMovement;
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
        public Collider playerCollider;
        public FaceCenterFromGround  faceCenterFromGround;
        public StatisticCounter  statisticCounter;
        
        public readonly SyncVar<bool> IsDead = new();
        public readonly SyncVar<Team> Team = new();
        public ServerRoom serverRoom; //only server
        public string warriorCode;
        
        public override void OnStartClient()
        {
            IsDead.OnChange += OnIsDeadChanged;
        }

        public override void OnStopClient()
        {
            IsDead.OnChange -= OnIsDeadChanged;
        }

        public void Init()
        {
            playerCamera = CameraSync.In.gameplayCamera;
            faceCenterFromGround.FaceCenterFromGroundLayer(this);
        }

        [Server]
        public void SetDeadServer()
        {
            IsDead.Value = true;
        }

        private void OnIsDeadChanged(bool prev, bool next, bool asServer)
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
            }
        }
    }
}
