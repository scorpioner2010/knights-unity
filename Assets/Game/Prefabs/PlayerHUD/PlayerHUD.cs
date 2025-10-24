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
        public TMP_Text nickName;
        public Image hpView;
        public FloatingText floatingTextPrefab;

        public void Init(Camera cam, string nick)
        {
            _mainCamera = cam;
            nickName.text = nick;
            gameObject.SetActive(true);
        }

        public void Deactivate()
        {
            gameObject.SetActive(false);
        }

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
        
        private void LateUpdate()
        {
            if (_mainCamera != null) transform.forward = _mainCamera.transform.forward;
        }
    }
}