using System;
using FishNet.Object;
using Game.Scripts.Core.Utils;
using UnityEngine;

namespace Game.Scripts.Player
{
    [DefaultExecutionOrder(-50)]
    public class CharacterInput : NetworkBehaviour
    {
        public Vector3 inputDirection;
        public bool shieldHeld;
        public bool attackPressed;
        public event Action OnUpdateInput;

        private Vector3 _lastRawDirection = Vector3.zero;
        private float _lastMoveAmount;
        private bool _lastShield, _lastAttack;

        public float accelerationTime = 0.5f;
        public float decelerationMultiplier = 3.0f;
        public float maxReverseSpeedCap = 0.2f;
        public float directionChangeDecelMultiplier = 2.0f;
        public float turnSpeedAtZero = 720f;
        public float turnSpeedAtMax = 240f;

        public PlayerRoot playerRoot;

        public static float GetAxisX;
        public static float GetAxisY;
        public static bool Escape => Input.GetKeyDown(KeyCode.Escape);
        public static bool Tab => Input.GetKeyDown(KeyCode.Tab);

        private float _moveAmount;
        private Vector3 _currentDir = Vector3.forward;
        public float MoveAmount => _moveAmount;

        private TimerBlocker _attackDelay = new(0.6f);

        private void Update()
        {
            if (!IsOwner)
                return;

            if (playerRoot.IsDead.Value)
                return;

            bool isMobile = MobileManager.IsNativeMobile();

            if (!isMobile)
            {
                GetAxisX = Input.GetAxis("Mouse X");
                GetAxisY = Input.GetAxis("Mouse Y");
            }
            else
            {
                if (MobileManager.In != null)
                {
                    Vector2 r = MobileManager.In.GetRotateInput();
                    GetAxisX = r.x;
                    GetAxisY = r.y;
                }
                else
                {
                    GetAxisX = 0f;
                    GetAxisY = 0f;
                }
            }

            bool shield = !isMobile ? Input.GetMouseButton(1) : MobileManager.In.IsShieldHeld();

            bool isUnblock = _attackDelay.IsBlock() == false;
            bool attack = !isMobile ? Input.GetMouseButtonDown(0) && isUnblock : MobileManager.In.ConsumeAttackDown() && isUnblock;

            if (attack)
            {
                _attackDelay.Block();
            }

            Vector3 rawTarget = Vector3.zero;

            if (isMobile && MobileManager.In != null)
            {
                Vector2 mv = MobileManager.In.GetMoveInput();
                if (mv.sqrMagnitude > 0f && playerRoot != null)
                {
                    rawTarget += playerRoot.transform.forward * mv.y;
                    rawTarget += playerRoot.transform.right * mv.x;
                }
            }
            else if (attack == false && shield == false)
            {
                if (Input.GetKey("w")) rawTarget += playerRoot.transform.forward;
                if (Input.GetKey("s")) rawTarget -= playerRoot.transform.forward;
                if (Input.GetKey("a")) rawTarget -= playerRoot.transform.right;
                if (Input.GetKey("d")) rawTarget += playerRoot.transform.right;
            }

            if (rawTarget.sqrMagnitude > 1f) rawTarget.Normalize();

            bool hasInput = rawTarget != Vector3.zero;

            float turnSpeed = Mathf.Lerp(turnSpeedAtZero, turnSpeedAtMax, Mathf.Clamp01(_moveAmount));
            if (hasInput)
            {
                Vector3 targetDir = rawTarget.normalized;
                float maxStep = turnSpeed * Time.deltaTime;
                _currentDir = Vector3.RotateTowards(_currentDir, targetDir, Mathf.Deg2Rad * maxStep, float.MaxValue);
                _currentDir.Normalize();
            }

            float alignment = hasInput ? Mathf.Clamp(Vector3.Dot(_currentDir, rawTarget.normalized), -1f, 1f) : 1f;
            float dirFactor = Mathf.InverseLerp(-1f, 1f, alignment);
            float directionSpeedCap = Mathf.Lerp(maxReverseSpeedCap, 1f, dirFactor);

            float targetAmount = hasInput ? directionSpeedCap : 0f;
            float accel = Mathf.Max(0.0001f, accelerationTime);
            float baseDecel = Mathf.Max(0.0001f, accelerationTime / Mathf.Max(0.0001f, decelerationMultiplier));
            bool decelPhase = targetAmount < _moveAmount;
            float decel = decelPhase ? baseDecel / Mathf.Max(0.0001f, directionChangeDecelMultiplier) : baseDecel;
            float step = decelPhase ? Time.deltaTime / decel : Time.deltaTime / accel;
            _moveAmount = Mathf.MoveTowards(_moveAmount, targetAmount, step);

            Vector3 smoothedDirection = (_moveAmount > 0.0001f) ? _currentDir * _moveAmount : Vector3.zero;

            bool changed =
                hasInput ||
                rawTarget != _lastRawDirection ||
                Mathf.Abs(_moveAmount - _lastMoveAmount) > 0.0001f ||
                shield != _lastShield ||
                attack != _lastAttack;

            if (changed)
            {
                _lastRawDirection = rawTarget;
                _lastMoveAmount = _moveAmount;
                _lastShield = shield;
                _lastAttack = attack;

                inputDirection = smoothedDirection;
                shieldHeld = shield;
                attackPressed = attack;

                OnUpdateInput?.Invoke();
            }
        }
    }
}
