using FishNet.Object;
using UnityEngine;

namespace Game.Scripts.Player
{
    public class CharacterCameraController : NetworkBehaviour
    {
        public Transform cameraFocusPoint;
        public float focusSmoothSpeed = 5f;
        public PlayerRoot playerRoot;

        public bool blockCameraRotation;

        public float cameraDistance = 5.0f;
        public float xSpeed = 120.0f;
        public float ySpeed = 120.0f;

        public float mouseSpeed = 0.02f;
        public float mouseSpeedMobile = 0.01f;

        public bool isActiveLerp = true;

        private float _x;
        private float _y;
        private float _lastSentX;
        private Vector3 _smoothedFocusPosition;

        private void Start()
        {
            if (playerRoot != null && playerRoot.playerCamera != null)
            {
                Vector3 angles = playerRoot.playerCamera.transform.eulerAngles;
                _x = angles.y;
                _y = angles.x;
            }

            _lastSentX = _x;

            if (cameraFocusPoint != null)
                _smoothedFocusPosition = cameraFocusPoint.position;
        }

        private void LateUpdate()
        {
            if (!IsOwner)
                return;

            if (playerRoot == null || playerRoot.playerCamera == null)
                return;

            if (playerRoot.IsDead.Value)
                return;

            if (blockCameraRotation)
                return;

            if (cameraFocusPoint == null)
                return;

            if (isActiveLerp)
                _smoothedFocusPosition = Vector3.Lerp(_smoothedFocusPosition, cameraFocusPoint.position, focusSmoothSpeed * Time.deltaTime);

            bool isMobile = MobileManager.IsNativeMobile();

            if (!isMobile)
            {
                // ПК: як було
                _y -= CharacterInput.GetAxisY * ySpeed * mouseSpeed;
                _x += CharacterInput.GetAxisX * xSpeed * mouseSpeed;
            }
            else
            {
                // Мобільні: Y задається твоїм MobileManager (як було),
                // а X нормалізуємо по часу, щоб не залежав від FPS.
                _y = MobileManager.In.yCameraAngle;
                _x += CharacterInput.GetAxisX * xSpeed * mouseSpeedMobile * Time.deltaTime;
            }

            _y = Mathf.Clamp(_y, -10f, 60f);

            Quaternion camRotation = Quaternion.Euler(_y, _x, 0f);
            Vector3 camPosition = camRotation * new Vector3(0f, 0f, -cameraDistance) + _smoothedFocusPosition;

            playerRoot.playerCamera.transform.rotation = camRotation;
            playerRoot.playerCamera.transform.position = camPosition;

            if (!Mathf.Approximately(_lastSentX, _x))
            {
                _lastSentX = _x;
                transform.rotation = Quaternion.Euler(0f, _lastSentX, 0f);
            }
        }
        
        public void OverrideYawOnce(float worldYawDegrees)
        {
            _x = worldYawDegrees;
            _lastSentX = _x;

            if (cameraFocusPoint != null)
                _smoothedFocusPosition = cameraFocusPoint.position;

            if (playerRoot == null || playerRoot.playerCamera == null)
                return;

            Quaternion camRotation = Quaternion.Euler(_y, _x, 0f);
            Vector3 camPosition = camRotation * new Vector3(0f, 0f, -cameraDistance) + _smoothedFocusPosition;

            playerRoot.playerCamera.transform.rotation = camRotation;
            playerRoot.playerCamera.transform.position = camPosition;

            transform.rotation = Quaternion.Euler(0f, _x, 0f);
        }
    }
}
