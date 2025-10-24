using System;
using Game.Scripts.Player;
using Game.Scripts.World.Spawns;

namespace Game.Scripts.Networking.Lobby
{
    [Serializable]
    public class Player
    {
        public string loginName;
        public int clientId;
        public PlayerRoot playerRoot;
        public bool connected; //for random game
        public Team team;
        public int matchId;
        public bool isLoaded;

        public bool IsBot => clientId <= BotStartNumber;
        public static int BotStartNumber = -10;
    }
}