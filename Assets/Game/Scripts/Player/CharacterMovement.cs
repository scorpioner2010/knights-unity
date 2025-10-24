using FishNet.Object;
using UnityEngine;

namespace Game.Scripts.Player
{
    [DefaultExecutionOrder(0)]
    public class CharacterMovement : NetworkBehaviour
    {
        public bool useRootMotion = true;
        public float speed = 7.0f;

        public float gravity = 10.0f;
        public float groundedStick = 2.0f;
        public float maxFallSpeed = 50f;

        public PlayerRoot playerRoot;

        public float moveLerpSpeed = 20;

        public Transform skeleton;
        public float skeletonTurnSpeed = 500f;

        private Vector3 _verticalVelocity;
        private Vector3 _lastPosition;
        private float _movementThreshold = 0.01f;

        public bool IsMoving { get; private set; }
        public float CurrentSpeed { get; private set; }

        private CharacterController _cc;
        private Animator _anim;

        public void SetUseRootMotion(bool v)
        {
            useRootMotion = v;
        }

        private void Awake()
        {
            _cc = playerRoot.characterController;
            _anim = playerRoot.animator;
            _anim.applyRootMotion = false;
        }

        private void FixedUpdate()
        {
            Vector3 delta = transform.position - _lastPosition;
            float distance = delta.magnitude;
            IsMoving = distance > _movementThreshold;
            CurrentSpeed = distance / Time.fixedDeltaTime;
            _lastPosition = transform.position;
            if (IsOwner)
            {
                float animLocomotion01 = playerRoot.characterInput != null ? playerRoot.characterInput.MoveAmount : 0f;
                playerRoot.animationController.SetLocomotion(animLocomotion01, moveLerpSpeed);
            }
        }

        private void OnEnable()
        {
            playerRoot.characterInput.OnUpdateInput += InputUpdated;
        }

        private void OnDisable()
        {
            playerRoot.characterInput.OnUpdateInput -= InputUpdated;
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
        }

        private void Update()
        {
            if (!IsOwner || _cc == null || !_cc.enabled)
            {
                return;
            }
            bool grounded = _cc.isGrounded;
            if (grounded && _verticalVelocity.y < 0f)
            {
                _verticalVelocity.y = -groundedStick;
            }
            else
            {
                _verticalVelocity.y = Mathf.Max(_verticalVelocity.y - gravity * Time.deltaTime, -maxFallSpeed);
            }
            if (!useRootMotion)
            {
                Vector3 direction = (playerRoot.characterInput != null ? playerRoot.characterInput.inputDirection : Vector3.zero) * speed;
                direction.y = _verticalVelocity.y;
                _cc.Move(direction * Time.deltaTime);
            }
            UpdateSkeletonRotation();
        }

        private void OnAnimatorMove()
        {
            if (!useRootMotion)
            {
                return;
            }
            if (!IsOwner || _cc == null || !_cc.enabled)
            {
                return;
            }
            if (_anim == null)
            {
                return;
            }
            if (playerRoot.IsDead.Value)
            {
                return;
            }
            Vector3 dp = _anim.deltaPosition;
            Vector2 planar = new Vector2(dp.x, dp.z);
            float stepLength = planar.magnitude;
            Vector3 desiredDir = playerRoot.characterInput != null ? playerRoot.characterInput.inputDirection : Vector3.zero;
            if (desiredDir.sqrMagnitude < 0.0001f || stepLength < 0.000001f)
            {
                Vector3 onlyY = Vector3.up * (_verticalVelocity.y * Time.deltaTime);
                _cc.Move(onlyY);
                return;
            }
            desiredDir.Normalize();
            Vector3 worldDelta = desiredDir * stepLength;
            worldDelta.y += _verticalVelocity.y * Time.deltaTime;
            _cc.Move(worldDelta);
        }

        private void UpdateSkeletonRotation()
        {
            if (skeleton == null || playerRoot.characterInput == null)
            {
                return;
            }
            Vector3 moveInput = new Vector3(playerRoot.characterInput.inputDirection.x, 0f, playerRoot.characterInput.inputDirection.z);
            if (moveInput.sqrMagnitude < 0.01f)
            {
                skeleton.localRotation = Quaternion.RotateTowards(skeleton.localRotation, Quaternion.Euler(0f, 0f, 0f), skeletonTurnSpeed * Time.deltaTime);
                return;
            }
            Vector3 localMove = transform.InverseTransformDirection(moveInput.normalized);
            float targetLocalAngle = Mathf.Atan2(localMove.x, localMove.z) * Mathf.Rad2Deg;
            Quaternion targetRotation = Quaternion.Euler(0f, targetLocalAngle, 0f);
            skeleton.localRotation = Quaternion.RotateTowards(skeleton.localRotation, targetRotation, skeletonTurnSpeed * Time.deltaTime);
        }
    }
}
