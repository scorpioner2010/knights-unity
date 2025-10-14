using System;
using FishNet.Object;
using UnityEngine;

namespace Game.Scripts.Player
{
    [DefaultExecutionOrder(-50)]
    public class CharacterInput : NetworkBehaviour
    {
        // — SyncVar з правом запису лише на клієнті-власнику
        public Vector3 inputDirection;
        public bool jumpPressed;
        public bool attackPressed;
        
        public bool slot1;
        public bool slot2;
        public bool slot3;

        public event Action OnUpdateInput;

        // — попередній стан, щоб не спамити RPC
        private Vector3 _lastDirection = Vector3.zero;
        
        private bool 
            _lastJump, 
            _lastAttack, 
            _lastSlot1, 
            _lastSlot2, 
            _lastSlot3;
        
        [SerializeField] private Transform skeleton;
        [SerializeField] private float accelerationTime = 0.02f;
        [SerializeField] private float decelerationMultiplier = 3.0f;
        
        public PlayerRoot playerRoot;

        public static float GetAxisVertical => Input.GetAxis("Vertical");
        public static float GetAxisHorizontal => Input.GetAxis("Horizontal");
        public static float GetAxisX => Input.GetAxis("Mouse X");
        public static float GetAxisY => Input.GetAxis("Mouse Y");
        public static bool Escape => Input.GetKeyDown(KeyCode.Escape);
        public static bool Tab => Input.GetKeyDown(KeyCode.Tab);
        public static bool E => Input.GetKey(KeyCode.E);
        public static bool Q => Input.GetKey(KeyCode.Q);
        
        private float GetAlignmentScore()
        {
            Vector3 characterForward = new Vector3(skeleton.forward.x, 0, skeleton.forward.z).normalized;
            Vector3 cameraForward = new Vector3(playerRoot.playerCamera.transform.forward.x, 0, playerRoot.playerCamera.transform.forward.z).normalized;
            float dot = Vector3.Dot(characterForward, cameraForward);
            return (dot + 1) / 2;
        }

        private void Update()
        {
            if (IsOwner == false)
            {
                return;
            }
            
            bool canShoot = false;

            /*if (playerRoot.weaponController.currentWeapon != WeaponType.Fist && playerRoot.weaponController.currentWeapon != WeaponType.Sword)
            {
                if (playerRoot.playerCamera != null)
                {
                    float angle = GetAlignmentScore();
                    canShoot = angle > 0.95f;
                    CrosshairScreen.SetActiveScreen(canShoot);
                }
            }
            else
            {
                CrosshairScreen.SetActiveScreen(false);
            }*/

            // 1) Зчитуємо ввід
            Vector3 targetDirection = Vector3.zero;
            if (Input.GetKey("w")) targetDirection += playerRoot.transform.forward;
            if (Input.GetKey("s")) targetDirection -= playerRoot.transform.forward;
            if (Input.GetKey("a")) targetDirection -= playerRoot.transform.right;
            if (Input.GetKey("d")) targetDirection += playerRoot.transform.right;
            if (targetDirection.magnitude > 1) targetDirection.Normalize();

            bool jump = Input.GetKeyDown(KeyCode.Space);
            bool attack = false;
            bool slotNumber1 = Input.GetKey("1");
            bool slotNumber2 = Input.GetKey("2");
            bool slotNumber3 = Input.GetKey("3");
            
            // 2) Перевіряємо, чи треба відправити RPC
            bool movementHeld = targetDirection != Vector3.zero;

            /*if (playerRoot.weaponController.currentWeapon == WeaponType.Fist || playerRoot.weaponController.currentWeapon == WeaponType.Sword)
            {
                attack = MobileHUD.IsMobile() ? MobileHUD.AttackPressed : Input.GetMouseButtonDown(0);
            }
            else
            {
                if (canShoot)
                {
                    attack = MobileHUD.IsMobile() ? MobileHUD.AttackPressed : Input.GetMouseButtonDown(0);
                    playerRoot.sightLookController.SetSightPosition();
                }
            }*/
            
            bool changed =
                movementHeld ||
                targetDirection != _lastDirection ||
                jump != _lastJump ||
                attack != _lastAttack ||
                slotNumber1 != _lastSlot1 ||
                slotNumber2 != _lastSlot2 ||
                slotNumber3 != _lastSlot3;

            if (changed)
            {
                _lastDirection = targetDirection;
                _lastJump = jump;
                _lastAttack = attack;
                _lastSlot1 = slotNumber1;
                _lastSlot2 = slotNumber2;
                _lastSlot3 = slotNumber3;

                inputDirection = targetDirection;
                jumpPressed = jump;
                attackPressed = attack;
                
                slot1 = slotNumber1;
                slot2 = slotNumber2;
                slot3 = slotNumber3;
                
                OnUpdateInput?.Invoke();
            }
        }
    }
}
