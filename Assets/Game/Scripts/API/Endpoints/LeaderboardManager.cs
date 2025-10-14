using System;
using Cysharp.Threading.Tasks;
using Game.Scripts.API.Helpers;
using UnityEngine.Networking;

namespace Game.Scripts.API.Endpoints
{
    public static class LeaderboardManager
    {
        private static async UniTask<(bool, string, LeaderboardEntry[])> GetLeaderboard(string url, string token = null)
        {
            UnityWebRequest request = UnityWebRequest.Get(url);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.certificateHandler = new AcceptAllCertificates();
            request.SetRequestHeader("Content-Type", "application/json");
            if (!string.IsNullOrEmpty(token))
                request.SetRequestHeader("Authorization", "Bearer " + token);

            try { await request.SendWebRequest(); }
            catch (UnityWebRequestException) { return (false, "Request failed", null); }

            string text = request.downloadHandler.text;
            if (request.result == UnityWebRequest.Result.Success)
            {
                LeaderboardEntry[] data = JsonHelper.FromJson<LeaderboardEntry>(text);
                return (true, text, data);
            }

            return (false, text, null);
        }

        public static UniTask<(bool, string, LeaderboardEntry[])> GetMmrLeaderboard(int top, string token)
            => GetLeaderboard($"{HttpLink.APIBase}/leaderboard/mmr?top={top}", token);

        public static UniTask<(bool, string, LeaderboardEntry[])> GetFreeXpLeaderboard(int top, string token)
            => GetLeaderboard($"{HttpLink.APIBase}/leaderboard/free-xp?top={top}", token);

        public static UniTask<(bool, string, WarriorXpEntry[])> GetWarriorXpLeaderboard(int top, string token)
        {
            return GetWarriorXpInternal($"{HttpLink.APIBase}/leaderboard/warrior-xp?top={top}", token);
        }

        private static async UniTask<(bool, string, WarriorXpEntry[])> GetWarriorXpInternal(string url, string token)
        {
            UnityWebRequest request = UnityWebRequest.Get(url);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.certificateHandler = new AcceptAllCertificates();
            request.SetRequestHeader("Authorization", "Bearer " + token);

            try { await request.SendWebRequest(); }
            catch (UnityWebRequestException) { return (false, "Request failed", null); }

            string text = request.downloadHandler.text;
            if (request.result == UnityWebRequest.Result.Success)
            {
                WarriorXpEntry[] data = JsonHelper.FromJson<WarriorXpEntry>(text);
                return (true, text, data);
            }

            return (false, text, null);
        }
    }

    [Serializable]
    public class LeaderboardEntry
    {
        public int userId;
        public string username;
        public int value;
    }

    [Serializable]
    public class WarriorXpEntry
    {
        public int userId;
        public string username;
        public string warriorName;
        public int xp;
    }
}
