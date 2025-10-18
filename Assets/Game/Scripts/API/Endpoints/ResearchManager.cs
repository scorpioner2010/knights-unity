using System;
using System.Text;
using Cysharp.Threading.Tasks;
using Game.Scripts.API.Helpers;
using UnityEngine;
using UnityEngine.Networking;

namespace Game.Scripts.API.Endpoints
{
    // відпрацьовує дослідження (unlock) юнітів та читання списку розблокованих
    public static class ResearchManager
    {
        // POST /user-warriors/me/research-unlock
        // body: { successorWarriorId, predecessorWarriorId }
        public static async UniTask<(bool ok, string message)> Unlock(int successorWarriorId, int predecessorWarriorId, string token)
        {
            string url = $"{HttpLink.APIBase}/user-warriors/me/research-unlock";

            Payload body = new Payload
            {
                successorWarriorId = successorWarriorId,
                predecessorWarriorId = predecessorWarriorId
            };

            string json = JsonUtility.ToJson(body);

            UnityWebRequest req = new UnityWebRequest(url, "POST")
            {
                uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json)),
                downloadHandler = new DownloadHandlerBuffer(),
                certificateHandler = new AcceptAllCertificates()
            };

            req.SetRequestHeader("Content-Type", "application/json");
            req.SetRequestHeader("Authorization", "Bearer " + token);

            try { await req.SendWebRequest(); }
            catch (UnityWebRequestException) { return (false, "Request failed"); }

            return (req.result == UnityWebRequest.Result.Success, req.downloadHandler.text);
        }

        // GET /user-warriors/me/unlocked
        // очікуємо масив вигляду: [{ "value": 12 }, { "value": 17 }, ...] — де value = WarriorId (successor)
        public static async UniTask<(bool ok, string message, int[] ids)> GetMyUnlocked(string token)
        {
            string url = $"{HttpLink.APIBase}/user-warriors/me/unlocked";

            UnityWebRequest req = UnityWebRequest.Get(url);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.certificateHandler = new AcceptAllCertificates();
            req.SetRequestHeader("Authorization", "Bearer " + token);

            try { await req.SendWebRequest(); }
            catch (UnityWebRequestException) { return (false, "Request failed", null); }

            string text = req.downloadHandler.text;

            if (req.result == UnityWebRequest.Result.Success)
            {
                // перетворюємо [{value: id}, ...] у int[]
                IntWrap[] arr = JsonHelper.FromJson<IntWrap>(text);
                if (arr != null)
                {
                    int[] ids = Array.ConvertAll(arr, it => it.value);
                    return (true, text, ids);
                }
            }

            return (false, text, null);
        }

        [Serializable]
        private class Payload
        {
            public int successorWarriorId;
            public int predecessorWarriorId;
        }

        [Serializable]
        private class IntWrap
        {
            public int value;
        }
    }
}
