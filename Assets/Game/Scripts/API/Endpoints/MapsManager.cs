using System;
using Cysharp.Threading.Tasks;
using Game.Scripts.API.Helpers;
using UnityEngine.Networking;

namespace Game.Scripts.API.Endpoints
{
    public static class MapsManager
    {
        public static async UniTask<(bool isSuccess, string message, MapDto[] maps)> GetAllMaps()
        {
            string url = HttpLink.APIBase + "/maps";

            UnityWebRequest request = UnityWebRequest.Get(url);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.certificateHandler = new AcceptAllCertificates();
            request.SetRequestHeader("Content-Type", "application/json");

            try { await request.SendWebRequest(); }
            catch (UnityWebRequestException) { return (false, "Request failed", null); }

            string text = request.downloadHandler.text;
            if (request.result == UnityWebRequest.Result.Success)
            {
                MapDto[] data = JsonHelper.FromJson<MapDto>(text);
                return (true, text, data);
            }

            return (false, text, null);
        }
    }

    [Serializable]
    public class MapDto
    {
        public int id;
        public string code;
        public string name;
        public string description;
    }
}