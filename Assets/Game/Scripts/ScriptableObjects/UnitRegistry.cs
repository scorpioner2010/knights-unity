using System;
using System.Collections.Generic;
using Game.Scripts.Player;
using NaughtyAttributes;
using UnityEngine;

namespace Game.Scripts.ScriptableObjects
{
    [CreateAssetMenu(fileName = "RobotRegistry", menuName = "WOM/Robot Registry")]
    public class UnitRegistry : ScriptableObject
    {
        [Serializable]
        public class Item
        {
            public string code;
            public MeshPack meshPack;
            [ShowAssetPreview] public Sprite icon;
        }

        public PlayerRoot prefab;
        public List<Item> items = new ();
        
        public PlayerRoot GetPrefab()
        {
            return prefab;
        }
        
        public MeshPack GetMesh(string code)
        {
            foreach (Item it in items)
            {
                if (it.code == code)
                {
                    return it.meshPack;
                }
            }
        
            return null;
        }
    
        public Sprite GetIcon(string code)
        {
            foreach (Item it in items)
            {
                if (it.code == code)
                {
                    return it.icon;
                }
            }
        
            return null;
        }
    }
}