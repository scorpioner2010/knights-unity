using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HealthBar : MonoBehaviour
{
    public Slider slider;      // Slider (min=0, max=1)
    public TMP_Text label;     // 100 / 100 тощо (опційно)

    public void SetHpView(int hp, int maxHp)
    {
        float max = Mathf.Max(1f, maxHp);
        float cur01 = Mathf.Clamp01(hp / max); // float-ділення, не інт
        slider.value = cur01;
        label.text = hp.ToString();
    }
}
