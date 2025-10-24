using System.Globalization;
using UnityEngine;

namespace Game.Scripts.Core.Utils
{
    public class FPSCounter : MonoBehaviour
    {
        private const int MaxSamples = 120;
        private readonly float[] _deltas = new float[MaxSamples];
        private int _index;
        private int _count;
        private float _sum;
        
        private float _displayTimer;
        private float _displayInterval = 0.1f;
        private float _fps;

        private GUIStyle _textStyle = new();

        private void Start()
        {
            _textStyle.fontStyle = FontStyle.Bold;
            _textStyle.fontSize = 20;
            _textStyle.normal.textColor = Color.white;
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;

            if (dt < 0.0005f)
            {
                dt = 0.0005f;
            }

            if (dt > 0.5f)
            {
                dt = 0.5f;
            }

            if (_count < MaxSamples)
            {
                _deltas[_count] = dt;
                _sum += dt;
                _count++;
            }
            else
            {
                _sum -= _deltas[_index];
                _deltas[_index] = dt;
                _sum += dt;
                _index++;
                if (_index >= MaxSamples) { _index = 0; }
            }

            _displayTimer += Time.unscaledDeltaTime;
            if (_displayTimer >= _displayInterval && _sum > 0f)
            {
                _fps = _count / _sum;
                _displayTimer = 0f;
            }
        }

        private void OnGUI()
        {
            GUI.Label(new Rect(10f, 250f, 140f, 30f), "FPS:" + _fps.ToString("0", CultureInfo.InvariantCulture), _textStyle);
        }
    }
}