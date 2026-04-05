using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using SmartProfiler.Runtime;

namespace SmartProfiler.Editor
{
    public class ChartRenderer
    {
        private IMGUIContainer _container;
        private DataCollector _collector;
        private Material _chartMaterial;

        // Custom Colors for Pro Aesthetics
        private readonly Color _bgColor = new Color(0.12f, 0.12f, 0.12f, 1f);
        private readonly Color _gridColor = new Color(1f, 1f, 1f, 0.05f);
        private readonly Color _fpsLineColor = new Color(0.22f, 0.54f, 0.87f, 1f);
        private readonly Color _fpsFillColor = new Color(0.22f, 0.54f, 0.87f, 0.2f);
        private readonly Color _memLineColor = new Color(0.11f, 0.62f, 0.46f, 1f);
        private readonly Color _dcLineColor = new Color(0.93f, 0.62f, 0.15f, 1f);

        // Scaling settings
        public bool AutoScale = true;
        public float FixedMaxFps = 120f;
        public float FixedMaxMemory = 250f;
        public float FixedMaxDrawCalls = 1000f;

        private Vector3[] _vertices = new Vector3[DataCollector.Capacity];

        public ChartRenderer(IMGUIContainer container, DataCollector collector)
        {
            _container = container;
            _collector = collector;
            _container.onGUIHandler += DrawChart;

            var shader = Shader.Find("Hidden/Internal-Colored");
            if (shader != null) _chartMaterial = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
        }

        private void DrawChart()
        {
            if (_chartMaterial == null || _collector == null) return;
            Rect rect = _container.contentRect;
            if (rect.width <= 0 || rect.height <= 0) return;

            var samples = _collector.GetLastN(DataCollector.Capacity);
            if (samples.Length < 2) return;

            Event e = Event.current;
            Rect fpsRect = new Rect(0, 0, rect.width, rect.height * 0.5f);
            Rect memRect = new Rect(0, rect.height * 0.5f, rect.width, rect.height * 0.25f);
            Rect dcRect = new Rect(0, rect.height * 0.75f, rect.width, rect.height * 0.25f);

            if (e.type == EventType.ScrollWheel && !AutoScale)
            {
                if (fpsRect.Contains(e.mousePosition))
                {
                    FixedMaxFps -= e.delta.y * 10f;
                    if (FixedMaxFps < 30f) FixedMaxFps = 30f;
                    e.Use();
                    _container.MarkDirtyRepaint();
                }
                else if (memRect.Contains(e.mousePosition))
                {
                    FixedMaxMemory += e.delta.y * 10f;
                    if (FixedMaxMemory < 10f) FixedMaxMemory = 10f;
                    e.Use();
                    _container.MarkDirtyRepaint();
                }
                else if (dcRect.Contains(e.mousePosition))
                {
                    FixedMaxDrawCalls += e.delta.y * 50f;
                    if (FixedMaxDrawCalls < 10f) FixedMaxDrawCalls = 10f;
                    e.Use();
                    _container.MarkDirtyRepaint();
                }
            }

            if (e.type != EventType.Repaint) return;

            _chartMaterial.SetPass(0);
            GUI.BeginClip(rect);

            DrawBackgroundAndGrid(rect);
            
            DrawFpsChart(fpsRect, samples);
            DrawMemChart(memRect, samples);
            DrawDcChart(dcRect, samples);

            GUI.EndClip();
            
            // Draw hover tooltip
            DrawTooltip(rect, samples);
        }

