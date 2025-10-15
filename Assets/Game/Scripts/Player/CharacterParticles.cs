using UnityEngine;

namespace Game.Scripts.Player
{
    public class CharacterParticles : MonoBehaviour
    {
        public ParticleSystem hitPrefab;
        public ParticleSystem hitPrefabServer;
        public void HitEffectPlay(Vector3 spawnPosition) => Play(spawnPosition, hitPrefab);
        public void HitEffectPlayServer(Vector3 spawnPosition) => Play(spawnPosition, hitPrefabServer);
        
        private ParticleSystem Play(Vector3 spawnPosition, ParticleSystem prefab)
        {
            ParticleSystem hit = Instantiate(prefab, null, true);
            hit.transform.position = spawnPosition;
            hit.Play();
            Destroy(hit.gameObject, 2);
            return hit;
        }

        public void PlatTransform(Vector3 spawnPosition, Transform spawnTransform, ParticleSystem prefab)
        {
            ParticleSystem hit = Play(spawnPosition, prefab);
            hit.transform.parent = spawnTransform;
        }
    }
}