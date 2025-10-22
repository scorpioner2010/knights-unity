using Game.Scripts.Player;
using UnityEngine;

namespace Game.Scripts
{
    public class MeshPack : MonoBehaviour
    {
        public Avatar humanAvatar;
        public Transform bladeRoot;
        public Transform bladeTip;
    
        private Animator _animator;
        private PlayerRoot _playerRoot;
    
        public void Init(Animator animator, PlayerRoot playerRoot)
        {
            _playerRoot = playerRoot;
            _animator = animator;
            _animator.avatar = humanAvatar;
            _animator.Rebind();
            
            (Transform root, Transform tip) comps = GetRoot();
            _playerRoot.meleeWeapon.InitWeapon(comps.root, comps.tip);
        }

        public (Transform root, Transform tip) GetRoot()
        {
            return (bladeRoot, bladeTip);
        }
    }
}