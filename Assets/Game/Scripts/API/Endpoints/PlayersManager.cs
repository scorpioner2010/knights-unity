using System;
using Cysharp.Threading.Tasks;
using Game.Scripts.API.Helpers;
using UnityEngine;
using UnityEngine.Networking;

namespace Game.Scripts.API.Endpoints
{
    public static class PlayersManager
    {
        public static async UniTask<(bool ok, string message, PlayerProfileDto data)> GetMyProfile(string token)
        {
            UnityWebRequest request = UnityWebRequest.Get($"{HttpLink.APIBase}/players/me");
            request.downloadHandler = new DownloadHandlerBuffer();
            request.certificateHandler = new AcceptAllCertificates();
            request.SetRequestHeader("Authorization", "Bearer " + token);

            try { await request.SendWebRequest(); }
            catch (UnityWebRequestException) { return (false, "Request failed", null); }

            string text = request.downloadHandler.text;
            if (request.result == UnityWebRequest.Result.Success)
            {
                PlayerProfileDto data = JsonUtility.FromJson<PlayerProfileDto>(text);
                return (true, text, data);
            }

            return (false, text, null);
        }

        public static async UniTask<(bool ok, string message)> SetActiveWarrior(int warriorId, string token)
        {
            UnityWebRequest request = UnityWebRequest.Put($"{HttpLink.APIBase}/players/me/active/{warriorId}", "");
            request.downloadHandler = new DownloadHandlerBuffer();
            request.certificateHandler = new AcceptAllCertificates();
            request.SetRequestHeader("Authorization", "Bearer " + token);

            try { await request.SendWebRequest(); }
            catch (UnityWebRequestException) { return (false, "Request failed"); }

            return (request.result == UnityWebRequest.Result.Success, request.downloadHandler.text);
        }
    }

    [Serializable]
    public class PlayerProfileDto
    {
        public int id;
        public string username;
        public bool isAdmin;
        public int mmr;
        public int coins;
        public int gold;
        public int freeXp;
        public int activeWarriorId;
        public string activeWarriorCode;
        public string activeWarriorName;
        public OwnedWarriorDto[] ownedWarriors;
        
        public OwnedWarriorDto GetSelected()
        {
            OwnedWarriorDto active = null;

            foreach (OwnedWarriorDto dto in ownedWarriors)
            {
                if (activeWarriorId == dto.warriorId)
                {
                    return dto;
                }
            }
            
            return null;
        }

        public bool IsHave(int idVehicle)
        {
            foreach (OwnedWarriorDto vehicleDto in ownedWarriors)
            {
                if (vehicleDto.warriorId == idVehicle)
                {
                    return true;
                }
            }
            
            return false;
        }
    }

    [Serializable]
    public class OwnedWarriorDto
    {
        public int warriorId;
        public string code;
        public string name;
        public bool isActive;
        public int xp;
    }
}
