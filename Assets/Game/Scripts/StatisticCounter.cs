using System;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using Game.Scripts.Player;

namespace Game.Scripts
{
    public class StatisticCounter : NetworkBehaviour
    {
        public PlayerRoot playerRoot;
        public readonly SyncVar<UnitStatistic> UnitStats = new ();

        public void AddKill()
        {
            UnitStatistic info = UnitStats.Value;
            info.kills++;
            UnitStats.Value =  info;
        }
        
        public void AddDamage(int damage)
        {
            UnitStatistic info = UnitStats.Value;
            info.damage += damage;
            UnitStats.Value =  info;
        }
    }

    [Serializable]
    public struct UnitStatistic
    {
        public int kills;
        public int damage;
    }
}