using FishNet.Object;
using TMPro;
using UnityEngine;

namespace Game.Scripts.Player
{
    public class NickDisplay : NetworkBehaviour
    {
        [SerializeField] private TMP_Text nickText;

        public void SetNick(string nick)
        {
            nickText.text = nick;
        }
    }
}