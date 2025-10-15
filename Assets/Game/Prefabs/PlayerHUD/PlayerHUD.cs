using Game.Scripts.Player;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Script.Player.UI
{
    public class PlayerHUD : MonoBehaviour
    {
        public PlayerRoot tankRoot;
        private Camera _mainCamera;
        [SerializeField] private TMP_Text nickName;
        [SerializeField] private Image hpView;
        [SerializeField] private FloatingText floatingTextPrefab;

        private void Start()
        {
            tankRoot.health.OnDamaged += (dmg, current, max) =>
            {
                hpView.fillAmount = Mathf.Clamp01(current / Mathf.Max(1f, max));
                ShowFloatingText(dmg);
            };
        }

        private void ShowFloatingText(float dmg)
        {
            FloatingText t = Instantiate(floatingTextPrefab, transform.position, Quaternion.identity, transform);
            string damage = Mathf.RoundToInt(dmg).ToString();
            t.SetText(damage);
        }

        public void SetNick(string nick)
        {
            nickName.text = nick;
            gameObject.SetActive(true);
        }
        
        public void SetCamera(Camera cam)
        {
            _mainCamera = cam;
        }
        
        private void LateUpdate()
        {
            if (_mainCamera != null) transform.forward = _mainCamera.transform.forward;
        }
    }
}