using UnityEngine;
using UnityEngine.UI;

namespace SmartProfiler.Runtime
{
    /// <summary>
    /// FPS Canvas'ını günceller. Her frame'de FPS değerini hesaplar ve Text'i günceller.
    /// </summary>
    public class FPSCanvasUpdater : MonoBehaviour
    {
        [SerializeField] private Text _fpsText;
        private float _frameCount = 0f;
        private float _deltaTime = 0f;
        [SerializeField, Min(0.05f)]
        private float _updateInterval = 0.5f; // Her 0.5 saniyede güncelle

        private float _nextAutoFindTime = 0f;

        public void SetFpsText(Text fpsText)
        {
            _fpsText = fpsText;
        }

        private void Awake()
        {
            EnsureTextReference();
        }

        private void OnEnable()
        {
            EnsureTextReference();
        }

        private void Reset()
        {
            EnsureTextReference();
        }

        private void Update()
        {
            if (_fpsText == null)
            {
                if (Time.unscaledTime >= _nextAutoFindTime)
                {
                    _nextAutoFindTime = Time.unscaledTime + 1f;
                    EnsureTextReference();
                }

                if (_fpsText == null)
                {
                    return;
                }
            }

            _frameCount++;
            _deltaTime += Time.deltaTime;

            if (_deltaTime >= _updateInterval)
            {
                float fps = _frameCount / _deltaTime;
                _fpsText.text = $"FPS: {fps:F1}";
                _frameCount = 0f;
                _deltaTime = 0f;
            }
        }

        private void EnsureTextReference()
        {
            if (_fpsText != null)
            {
                return;
            }

            Text[] texts = GetComponentsInChildren<Text>(true);
            for (int i = 0; i < texts.Length; i++)
            {
                if (texts[i] != null && texts[i].gameObject.name == "FPS_Text")
                {
                    _fpsText = texts[i];
                    return;
                }
            }

            if (texts.Length > 0)
            {
                _fpsText = texts[0];
            }
        }
    }
}
