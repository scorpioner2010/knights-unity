using System;
using System.Text;
using Cysharp.Threading.Tasks;
using Game.Scripts.API.Helpers;
using UnityEngine;
using UnityEngine.Networking;

namespace Game.Scripts.API.Endpoints
{
    public static class MatchesManager
    {
        public static async UniTask<(bool ok, string message, MatchStartResponse data)> StartMatch(string map, string token)
        {
            var payload = JsonUtility.ToJson(new StartMatchRequest { map = map });
            UnityWebRequest request = new UnityWebRequest($"{HttpLink.APIBase}/matches/start", "POST")
            {
                uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(payload)),
                downloadHandler = new DownloadHandlerBuffer(),
                certificateHandler = new AcceptAllCertificates()
            };
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", "Bearer " + token);

            try { await request.SendWebRequest(); }
            catch (UnityWebRequestException) { return (false, "Request failed", null); }

            string text = request.downloadHandler.text;
            if (request.result == UnityWebRequest.Result.Success)
            {
                MatchStartResponse data = JsonUtility.FromJson<MatchStartResponse>(text);
                return (true, text, data);
            }

            return (false, text, null);
        }

        public static async UniTask<(bool ok, string message)> EndMatch(int matchId, EndMatchRequest body, string token)
        {
            string json = JsonUtility.ToJson(body);
            UnityWebRequest request = new UnityWebRequest($"{HttpLink.APIBase}/matches/{matchId}/end", "POST")
            {
                uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json)),
                downloadHandler = new DownloadHandlerBuffer(),
                certificateHandler = new AcceptAllCertificates()
            };
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", "Bearer " + token);

            try { await request.SendWebRequest(); }
            catch (UnityWebRequestException) { return (false, "Request failed"); }

            return (request.result == UnityWebRequest.Result.Success, request.downloadHandler.text);
        }

        public static async UniTask<(bool ok, string message, ParticipantEntry[] participants)> GetParticipants(int matchId, string token)
        {
            UnityWebRequest request = UnityWebRequest.Get($"{HttpLink.APIBase}/matches/{matchId}/participants");
            request.downloadHandler = new DownloadHandlerBuffer();
            request.certificateHandler = new AcceptAllCertificates();
            request.SetRequestHeader("Authorization", "Bearer " + token);

            try { await request.SendWebRequest(); }
            catch (UnityWebRequestException) { return (false, "Request failed", null); }

            string text = request.downloadHandler.text;
            if (request.result == UnityWebRequest.Result.Success)
            {
                ParticipantEntry[] data = JsonHelper.FromJson<ParticipantEntry>(text);
                return (true, text, data);
            }

            return (false, text, null);
        }
    }

    [Serializable] public class StartMatchRequest { public string map; }
    [Serializable] public class MatchStartResponse { public int matchId; }

    [Serializable]
    public class EndMatchRequest
    {
        public ParticipantInput[] participants;
    }

    [Serializable]
    public class ParticipantInput
    {
        public int userId;
        public int warriorId;
        public int team;
        public string result;
        public int kills;
        public int damage;
    }

    [Serializable]
    public class ParticipantEntry
    {
        public int userId;
        public string username;
        public int warriorId;
        public string warriorName;
        public int team;
        public string result;
        public int kills;
        public int damage;
        public int xpEarned;
        public int mmrDelta;
    }
}
