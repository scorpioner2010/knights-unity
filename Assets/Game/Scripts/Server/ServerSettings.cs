using UnityEngine;

namespace Game.Scripts.Server
{
    public class ServerSettings : MonoBehaviour
    {
        private static ServerSettings _in;
        
        public bool isTestMode;
        public static bool IsTestMode => _in.isTestMode;
        public static int FindRoomSeconds => _in.findRoomSeconds;
        public static int MaxPlayersForFindRoom => _in.maxPlayersForFindRoom;
        
        public int maxPlayersForFindRoom = 1;
        public int findRoomSeconds = 60;
        
        private void Awake()
        {
            _in = this;
        }
    }
}