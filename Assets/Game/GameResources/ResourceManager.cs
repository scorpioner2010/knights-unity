using Game.Scripts;
using Game.Scripts.Player;
using Game.Scripts.ScriptableObjects;
using UnityEngine;

namespace Game.GameResources
{
    public class ResourceManager : MonoBehaviour
    {
        private static ResourceManager _in;
        public UnitRegistry registry;
        private void Awake() => _in = this;
        public static PlayerRoot GetPrefab() => _in.registry.GetPrefab();
        public static MeshPack GetMesh(string code) => _in.registry.GetMesh(code);
        public static Sprite GetIcon(string code) => _in.registry.GetIcon(code);
    }
}