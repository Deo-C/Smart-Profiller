using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SmartProfiler.Runtime
{
    /// <summary>
    /// FPS Canvas'ını oluşturmaktan sorumlu utility class.
    /// </summary>
    public static class FPSCanvasCreator
    {
        public static GameObject CreateFpsCanvas()
        {
            Scene activeScene = SceneManager.GetActiveScene();
            if (!activeScene.IsValid())
            {
                return null;
            }

            // Canvas GameObject oluştur
            GameObject canvasGO = new GameObject("FPS_Canvas");
            SceneManager.MoveGameObjectToScene(canvasGO, activeScene);

            Canvas canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true;
            canvas.sortingOrder = 10000;

            CanvasScaler canvasScaler = canvasGO.AddComponent<CanvasScaler>();
            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasScaler.referenceResolution = new Vector2(1920, 1080);

            canvasGO.AddComponent<GraphicRaycaster>();

            // Canvas altında Text (legacy) oluştur
            GameObject textGO = new GameObject("FPS_Text");
            textGO.transform.SetParent(canvasGO.transform, false);

            int uiLayer = LayerMask.NameToLayer("UI");
            if (uiLayer >= 0)
            {
                canvasGO.layer = uiLayer;
                textGO.layer = uiLayer;
            }

            Text fpsText = textGO.AddComponent<Text>();
            fpsText.text = "FPS: 0";
            fpsText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            fpsText.fontSize = 40;
            fpsText.fontStyle = FontStyle.Bold;
            fpsText.alignment = TextAnchor.UpperRight;
            fpsText.color = Color.white;

            // RectTransform ayarla - sağ üst köşe
            RectTransform textRect = textGO.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.one;  // Sağ üst
            textRect.anchorMax = Vector2.one;  // Sağ üst
            textRect.pivot = Vector2.one;      // Sağ üst
            textRect.sizeDelta = new Vector2(400, 100);
            textRect.anchoredPosition = new Vector2(-20f, -20f);

            // FPS Updater script'i ekle
            FPSCanvasUpdater updater = canvasGO.AddComponent<FPSCanvasUpdater>();
            updater.SetFpsText(fpsText);

            return canvasGO;
        }
    }
}
