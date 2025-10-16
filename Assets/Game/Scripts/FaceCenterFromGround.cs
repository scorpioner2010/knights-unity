using Game.Scripts.Player;
using UnityEngine;

namespace Game.Scripts
{
    public class FaceCenterFromGround : MonoBehaviour
    {
        [SerializeField] private LayerMask groundLayerMask = ~0; // за замовчуванням — всі шари
        
        public void FaceCenterFromGroundLayer(PlayerRoot playerRoot)
        {
            Transform t = playerRoot.transform;
            Vector3 origin = t.position + Vector3.up * 10f;
            float maxDist = 200f;
            
            if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, maxDist, groundLayerMask, QueryTriggerInteraction.Ignore))
            {
                Vector3 center = hit.collider.bounds.center;
                FaceTowardsXZ(playerRoot, t, center);
            }
            else
            {
                FaceTowardsXZ(playerRoot, t, Vector3.zero);
            }
        }

        private void FaceTowardsXZ(PlayerRoot playerRoot, Transform t, Vector3 target)
        {
            Vector3 look = new Vector3(target.x - t.position.x, 0f, target.z - t.position.z);

            if (look.sqrMagnitude <= 1e-6f)
            {
                return;
            }

            Quaternion rotation = Quaternion.LookRotation(look.normalized, Vector3.up);
            t.rotation = rotation;
            float yawDeg = Mathf.Atan2(look.x, look.z) * Mathf.Rad2Deg;
            playerRoot.characterCameraController.OverrideYawOnce(yawDeg);
        }
    }
}
