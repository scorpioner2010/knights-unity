using FishNet.Object;
using UnityEngine;

namespace Game.Scripts.Player
{
    [DefaultExecutionOrder(0)]
    public class CharacterMovement : NetworkBehaviour
    {
        [SerializeField] private float speed = 7.0f;
        [SerializeField] private float jumpPower = 7.0f;
        [SerializeField] private float gravity = 10.0f;

        public PlayerRoot playerRoot;
        public float moveLerpSpeed = 20;

        public Transform skeleton;
        public float skeletonTurnSpeed = 500f;

        private Vector3 _verticalVelocity;

        private Vector3 _lastPosition;
        private float _movementThreshold = 0.01f;

        public bool IsMoving { get; private set; }
        public float CurrentSpeed { get; private set; }

        private void FixedUpdate()
        {
            // Фізична швидкість (як була)
            Vector3 delta = transform.position - _lastPosition;
            float distance = delta.magnitude;
            IsMoving = distance > _movementThreshold;
            CurrentSpeed = distance / Time.fixedDeltaTime;
            _lastPosition = transform.position;

            // ✅ Анімацію бігу підживлює ТІЛЬКИ власник; інші отримають RPC
            if (IsOwner)
            {
                float animLocomotion01 = playerRoot.characterInput != null ? playerRoot.characterInput.MoveAmount : 0f;
                playerRoot.animationController.SetLocomotion(animLocomotion01, moveLerpSpeed);
            }
        }

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

            if (playerRoot.characterController.isGrounded && playerRoot.characterInput.jumpPressed)
                _verticalVelocity.y = jumpPower;
        }

        private void Update()
        {
            if (!IsOwner || !playerRoot.characterController.enabled)
                return;

            if (_verticalVelocity.y > -gravity)
                _verticalVelocity.y -= gravity * Time.deltaTime;

            Vector3 direction = playerRoot.characterInput.inputDirection * speed;
            direction.y = _verticalVelocity.y;
            playerRoot.characterController.Move(direction * Time.deltaTime);

            UpdateSkeletonRotation();
        }

        private void UpdateSkeletonRotation()
        {
            if (skeleton == null)
                return;

            Vector3 moveInput = new Vector3(
                playerRoot.characterInput.inputDirection.x, 0f, playerRoot.characterInput.inputDirection.z);

            if (moveInput.sqrMagnitude < 0.01f)
            {
                skeleton.localRotation = Quaternion.RotateTowards(
                    skeleton.localRotation, Quaternion.Euler(0f, 0, 0f), skeletonTurnSpeed * Time.deltaTime);
                return;
            }

            Vector3 localMove = transform.InverseTransformDirection(moveInput.normalized);
            float targetLocalAngle = Mathf.Atan2(localMove.x, localMove.z) * Mathf.Rad2Deg;
            Quaternion targetRotation = Quaternion.Euler(0f, targetLocalAngle, 0f);
            skeleton.localRotation = Quaternion.RotateTowards(
                skeleton.localRotation, targetRotation, skeletonTurnSpeed * Time.deltaTime);
        }
    }
}
