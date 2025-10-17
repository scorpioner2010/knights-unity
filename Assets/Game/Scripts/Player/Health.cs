using System;
using System.Linq;
using FishNet.Object;
using Game.Scripts.Core.Services;
using Game.Scripts.Networking.Lobby;
using Game.Scripts.World.Spawns;
using UnityEngine;
using UnityEngine.Events;

namespace Game.Scripts.Player
{
    public class Health : NetworkBehaviour
    {
        public int maxHp = 100;
        public int hp;

        public Action<int, int, int> OnDamaged;
        public UnityEvent onDeath;

        private HealthBar _healthBar;
        public PlayerRoot playerRoot;

        public override void OnStartNetwork()
        {
            if (IsServer)
            {
                hp = maxHp;
            }
        }

        public override void OnStartClient()
        {
            if (IsOwner)
            {
                TryBindHealthBar();
                UpdateOwnerHud(maxHp);
            }
        }

        private void TryBindHealthBar()
        {
            if (_healthBar == null)
            {
                _healthBar = Singleton<HealthBar>.Instance;
            }
        }

        [Server]
        public void ApplyDamage(int dmg, Vector3 hitPoint, Vector3 impulse, NetworkObject attacker)
        {
            if (hp <= 0)
            {
                return;
            }
            
            hp = Mathf.Max(0, hp - dmg);
            DamageObserversRpc(dmg, hitPoint, impulse, attacker.ObjectId, hp);
            
            PlayerRoot attackerRoot = playerRoot.serverRoom.players
                .Select(p => p.playerRoot)
                .FirstOrDefault(r => r != null && r.OwnerId == attacker.OwnerId);

            attackerRoot?.statisticCounter.AddDamage(dmg);
            
            if (hp == 0)
            {
                DeathServer();
                attackerRoot?.statisticCounter.AddKill();
            }
        }

        [Server]
        private void DeathServer()
        {
            if (!playerRoot.IsDead.Value)
            {
                playerRoot.SetDeadServer();
            }

            OffColliders();
            playerRoot.animationController.TriggerAnimationObserversRpc("Die");
            DiedObserversRpc();
            
            if (IsOneTeamLeft(out Team leftTeam))
            {
                if (leftTeam != Team.Draw)
                {
                    playerRoot.serverRoom.gameplayTimer.Close();
                }
            }
        }
        
        private bool IsOneTeamLeft(out Team winner)
        {
            winner = default;
            
            PlayerRoot[] players = playerRoot.serverRoom.players.Select(p=>p.playerRoot).ToArray();

            bool anyAlive = false;
            bool hasRed = false;
            bool hasBlue = false;

            for (int i = 0; i < players.Length; i++)
            {
                PlayerRoot p = players[i];
                
                if (p == null || !p.gameObject.activeInHierarchy || p.IsDead.Value)
                {
                    continue;
                }

                anyAlive = true;
                
                if (p.Team.Value == Team.Red)
                {
                    hasRed = true;
                }
                else if (p.Team.Value == Team.Blue)
                {
                    hasBlue = true;
                }

                if (hasRed && hasBlue)
                {
                    winner = default;
                    return false; // обидві команди ще живі
                }
            }

            if (!anyAlive)
            {
                winner = default;
                return false; // нікого не лишилось
            }

            winner = hasRed ? Team.Red : Team.Blue;
            return true; // лишилась одна команда
        }

        private void OffColliders()
        {
            playerRoot.playerCollider.enabled = false;
            playerRoot.characterController.enabled = false;
        }

        [ObserversRpc]
        private void DamageObserversRpc(int dmg, Vector3 hitPoint, Vector3 impulse, int attackerObjectId, int newHp)
        {
            hp = newHp;
            OnDamaged?.Invoke(dmg, newHp, maxHp);
            if (!IsOwner)
            {
                return;
            }
            TryBindHealthBar();
            UpdateOwnerHud(newHp);
        }

        [ObserversRpc]
        private void DiedObserversRpc()
        {
            onDeath?.Invoke();
            OffColliders();
        }

        private void UpdateOwnerHud(int currentHp)
        {
            if (_healthBar == null || _healthBar.slider == null)
            {
                return;
            }
            _healthBar.SetHpView(currentHp, maxHp);
        }
    }
}
