using FishNet.Object;
using UnityEngine;

namespace Game.Scripts.Player
{
    [DefaultExecutionOrder(0)]
    public class CharacterMovement : NetworkBehaviour
    {
        [Header("Рух")]
        [SerializeField] private bool useRootMotion = true; // Ввімкнено: RM з редиректом; вимкнеш — стара схема speed*dir
        [SerializeField] private float speed = 7.0f;        // Використовується лише коли useRootMotion = false

        [Header("Гравітація")]
        [SerializeField] private float gravity = 10.0f;
        [SerializeField] private float groundedStick = 2.0f; // невеликий притиск до землі
        [SerializeField] private float maxFallSpeed = 50f;

        public PlayerRoot playerRoot;

        [Header("Анімація")]
        public float moveLerpSpeed = 20;

        [Header("Скелет/локальний поворот")]
        public Transform skeleton;
        public float skeletonTurnSpeed = 500f;

        private Vector3 _verticalVelocity;

        private Vector3 _lastPosition;
        private float _movementThreshold = 0.01f;

        public bool IsMoving { get; private set; }
        public float CurrentSpeed { get; private set; }

        private CharacterController _cc;
        private Animator _anim;

        private void Awake()
        {
            _cc = playerRoot != null ? playerRoot.characterController : GetComponent<CharacterController>();
            _anim = playerRoot != null ? playerRoot.animator : GetComponentInChildren<Animator>();

            // Ми самі застосовуємо root motion, тому відключаємо авто-аплай на аніматорі.
            if (_anim != null) _anim.applyRootMotion = false;
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
                float animLocomotion01 = (playerRoot.characterInput != null) ? playerRoot.characterInput.MoveAmount : 0f;
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
            if (!IsOwner) return;
            if (playerRoot.IsDead.Value) return;
        }

        private void Update()
        {
            if (!IsOwner || _cc == null || !_cc.enabled) return;

            bool grounded = _cc.isGrounded;
            if (grounded && _verticalVelocity.y < 0f)
                _verticalVelocity.y = -groundedStick;
            else
                _verticalVelocity.y = Mathf.Max(_verticalVelocity.y - gravity * Time.deltaTime, -maxFallSpeed);

            if (!useRootMotion)
            {
                // Старий режим: рух через speed * inputDirection
                Vector3 direction = (playerRoot.characterInput != null ? playerRoot.characterInput.inputDirection : Vector3.zero) * speed;
                direction.y = _verticalVelocity.y;
                _cc.Move(direction * Time.deltaTime);
            }

            UpdateSkeletonRotation();
        }

        private void OnAnimatorMove()
        {
            if (!useRootMotion) return;
            if (!IsOwner || _cc == null || !_cc.enabled) return;
            if (_anim == null) return;
            if (playerRoot.IsDead.Value) return;

            // 1) Довжина кроку з кліпу (у площині XZ)
            Vector3 dp = _anim.deltaPosition;
            Vector2 planar = new Vector2(dp.x, dp.z);
            float stepLength = planar.magnitude;

            // 2) Бажаний напрям у світі з CharacterInput (вже відносно тіла/камери як ти задумав)
            Vector3 desiredDir = (playerRoot.characterInput != null) ? playerRoot.characterInput.inputDirection : Vector3.zero;

            // Якщо нема інпуту — не рухаємо (щоб RM не тягнув вперед)
            if (desiredDir.sqrMagnitude < 0.0001f || stepLength < 0.000001f)
            {
                // все одно додаємо лише вертикаль (фол)
                Vector3 onlyY = Vector3.up * (_verticalVelocity.y * Time.deltaTime);
                _cc.Move(onlyY);
                return;
            }

            desiredDir.Normalize();

            // 3) Редирект: світова дельта у потрібному напрямку з тією ж довжиною кроку
            Vector3 worldDelta = desiredDir * stepLength;
            worldDelta.y += _verticalVelocity.y * Time.deltaTime;

            _cc.Move(worldDelta);

            // 4) Ігноруємо animator.deltaRotation — корінь обертає твоя камера/логіка
            // Якщо треба — можна додати легкий доворот тіла під desiredDir:
            // transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(desiredDir, Vector3.up), 720f * Time.deltaTime);
        }

        private void UpdateSkeletonRotation()
        {
            if (skeleton == null || playerRoot.characterInput == null) return;

            Vector3 moveInput = new Vector3(playerRoot.characterInput.inputDirection.x, 0f, playerRoot.characterInput.inputDirection.z);

            if (moveInput.sqrMagnitude < 0.01f)
            {
                skeleton.localRotation = Quaternion.RotateTowards(
                    skeleton.localRotation,
                    Quaternion.Euler(0f, 0f, 0f),
                    skeletonTurnSpeed * Time.deltaTime
                );
                return;
            }

            Vector3 localMove = transform.InverseTransformDirection(moveInput.normalized);
            float targetLocalAngle = Mathf.Atan2(localMove.x, localMove.z) * Mathf.Rad2Deg;
            Quaternion targetRotation = Quaternion.Euler(0f, targetLocalAngle, 0f);
            skeleton.localRotation = Quaternion.RotateTowards(skeleton.localRotation, targetRotation, skeletonTurnSpeed * Time.deltaTime);
        }
    }
}
