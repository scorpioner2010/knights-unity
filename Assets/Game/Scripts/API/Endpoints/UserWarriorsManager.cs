using System;
using System.Text;
using Cysharp.Threading.Tasks;
using Game.Scripts.API.Helpers;
using UnityEngine;
using UnityEngine.Networking;

namespace Game.Scripts.API.Endpoints
{
    public static class UserWarriorsManager
    {
        public static async UniTask<(bool ok, string message, MyWarriorsResponse data)> GetMyWarriors(string token)
        {
            UnityWebRequest request = UnityWebRequest.Get($"{HttpLink.APIBase}/user-warriors/me");
            request.downloadHandler = new DownloadHandlerBuffer();
            request.certificateHandler = new AcceptAllCertificates();
            request.SetRequestHeader("Authorization", "Bearer " + token);

            try { await request.SendWebRequest(); }
            catch (UnityWebRequestException) { return (false, "Request failed", null); }

            string text = request.downloadHandler.text;
            if (request.result == UnityWebRequest.Result.Success)
            {
                MyWarriorsResponse data = JsonUtility.FromJson<MyWarriorsResponse>(text);
                return (true, text, data);
            }

            return (false, text, null);
        }

        public static async UniTask<(bool ok, string message)> SetActiveWarrior(int warriorId, string token)
        {
            UnityWebRequest request = UnityWebRequest.Put($"{HttpLink.APIBase}/user-warriors/me/active/{warriorId}", "");
            request.downloadHandler = new DownloadHandlerBuffer();
            request.certificateHandler = new AcceptAllCertificates();
            request.SetRequestHeader("Authorization", "Bearer " + token);

            try { await request.SendWebRequest(); }
            catch (UnityWebRequestException) { return (false, "Request failed"); }

            return (request.result == UnityWebRequest.Result.Success, request.downloadHandler.text);
        }

        public static async UniTask<(bool ok, string message)> BuyWarrior(string code, string token)
        {
            UnityWebRequest request = new UnityWebRequest($"{HttpLink.APIBase}/user-warriors/me/buy/{code}", "POST")
            {
                downloadHandler = new DownloadHandlerBuffer(),
                certificateHandler = new AcceptAllCertificates()
            };
            request.SetRequestHeader("Authorization", "Bearer " + token);

            try { await request.SendWebRequest(); }
            catch (UnityWebRequestException) { return (false, "Request failed"); }

            return (request.result == UnityWebRequest.Result.Success, request.downloadHandler.text);
        }

        public static async UniTask<(bool ok, string message)> SellWarrior(int warriorId, string token)
        {
            UnityWebRequest request = new UnityWebRequest($"{HttpLink.APIBase}/user-warriors/me/sell/{warriorId}", "POST")
            {
                downloadHandler = new DownloadHandlerBuffer(),
                certificateHandler = new AcceptAllCertificates()
            };
            request.SetRequestHeader("Authorization", "Bearer " + token);

            try { await request.SendWebRequest(); }
            catch (UnityWebRequestException) { return (false, "Request failed"); }

            return (request.result == UnityWebRequest.Result.Success, request.downloadHandler.text);
        }

        public static async UniTask<(bool ok, string message)> ConvertFreeXp(int warriorId, int amount, string token)
        {
            var payload = JsonUtility.ToJson(new ConvertFreeXpRequest { amount = amount });

            UnityWebRequest request = new UnityWebRequest($"{HttpLink.APIBase}/user-warriors/{warriorId}/convert-freexp", "POST")
            {
                uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(payload)),
                downloadHandler = new DownloadHandlerBuffer(),
                certificateHandler = new AcceptAllCertificates()
            };
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", "Bearer " + token);

            try { await request.SendWebRequest(); }
            catch (UnityWebRequestException) { return (false, "Request failed"); }

            return (request.result == UnityWebRequest.Result.Success, request.downloadHandler.text);
        }
    }

    [Serializable]
    public class MyWarriorsResponse
    {
        public int freeXp;
        public UserWarriorDto[] warriors;
    }

    [Serializable]
    public class UserWarriorDto
    {
        public int id;
        public int warriorId;
        public string warriorCode;
        public string warriorName;
        public int xp;
        public bool isActive;
    }

    [Serializable]
    public class ConvertFreeXpRequest
    {
        public int amount;
    }
}
