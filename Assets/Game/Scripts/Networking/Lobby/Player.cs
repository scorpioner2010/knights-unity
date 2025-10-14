using System;
using FishNet.Connection;
using Game.Script.Player;
using Game.Scripts.Gameplay.Robots;
using Game.Scripts.Player;

namespace Game.Scripts.Networking.Lobby
{
    [Serializable]
    public class Player
    {
        public string loginName;
        public NetworkConnection Connection;
        public PlayerRoot playerRoot;
        public bool isBot;
        public bool randomPlayerConnected; //for random game
    }
}