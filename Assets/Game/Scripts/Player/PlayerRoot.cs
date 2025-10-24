using System;
using Cysharp.Threading.Tasks;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using Game.Script.Player.UI;
using Game.Scripts.Gameplay;
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
        public CharacterInput characterInput;
        public CharacterController characterController;
        public CharacterCameraController characterCameraController;
        public CharacterInit characterInit;
        public Health health;
        public MeleeWeapon meleeWeapon;
        public CharacterParticles characterParticles;
        public PlayerHUD playerHUD;
        public Collider playerCollider;
        public FaceCenterFromGround  faceCenterFromGround;
        public StatisticCounter  statisticCounter;
        public Bot bot;
        
        public readonly SyncVar<bool> IsDead = new();
        public readonly SyncVar<Team> Team = new();
        
        [HideInInspector] public UnityEngine.Camera playerCamera;
        [HideInInspector] public string warriorCode;
        [HideInInspector] public MeshPack mesh;
        
        public ParticleSystem teamView;

        public void InitMesh(MeshPack pack)
        {
            mesh = Instantiate(pack, characterMovement.skeleton);
            mesh.Init(animator, this);
        }

        [Obsolete("Obsolete")]
        public void InitTeamView()
        {
            teamView.gameObject.SetActive(true);
            teamView.startColor = Team.Value switch
            {
                World.Spawns.Team.Blue => Color.blue,
                World.Spawns.Team.Red => Color.red,
                _ => default
            };
        }
        
        public override void OnStartClient()
        {
            IsDead.OnChange += OnIsDeadChanged;
        }

        public override void OnStopClient()
        {
            IsDead.OnChange -= OnIsDeadChanged;
        }

        public void InitOwner(UnityEngine.Camera playerCam)
        {
            playerCamera = playerCam;
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

        private async void ApplyDeadState(bool isDead)
        {
            if (isDead)
            {
                animator.ResetTrigger("Attack");
                animator.SetBool("Shield", false);
                animator.SetFloat("Locomotion", 0f);
                animator.SetTrigger("Die");

                await UniTask.Delay(2000);
                
                playerHUD.Deactivate();
            }
        }
    }
}
