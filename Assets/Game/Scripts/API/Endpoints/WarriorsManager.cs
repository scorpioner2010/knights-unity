using System;
using Cysharp.Threading.Tasks;
using Game.Scripts.API.Helpers;
using UnityEngine;
using UnityEngine.Networking;

namespace Game.Scripts.API.Endpoints
{
    public static class WarriorsManager
    {
        public static async UniTask<(bool ok, string message, WarriorDto[] data)> GetAll(string culture = null, string branch = null)
        {
            string url = HttpLink.APIBase + "/warriors";
            if (!string.IsNullOrEmpty(culture)) url += $"?culture={culture}";
            if (!string.IsNullOrEmpty(branch)) url += string.IsNullOrEmpty(culture) ? $"?branch={branch}" : $"&branch={branch}";

            UnityWebRequest request = UnityWebRequest.Get(url);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.certificateHandler = new AcceptAllCertificates();

            try { await request.SendWebRequest(); }
            catch (UnityWebRequestException) { return (false, "Request failed", null); }

            string text = request.downloadHandler.text;
            if (request.result == UnityWebRequest.Result.Success)
            {
                WarriorDto[] data = JsonHelper.FromJson<WarriorDto>(text);
                return (true, text, data);
            }

            return (false, text, null);
        }

        public static async UniTask<(bool ok, string message, WarriorDto data)> GetById(int id)
        {
            UnityWebRequest request = UnityWebRequest.Get($"{HttpLink.APIBase}/warriors/{id}");
            request.downloadHandler = new DownloadHandlerBuffer();
            request.certificateHandler = new AcceptAllCertificates();

            try { await request.SendWebRequest(); }
            catch (UnityWebRequestException) { return (false, "Request failed", null); }

            string text = request.downloadHandler.text;
            if (request.result == UnityWebRequest.Result.Success)
            {
                WarriorDto data = JsonUtility.FromJson<WarriorDto>(text);
                return (true, text, data);
            }

            return (false, text, null);
        }

        public static async UniTask<(bool ok, string message, WarriorDto data)> GetByCode(string code)
        {
            UnityWebRequest request = UnityWebRequest.Get($"{HttpLink.APIBase}/warriors/by-code/{code}");
            request.downloadHandler = new DownloadHandlerBuffer();
            request.certificateHandler = new AcceptAllCertificates();

            try { await request.SendWebRequest(); }
            catch (UnityWebRequestException) { return (false, "Request failed", null); }

            string text = request.downloadHandler.text;
            if (request.result == UnityWebRequest.Result.Success)
            {
                WarriorDto data = JsonUtility.FromJson<WarriorDto>(text);
                return (true, text, data);
            }

            return (false, text, null);
        }

        public static async UniTask<(bool ok, string message, WarriorGraphResponse data)> GetGraph(string culture = null)
        {
            string url = HttpLink.APIBase + "/warriors/graph";
            if (!string.IsNullOrEmpty(culture))
                url += "?culture=" + culture;

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

        // 🔹 НОВЕ: отримати вимоги на дослідження для конкретного юніта (список предків і потрібний XP)
        // GET /warriors/{id}/research-from  -> [{ predecessorId, requiredXp }]
        public static async UniTask<(bool ok, string message, ResearchFromEntry[] data)> GetResearchFrom(int successorWarriorId)
        {
            string url = $"{HttpLink.APIBase}/warriors/{successorWarriorId}/research-from";

            UnityWebRequest request = UnityWebRequest.Get(url);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.certificateHandler = new AcceptAllCertificates();

            try { await request.SendWebRequest(); }
            catch (UnityWebRequestException) { return (false, "Request failed", null); }

            string text = request.downloadHandler.text;
            if (request.result == UnityWebRequest.Result.Success)
            {
                ResearchFromEntry[] data = JsonHelper.FromJson<ResearchFromEntry>(text);
                return (true, text, data);
            }

            return (false, text, null);
        }
    }

    [Serializable]
    public class WarriorDto
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

    // 🔹 НОВЕ: DTO для /warriors/{id}/research-from
    [Serializable]
    public class ResearchFromEntry
    {
        public int predecessorId;
        public int requiredXp;
    }
}
