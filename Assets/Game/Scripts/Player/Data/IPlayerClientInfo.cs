using Game.Scripts.API.Endpoints;
using Game.Scripts.API.Models;

namespace Game.Scripts.Player.Data
{
    public interface IPlayerClientInfo
    {
        public PlayerProfileDto Profile { get;}
        public int ClientId { get; }
        
        public void SetPlayerData(PlayerProfileDto profile);
        public void SetClientId(int clientId);
    }
}