using UnityEngine;

namespace Game.Scripts.Player
{
    public class GroundChecker : MonoBehaviour
    {
        public PlayerRoot playerRoot;

        [Header("Ground Check")]
        public Transform groundCheck;
        public float groundCheckDistance = 0.1f;
        public LayerMask groundMask;

        [Header("Debug")]
        public bool isGrounded;

        private void FixedUpdate()
        {
            CheckGround();
        }

        private void CheckGround()
        {
            if (groundCheck == null)
            {
                Debug.LogWarning("GroundCheck transform not assigned!");
                return;
            }

            isGrounded = Physics.Raycast(groundCheck.position, Vector3.down, groundCheckDistance, groundMask);
        }
    }
}