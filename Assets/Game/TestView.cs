using UnityEngine;

namespace Game
{
    public class TestView : MonoBehaviour
    {
        public GameObject[] testObj;

        [Range(0f, 1f)] public float widthPct = 0.22f;
        [Range(0f, 1f)] public float heightPct = 0.08f;
        [Range(0f, 1f)] public float marginXPct = 0.02f;
        [Range(0f, 1f)] public float marginYPct = 0.02f;

        private bool _enabledAll = true;

        private void ApplyState()
        {
            for (int i = 0; i < testObj.Length; i++)
            {
                if (testObj[i] != null)
                {
                    testObj[i].SetActive(_enabledAll);
                }
            }
        }

        private void OnGUI()
        {
            float w = Mathf.Clamp01(widthPct) * Screen.width;
            float h = Mathf.Clamp01(heightPct) * Screen.height;
            float mx = Mathf.Clamp01(marginXPct) * Screen.width;
            float my = Mathf.Clamp01(marginYPct) * Screen.height;
            float x = mx;
            float y = Screen.height - h - my;

            Rect r = new Rect(x, y, w, h);
            string label = _enabledAll ? "Вимкнути тест-об'єкти" : "Увімкнути тест-об'єкти";
            if (GUI.Button(r, label))
            {
                _enabledAll = !_enabledAll;
                ApplyState();
            }
        }
    }
}