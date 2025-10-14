using UnityEngine;

namespace Game.Scripts.Player
{
    public class FootstepEffects : MonoBehaviour
    {
        public float baseStepInterval = 0.5f;
        public float speedReference = 5f;
        public ParticleSystem stepFXPrefab;
        public Transform[] foots;
        public Vector3 fxSpawnOffset;
        
        public PlayerRoot playerRoot;
        
        private int _counterFoot;
        private Vector3 _lastPosition;
        private float _stepTimer;
    
        private void Start() => _lastPosition = transform.position;

        private void FixedUpdate()
        {
            float speed = (transform.position - _lastPosition).magnitude / Time.fixedDeltaTime;
            _lastPosition = transform.position;
            float currentStepInterval = speed > 0 ? baseStepInterval * (speedReference / speed) : float.MaxValue;
            _stepTimer += Time.fixedDeltaTime;
            
            if (_stepTimer >= currentStepInterval)
            {
                _stepTimer = 0f;
                PlayFootstep();
            }
        }
        
        private void PlayFootstep()
        {
            if (playerRoot.characterController.isGrounded == false)
            {
                return;
            }
            
            if (playerRoot.characterMovement.IsMoving)
            {
                if (_counterFoot >= 2)
                {
                    _counterFoot = 0;
                }
                
                Vector3 footPos = foots[_counterFoot].position;
                _counterFoot++;
                ParticleSystem particle = Instantiate(stepFXPrefab, footPos + fxSpawnOffset, Quaternion.identity);
                particle.Play();
                Destroy(particle.gameObject, 2);
            }
        }
    }
}