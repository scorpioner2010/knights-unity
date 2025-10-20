using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Game.Scripts
{
    public class MobileManager : MonoBehaviour
    {
        public static MobileManager In;
        public GameObject panel;

        public FixedJoystick movingJoystick;
        public FixedJoystick rotatingJoystick;

        public Button attack;

        public float moveSensitivity = 1.0f;
        public float rotateSensitivity = 1.0f;

        private bool _attackDownBuffered;
        private bool _shieldHeld;

        public float yCameraAngle;

        private void Awake()
        {
            In = this;
            if (attack != null) attack.onClick.AddListener(TriggerAttack);
        }

        private void Start()
        {
            if (IsNativeMobile() == false && panel != null)
                panel.gameObject.SetActive(false);
        }

        public void StartShield()
        {
            _shieldHeld = true;
        }

        public void StopShield()
        {
            _shieldHeld = false;
        }

        public bool IsShieldHeld()
        {
            return _shieldHeld;
        }

        public void TriggerAttack()
        {
            _attackDownBuffered = true;
        }

        public bool ConsumeAttackDown()
        {
            if (_attackDownBuffered)
            {
                _attackDownBuffered = false;
                return true;
            }
            return false;
        }

        public Vector2 GetMoveInput()
        {
            if (movingJoystick == null) return Vector2.zero;
            Vector2 v = movingJoystick.Direction * moveSensitivity;
            v = Vector2.ClampMagnitude(v, 1f);
            return v;
        }

        public Vector2 GetRotateInput()
        {
            if (rotatingJoystick == null) return Vector2.zero;
            Vector2 v = rotatingJoystick.Direction * rotateSensitivity;
            v = Vector2.ClampMagnitude(v, 1f);
            return v;
        }

        public static bool IsNativeMobile()
        {
#if UNITY_EDITOR
            return EditorUserBuildSettings.activeBuildTarget == BuildTarget.Android ||
                   EditorUserBuildSettings.activeBuildTarget == BuildTarget.iOS;
#else
            return Application.platform == RuntimePlatform.Android ||
                   Application.platform == RuntimePlatform.IPhonePlayer;
#endif
        }
    }
}
