using System.Globalization;
using UnityEngine;

namespace Game.Scripts.Core.Utils
{
    public class FPSCounter : MonoBehaviour
    {
        private float _accum;
        private int _frames;
        private float _timeleft;
        private float _fps;
        private float _updateInterval = 0.1f;
        private GUIStyle _textStyle = new();
        
        private int _valueSum;

        private void OnGUI()
        {
            //Display the fps and round to 2 decimals
            GUI.Label(new Rect(10, 250, 100, 25),"FPS:"+_fps.ToString("0", CultureInfo.InvariantCulture), _textStyle);
        }

        private void Start()
        {
            _textStyle.fontStyle = FontStyle.Bold;
            _textStyle.fontSize = 20;
            _textStyle.normal.textColor = Color.white;
            _timeleft = _updateInterval;
        }

        private void FPSCounterBehaviour()
        {
            _timeleft -= Time.deltaTime;
            _accum += Time.timeScale / Time.deltaTime;
            ++_frames;

            if (_timeleft <= 0)
            {
                _fps = (_accum / _frames);
                _timeleft = _updateInterval;
                _accum = 0;
                _frames = 0;
            }
        }
    
        private void Update()
        {
            FPSCounterBehaviour();
        }
    }
}
