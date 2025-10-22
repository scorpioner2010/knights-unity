using System;
using FishNet.Connection;
using Game.Scripts.Player;
using Game.Scripts.World.Spawns;

namespace Game.Scripts.Networking.Lobby
{
    [Serializable]
    public class Player
    {
        public string loginName;
        public NetworkConnection Connection;
        public PlayerRoot playerRoot;
        public bool isBot;
        public bool connected; //for random game
        public Team team;
        public int matchId;
        public bool isLoaded;
    }
}