        private void DrawTooltip(Rect rect, FrameSample[] samples)
        {
            Event e = Event.current;
            if (rect.Contains(e.mousePosition))
            {
                float normalizedX = e.mousePosition.x / rect.width;
                int index = Mathf.Clamp(Mathf.RoundToInt(normalizedX * (samples.Length - 1)), 0, samples.Length - 1);
                var s = samples[index];

                GUIStyle tooltipStyle = new GUIStyle(EditorStyles.helpBox) { richText = true, padding = new RectOffset(8, 8, 8, 8) };
                
                string highestCost = "None";
                float highestMs = 0;
                if (s.PhysicsTimeMs > highestMs) { highestCost = "Physics"; highestMs = s.PhysicsTimeMs; }
                if (s.CameraRenderMs > highestMs) { highestCost = "Camera"; highestMs = s.CameraRenderMs; }
                if (s.AnimatorUpdateMs > highestMs) { highestCost = "Animator"; highestMs = s.AnimatorUpdateMs; }
                if (s.GcCollectMs > highestMs) { highestCost = "GC.Collect"; highestMs = s.GcCollectMs; }

                string tooltipText = $"<b>Frame {s.FrameIndex}</b>\n" +
                                     $"Frame Time: <color=#ff8888>{s.FrameTimeMs:F1} ms</color>\n" +
                                     $"Heap Memory: <color=#88ff88>{s.TotalHeapBytes / 1048576f:F1} MB</color>\n" +
                                     $"Draw Calls: <color=#ffff88>{s.DrawCalls}</color>\n" +
                                     $"Biggest Subsystem: {highestCost} ({highestMs:F1}ms)";

                Vector2 size = tooltipStyle.CalcSize(new GUIContent(tooltipText));
                Rect tooltipRect = new Rect(e.mousePosition.x + 10, e.mousePosition.y + 10, size.x, size.y);
                
                // Prevent going offscreen
                if (tooltipRect.xMax > rect.xMax) tooltipRect.x = rect.xMax - size.x;
                if (tooltipRect.yMax > rect.yMax) tooltipRect.y = e.mousePosition.y - size.y - 10;
                
                GUI.Label(tooltipRect, tooltipText, tooltipStyle);
                
                // Draw vertical indicator line
                _chartMaterial.SetPass(0);
                GL.Begin(GL.LINES);
                GL.Color(new Color(1, 1, 1, 0.4f));
                GL.Vertex3(e.mousePosition.x, 0, 0);
                GL.Vertex3(e.mousePosition.x, rect.height, 0);
                GL.End();
            }
        }

        private void DrawBackgroundAndGrid(Rect rect)
        {
            GL.Begin(GL.QUADS);
            GL.Color(_bgColor);
            GL.Vertex3(0, 0, 0); GL.Vertex3(rect.width, 0, 0); GL.Vertex3(rect.width, rect.height, 0); GL.Vertex3(0, rect.height, 0);
            GL.End();

            GL.Begin(GL.LINES);
            GL.Color(_gridColor);
            for (int i = 1; i < 4; i++)
            {
                float y = rect.height * (i / 4f);
                GL.Vertex3(0, y, 0); GL.Vertex3(rect.width, y, 0);
            }
            GL.End();
        }

