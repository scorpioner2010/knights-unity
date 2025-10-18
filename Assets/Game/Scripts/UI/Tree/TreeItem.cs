using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Scripts.UI.Tree
{
    public class TreeItem : MonoBehaviour
    {
        public Button button;
        public TMP_Text vehicleName;
        public TMP_Text level;
        public Image image;
        public Vehicle vehicleType;
        public GameObject isClose;
        public GameObject isHave;
        public RectTransform rectTransform;
        
        public Image priceIcon;
        public TMP_Text price;

        public Image xpIcon;
        public TMP_Text xp;
        
        public void SetActiveCoinsView(bool active)
        {
            priceIcon.gameObject.SetActive(active);
        }
        
        public void SetActiveXpView(bool active)
        {
            xpIcon.gameObject.SetActive(active);
        }
    }
}