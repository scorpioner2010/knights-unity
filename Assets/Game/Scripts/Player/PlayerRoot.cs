using FishNet.Object;
using Game.Combat;
using Game.Script.Player;
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
        public CharacterParticles  characterParticles;
        public NickNameView nickNameView;
        
        private void Update()
        {
        }

        public void Init()
        {
            playerCamera = CameraSync.In.gameplayCamera;
        }
    }
}