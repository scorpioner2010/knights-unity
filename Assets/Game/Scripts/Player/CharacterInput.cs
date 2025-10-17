using System;
using FishNet.Object;
using UnityEngine;

namespace Game.Scripts.Player
{
    [DefaultExecutionOrder(-50)]
    public class CharacterInput : NetworkBehaviour
    {
        // Плавний рух: напрям (x,z) * амплітуда (0..1)
        public Vector3 inputDirection;
        public bool jumpPressed;
        public bool attackPressed;

        public bool slot1;
        public bool slot2;
        public bool slot3;

        public event Action OnUpdateInput;

        // Кеш для економії апдейтів
        private Vector3 _lastRawDirection = Vector3.zero;
        private float _lastMoveAmount = 0f;

        private bool
            _lastJump,
            _lastAttack,
            _lastSlot1,
            _lastSlot2,
            _lastSlot3;

        public Transform skeleton;
        public float accelerationTime = 0.5f;
        public float decelerationMultiplier = 3.0f;
        
        public float maxReverseSpeedCap = 0.2f;
        public float directionChangeDecelMultiplier = 2.0f;
        public float turnSpeedAtZero = 720f;
        public float turnSpeedAtMax = 240f;

        public PlayerRoot playerRoot;

        public static float GetAxisX => Input.GetAxis("Mouse X");
        public static float GetAxisY => Input.GetAxis("Mouse Y");
        public static bool Escape => Input.GetKeyDown(KeyCode.Escape);
        public static bool Tab => Input.GetKeyDown(KeyCode.Tab);

        // Внутрішній стан
        private float _moveAmount; // 0..1
        private Vector3 _currentDir = Vector3.forward; // поточний згладжений напрям (нормований)
        public float MoveAmount => _moveAmount;

        private void Update()
        {
            if (!IsOwner)
                return;

            if (playerRoot.IsDead.Value)
            {
                return;
            }
            
            // 1) Сирий таргет-напрямок з WASD
            Vector3 rawTarget = Vector3.zero;
            if (Input.GetKey("w")) rawTarget += playerRoot.transform.forward;
            if (Input.GetKey("s")) rawTarget -= playerRoot.transform.forward;
            if (Input.GetKey("a")) rawTarget -= playerRoot.transform.right;
            if (Input.GetKey("d")) rawTarget += playerRoot.transform.right;
            if (rawTarget.sqrMagnitude > 1f) rawTarget.Normalize();

            bool jump = Input.GetKeyDown(KeyCode.Space);
            bool attack = Input.GetMouseButtonDown(0);
            bool slotNumber1 = Input.GetKey("1");
            bool slotNumber2 = Input.GetKey("2");
            bool slotNumber3 = Input.GetKey("3");

            bool hasInput = rawTarget != Vector3.zero;

            // 2) Плавний поворот напрямку (інерція повороту)
            float turnSpeed = Mathf.Lerp(turnSpeedAtZero, turnSpeedAtMax, Mathf.Clamp01(_moveAmount));
            if (hasInput)
            {
                Vector3 targetDir = rawTarget.normalized;
                float maxStep = turnSpeed * Time.deltaTime;
                _currentDir = Vector3.RotateTowards(_currentDir, targetDir, Mathf.Deg2Rad * maxStep, float.MaxValue);
                _currentDir.Normalize();
            }

            // 3) Кутовий штраф швидкості
            float alignment = hasInput ? Mathf.Clamp(Vector3.Dot(_currentDir, rawTarget.normalized), -1f, 1f) : 1f;
            float dirFactor = Mathf.InverseLerp(-1f, 1f, alignment); // 0..1
            float directionSpeedCap = Mathf.Lerp(maxReverseSpeedCap, 1f, dirFactor);

            // 4) Розгін/гальмування з урахуванням капа
            float targetAmount = hasInput ? directionSpeedCap : 0f;
            float accel = Mathf.Max(0.0001f, accelerationTime);
            float baseDecel = Mathf.Max(0.0001f, accelerationTime / Mathf.Max(0.0001f, decelerationMultiplier));
            bool decelPhase = targetAmount < _moveAmount;
            float decel = decelPhase ? baseDecel / Mathf.Max(0.0001f, directionChangeDecelMultiplier) : baseDecel;
            float step = decelPhase ? Time.deltaTime / decel : Time.deltaTime / accel;
            _moveAmount = Mathf.MoveTowards(_moveAmount, targetAmount, step);

            // 5) Остаточний вектор руху
            Vector3 smoothedDirection = (_moveAmount > 0.0001f) ? _currentDir * _moveAmount : Vector3.zero;

            // 6) Нотифікація слухачів
            bool changed =
                hasInput ||
                rawTarget != _lastRawDirection ||
                Mathf.Abs(_moveAmount - _lastMoveAmount) > 0.0001f ||
                jump != _lastJump ||
                attack != _lastAttack ||
                slotNumber1 != _lastSlot1 ||
                slotNumber2 != _lastSlot2 ||
                slotNumber3 != _lastSlot3;

            if (changed)
            {
                _lastRawDirection = rawTarget;
                _lastMoveAmount = _moveAmount;

                _lastJump = jump;
                _lastAttack = attack;
                _lastSlot1 = slotNumber1;
                _lastSlot2 = slotNumber2;
                _lastSlot3 = slotNumber3;

                inputDirection = smoothedDirection;
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
