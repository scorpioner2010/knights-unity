using UnityEngine;

namespace Game.Scripts.Player
{
    public class SmoothLookAtCamera : MonoBehaviour
    {
        [HideInInspector] public UnityEngine.Camera targetCamera;
        public float rotateSpeed = 2f;

        private bool _isRotating;
        private bool _isDestroy;

        private void OnDestroy()
        {
            _isDestroy = true;
        }

        public void Activate(UnityEngine.Camera cam)
        {
            targetCamera = cam;
            
            if (targetCamera != null)
            {
                _isRotating = true;
            }
        }

        public void Deactivate()
        {
            _isRotating = false;
        }

        private void Update()
        {
            if (_isDestroy)
            {
                return;
            }
            
            if (!_isRotating)
            {
                return;
            }

            if (targetCamera == null)
            {
                return;
            }

            // Напрямок до камери, ігноруючи Y
            Vector3 direction = targetCamera.transform.position - transform.position;
            direction.y = 0;
            if (direction.sqrMagnitude < 0.001f)
            {
                return;
            }
            
            Quaternion targetRotation = Quaternion.LookRotation(direction); transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotateSpeed);
        }
    }
}