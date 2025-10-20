using UnityEngine;
using UnityEngine.EventSystems;

namespace Game.Scripts
{
    public class MobileShieldHold : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        public MobileManager manager;

        public void OnPointerDown(PointerEventData eventData)
        {
            if (manager != null) manager.StartShield();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (manager != null) manager.StopShield();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (manager != null) manager.StopShield();
        }
    }
}