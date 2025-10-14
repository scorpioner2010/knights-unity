using System;
using Cysharp.Threading.Tasks;
using Game.Scripts.API.Helpers;
using UnityEngine;
using UnityEngine.Networking;

namespace Game.Scripts.API.Endpoints
{
    public static class WarriorsManager
    {
        public static async UniTask<(bool ok, string message, VehicleLite[] data)> GetAll(string faction = null, string branch = null)
        {
            string url = HttpLink.APIBase + "/warriors";
            if (!string.IsNullOrEmpty(faction)) url += $"?faction={faction}";
            if (!string.IsNullOrEmpty(branch)) url += string.IsNullOrEmpty(faction) ? $"?branch={branch}" : $"&branch={branch}";

            UnityWebRequest request = UnityWebRequest.Get(url);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.certificateHandler = new AcceptAllCertificates();

            try { await request.SendWebRequest(); }
            catch (UnityWebRequestException) { return (false, "Request failed", null); }

            string text = request.downloadHandler.text;
            if (request.result == UnityWebRequest.Result.Success)
            {
                VehicleLite[] data = JsonHelper.FromJson<VehicleLite>(text);
                return (true, text, data);
            }

            return (false, text, null);
        }

        public static async UniTask<(bool ok, string message, VehicleLite data)> GetById(int id)
        {
            UnityWebRequest request = UnityWebRequest.Get($"{HttpLink.APIBase}/warriors/{id}");
            request.downloadHandler = new DownloadHandlerBuffer();
            request.certificateHandler = new AcceptAllCertificates();

            try { await request.SendWebRequest(); }
            catch (UnityWebRequestException) { return (false, "Request failed", null); }

            string text = request.downloadHandler.text;
            if (request.result == UnityWebRequest.Result.Success)
            {
                VehicleLite data = JsonUtility.FromJson<VehicleLite>(text);
                return (true, text, data);
            }

            return (false, text, null);
        }

        public static async UniTask<(bool ok, string message, VehicleLite data)> GetByCode(string code)
        {
            UnityWebRequest request = UnityWebRequest.Get($"{HttpLink.APIBase}/warriors/by-code/{code}");
            request.downloadHandler = new DownloadHandlerBuffer();
            request.certificateHandler = new AcceptAllCertificates();

            try { await request.SendWebRequest(); }
            catch (UnityWebRequestException) { return (false, "Request failed", null); }

            string text = request.downloadHandler.text;
            if (request.result == UnityWebRequest.Result.Success)
            {
                VehicleLite data = JsonUtility.FromJson<VehicleLite>(text);
                return (true, text, data);
            }

            return (false, text, null);
        }

        public static async UniTask<(bool ok, string message, WarriorGraphResponse data)> GetGraph(string faction = null)
        {
            string url = HttpLink.APIBase + "/warriors/graph";
            if (!string.IsNullOrEmpty(faction))
                url += "?faction=" + faction;

            UnityWebRequest request = UnityWebRequest.Get(url);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.certificateHandler = new AcceptAllCertificates();

            try { await request.SendWebRequest(); }
            catch (UnityWebRequestException) { return (false, "Request failed", null); }

            string text = request.downloadHandler.text;
            if (request.result == UnityWebRequest.Result.Success)
            {
                WarriorGraphResponse data = JsonUtility.FromJson<WarriorGraphResponse>(text);
                return (true, text, data);
            }

            return (false, text, null);
        }
    }
//WarriorDto
    [Serializable]
    public class VehicleLite
    {
        public int id;
        public string code;
        public string name;
        public string branch;
        public string cultureCode;
        public string cultureName;
        public string @class;
        public int level;
        public int purchaseCost;
        public bool isVisible;
        public int hp;
        public int damage;
        public float accuracy;
        public float speed;
        public float acceleration;
        public float traverseSpeed;
        public string armor;
    }
    
    [Serializable]
    public class WarriorGraphResponse
    {
        public WarriorGraphNode[] nodes;
        public WarriorGraphEdge[] edges;
    }
    
    [Serializable]
    public class WarriorGraphNode
    {
        public int id;
        public string code;
        public string name;
        public string @class;
        public int level;
        public string branch;
        public string cultureCode;
        public bool isVisible;
    }
    
    [Serializable]
    public class WarriorGraphEdge
    {
        public int fromId;
        public int toId;
        public int requiredXp;
    }
}