        private void DrawFpsChart(Rect rect, FrameSample[] samples)
        {
            float maxVal = FixedMaxFps;
            if (AutoScale)
            {
                maxVal = 90f;
                for (int i = 0; i < samples.Length; i++)
                {
                    float fps = samples[i].FrameTimeMs > 0f ? 1000f / samples[i].FrameTimeMs : 0f;
                    if (fps > maxVal) maxVal = fps;
                }

                maxVal = Mathf.Ceil(maxVal / 30f) * 30f;
            }

            float pad = 2f;
            float h = rect.height - pad * 2f;

            for (int i = 0; i < samples.Length; i++)
            {
                float px = (i / (float)(samples.Length - 1)) * rect.width;
                float fps = samples[i].FrameTimeMs > 0f ? 1000f / samples[i].FrameTimeMs : 0f;
                float normalized = fps / maxVal;
                if (normalized > 1f) normalized = 1f;
                float py = rect.y + rect.height - pad - normalized * h;
                _vertices[i] = new Vector3(px, py, 0);
            }

            // Area Fill
            GL.Begin(GL.QUADS);
            GL.Color(_fpsFillColor);
            for (int i = 0; i < samples.Length - 1; i++)
            {
                GL.Vertex3(_vertices[i].x, rect.y + rect.height, 0);
                GL.Vertex3(_vertices[i].x, _vertices[i].y, 0);
                GL.Vertex3(_vertices[i + 1].x, _vertices[i + 1].y, 0);
                GL.Vertex3(_vertices[i + 1].x, rect.y + rect.height, 0);
            }
            GL.End();

            // Line Trace
            GL.Begin(GL.LINES);
            GL.Color(_fpsLineColor);
            for (int i = 0; i < samples.Length - 1; i++)
            {
                GL.Vertex3(_vertices[i].x, _vertices[i].y, 0);
                GL.Vertex3(_vertices[i + 1].x, _vertices[i + 1].y, 0);
            }
            GL.End();

            DrawHorizontalLine(rect, 30f, maxVal, new Color(1f, 0.5f, 0f, 0.4f), "30 FPS");
            DrawHorizontalLine(rect, 60f, maxVal, new Color(0f, 1f, 0f, 0.4f), "60 FPS");
            DrawHorizontalLine(rect, 90f, maxVal, new Color(0f, 0.8f, 1f, 0.35f), "90 FPS");

            // Low-FPS markers
            GL.Begin(GL.QUADS);
            GL.Color(Color.red);
            float dotSize = 2f;
            for (int i = 0; i < samples.Length; i++)
            {
                float fps = samples[i].FrameTimeMs > 0f ? 1000f / samples[i].FrameTimeMs : 0f;
                if (fps < 30f)
                {
                    float px = _vertices[i].x;
                    float py = _vertices[i].y;
                    GL.Vertex3(px - dotSize, py - dotSize, 0); GL.Vertex3(px + dotSize, py - dotSize, 0);
                    GL.Vertex3(px + dotSize, py + dotSize, 0); GL.Vertex3(px - dotSize, py + dotSize, 0);
                }
            }
            GL.End();

            float currentFps = samples[samples.Length - 1].FrameTimeMs > 0f ? 1000f / samples[samples.Length - 1].FrameTimeMs : 0f;
            GUI.Label(new Rect(5, rect.y + 2, 220, 15), $"FPS: {currentFps:F0} (Max: {maxVal:F0})", new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = new Color(1f, 1f, 1f, 0.5f) } });
        }

        private void DrawMemChart(Rect rect, FrameSample[] samples)
        {
            float maxVal = FixedMaxMemory;
            if (AutoScale)
            {
                maxVal = 10f;
                for (int i = 0; i < samples.Length; i++) 
                {
                    float mb = samples[i].TotalHeapBytes / 1048576f;
                    if (mb > maxVal) maxVal = mb;
                }
                maxVal *= 1.2f;
            }

            float pad = 2f;
            float h = rect.height - pad * 2f;

            for (int i = 0; i < samples.Length; i++)
            {
                float px = (i / (float)(samples.Length - 1)) * rect.width;
                float normalized = (samples[i].TotalHeapBytes / 1048576f) / maxVal;
                if (normalized > 1f) normalized = 1f;
                float py = rect.y + rect.height - pad - normalized * h;
                _vertices[i] = new Vector3(px, py, 0);
            }

            GL.Begin(GL.LINES);
            GL.Color(_memLineColor);
            for (int i = 0; i < samples.Length - 1; i++)
            {
                GL.Vertex3(_vertices[i].x, _vertices[i].y, 0);
                GL.Vertex3(_vertices[i + 1].x, _vertices[i + 1].y, 0);
            }
            GL.End();

            // Visual Divider
            GL.Begin(GL.LINES);
            GL.Color(new Color(1,1,1,0.1f));
            GL.Vertex3(0, rect.y, 0); GL.Vertex3(rect.width, rect.y, 0);
            GL.End();
            
            float currentMb = samples[samples.Length - 1].TotalHeapBytes / 1048576f;
            GUI.Label(new Rect(5, rect.y + 2, 200, 15), $"HEAP MEMORY: {currentMb:F1} MB (Max: {maxVal:F0} MB)", new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = new Color(1,1,1,0.5f) } });
        }

        private void DrawDcChart(Rect rect, FrameSample[] samples)
        {
            float maxVal = FixedMaxDrawCalls;
            if (AutoScale)
            {
                maxVal = 50f;
                for (int i = 0; i < samples.Length; i++) if (samples[i].DrawCalls > maxVal) maxVal = samples[i].DrawCalls;
                maxVal *= 1.2f;
            }

            float pad = 2f;
            float h = rect.height - pad * 2f;

            for (int i = 0; i < samples.Length; i++)
            {
                float px = (i / (float)(samples.Length - 1)) * rect.width;
                float normalized = samples[i].DrawCalls / maxVal;
                if (normalized > 1f) normalized = 1f;
                float py = rect.y + rect.height - pad - normalized * h;
                _vertices[i] = new Vector3(px, py, 0);
            }

            GL.Begin(GL.LINES);
            GL.Color(_dcLineColor);
            for (int i = 0; i < samples.Length - 1; i++)
            {
                GL.Vertex3(_vertices[i].x, _vertices[i].y, 0);
                GL.Vertex3(_vertices[i + 1].x, _vertices[i + 1].y, 0);
            }
            GL.End();
            
            // Visual Divider
            GL.Begin(GL.LINES);
            GL.Color(new Color(1,1,1,0.1f));
            GL.Vertex3(0, rect.y, 0); GL.Vertex3(rect.width, rect.y, 0);
            GL.End();
            
            int currentDc = samples[samples.Length - 1].DrawCalls;
            GUI.Label(new Rect(5, rect.y + 2, 200, 15), $"DRAW CALLS: {currentDc} (Max: {maxVal:F0})", new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = new Color(1,1,1,0.5f) } });
        }

        private void DrawHorizontalLine(Rect rect, float targetVal, float maxVal, Color col, string label)
        {
            float y = rect.y + rect.height - (targetVal / maxVal) * rect.height;
            if (y < rect.y || y > rect.y + rect.height) return;

            GL.Begin(GL.LINES);
            GL.Color(col);
            GL.Vertex3(0, y, 0); GL.Vertex3(rect.width, y, 0);
            GL.End();

            GUI.Label(new Rect(rect.width - 45, y - 15, 45, 15), label, new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = col } });
        }
    }
}
