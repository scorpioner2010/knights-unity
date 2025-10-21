using System;
using Game.Scripts.API.Endpoints;
using UnityEngine;

namespace Game.Scripts.API.ServerManagers
{
    public class WarriorsServer : MonoBehaviour
    {
        private static WarriorDto[] _allWarriors;

        public static WarriorDto GetWarrior(string code)
        {
            foreach (WarriorDto warrior in _allWarriors)
            {
                if (warrior.code == code)
                {
                    return warrior;
                }
            }
            
            return null;
        }
        
        public static async void InitWarriors()
        {
            try
            {
                //if (_allWarriors.Length == 0)
                //{
                    (bool ok, string message, WarriorDto[] data) all = await WarriorsManager.GetAll();
                    _allWarriors = all.data;
                //}
            }
            catch (Exception e)
            {
                throw; // TODO handle exception
            }
        }
    }
}
