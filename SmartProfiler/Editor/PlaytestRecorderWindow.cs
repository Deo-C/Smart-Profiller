using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using SmartProfiler.Runtime;

namespace SmartProfiler.Editor
{
    public class PlaytestRecorderWindow : EditorWindow
    {
        private readonly List<PlaytestSessionData> _sessions = new List<PlaytestSessionData>();
        private int _selectedSessionIndex;
        private float _movementDiscSize = 0.2f;
        private Vector2 _scrollPosition;

        private GUIStyle _heroTitleStyle;
        private GUIStyle _heroBodyStyle;
        private GUIStyle _cardStyle;
        private GUIStyle _cardTitleStyle;
        private GUIStyle _mutedLabelStyle;
        private GUIStyle _statValueStyle;
        private GUIStyle _sectionTitleStyle;

        [MenuItem("Smart Profiler/Playtest Recorder", priority = 120)]
        public static void ShowWindow()
        {
            var wnd = GetWindow<PlaytestRecorderWindow>();
            wnd.titleContent = new GUIContent("Playtest Recorder");
            wnd.minSize = new Vector2(720f, 480f);
        }

        private void OnEnable()
        {
            SceneView.duringSceneGui += OnSceneGUI;
            ReloadSessions();
        }

        private void OnDisable()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
        }

        private void OnGUI()
        {
            EnsureStyles();

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, GUIStyle.none, GUI.skin.verticalScrollbar);
            EditorGUILayout.BeginVertical();
            GUILayout.Space(8f);
            DrawHeader();
            DrawToolbar();
            DrawSessionSelection();
            DrawStats();
            DrawLegend();
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndScrollView();

            if (GUI.changed)
            {
                SceneView.RepaintAll();
            }
        }

        private void DrawHeader()
        {
            Color oldColor = GUI.color;
            GUI.color = new Color(0.16f, 0.18f, 0.2f, 1f);
            EditorGUILayout.BeginVertical(_cardStyle);
            GUI.color = oldColor;

            GUILayout.Label("PLAYTEST RECORDER", _heroTitleStyle);
            GUILayout.Label("Record player paths in Play Mode and turn them into a scene heatmap. Use it to spot choke points, loops, and places where players linger.", _heroBodyStyle);

            GUILayout.Space(10f);
            Rect accentRect = GUILayoutUtility.GetRect(1f, 4f, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(accentRect, new Color(0.92f, 0.28f, 0.24f, 0.95f));
            EditorGUILayout.EndVertical();
            GUILayout.Space(10f);
        }

        private void DrawToolbar()
        {
            Color oldColor = GUI.color;
            GUI.color = new Color(0.18f, 0.2f, 0.22f, 1f);
            EditorGUILayout.BeginVertical(_cardStyle);
            GUI.color = oldColor;

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Reload Sessions", GUILayout.Width(130f), GUILayout.Height(24f)))
                {
                    ReloadSessions();
                }

                GUILayout.Space(8f);
                EditorGUI.BeginDisabledGroup(GetSelectedSession() == null);
                if (GUILayout.Button("Delete Session", GUILayout.Width(130f), GUILayout.Height(24f)))
                {
                    DeleteSelectedSession();
                }
                EditorGUI.EndDisabledGroup();

                GUILayout.Space(8f);
                DrawRecordingIndicator();
            }

            EditorGUILayout.EndVertical();
            GUILayout.Space(10f);
        }

        private void DrawRecordingIndicator()
        {
            bool isRecording = PlaytestRecorderRuntime.IsRecording;

            Rect rect = GUILayoutUtility.GetRect(220f, 20f, GUILayout.ExpandWidth(false));
            Rect iconRect = new Rect(rect.x, rect.y + 2f, 16f, 16f);
            Rect textRect = new Rect(rect.x + 22f, rect.y + 1f, rect.width - 22f, rect.height);

            Color outerColor = isRecording ? new Color(0.95f, 0.2f, 0.2f, 0.95f) : new Color(0.6f, 0.6f, 0.6f, 0.65f);
            Color innerColor = isRecording ? new Color(0.35f, 0.02f, 0.02f, 1f) : new Color(0.16f, 0.16f, 0.16f, 1f);

            Handles.BeginGUI();
            Handles.color = outerColor;
            Handles.DrawSolidDisc(iconRect.center, Vector3.forward, 7f);
            Handles.color = innerColor;
            Handles.DrawSolidDisc(iconRect.center, Vector3.forward, 3.5f);
            Handles.EndGUI();

            GUIStyle style = new GUIStyle(EditorStyles.miniBoldLabel);
            style.normal.textColor = isRecording ? new Color(1f, 0.85f, 0.85f, 1f) : new Color(0.8f, 0.8f, 0.8f, 0.9f);
            GUI.Label(textRect, isRecording ? "REC  Recording in Play Mode" : "REC  Waiting for Play Mode", style);
        }

        private void DrawSessionSelection()
        {
            string[] labels = BuildSessionLabels();
            if (labels.Length == 0)
            {
                EditorGUILayout.HelpBox("No playtest sessions found yet. Enter Play Mode and move around to generate data.", MessageType.Info);
                return;
            }

            Color oldColor = GUI.color;
            GUI.color = new Color(0.18f, 0.2f, 0.22f, 1f);
            EditorGUILayout.BeginVertical(_cardStyle);
            GUI.color = oldColor;

            GUILayout.Label("Session Controls", _sectionTitleStyle);
            GUILayout.Label("Pick a run and tune how wide the painted movement cells should appear in Scene view.", _mutedLabelStyle);
            GUILayout.Space(6f);

            _selectedSessionIndex = EditorGUILayout.Popup("Session", Mathf.Clamp(_selectedSessionIndex, 0, labels.Length - 1), labels);
            GUILayout.Space(2f);

            _movementDiscSize = EditorGUILayout.Slider("Movement Size", _movementDiscSize, 0.05f, 1f);
            GUILayout.Space(2f);
            EditorGUILayout.HelpBox("Smaller values show sharper paths. Larger values blend nearby movement into wider hot zones.", MessageType.None);
            EditorGUILayout.EndVertical();
            GUILayout.Space(10f);
        }

        private void DrawStats()
        {
            PlaytestSessionData session = GetSelectedSession();
            if (session == null)
            {
                return;
            }

            Color oldColor = GUI.color;
            GUI.color = new Color(0.18f, 0.2f, 0.22f, 1f);
            EditorGUILayout.BeginVertical(_cardStyle);
            GUI.color = oldColor;

            GUILayout.Label("Session Summary", _sectionTitleStyle);
            GUILayout.Space(6f);

            DrawStatRow("Scene", session.SceneName);
            DrawStatRow("Created", FormatDate(session.CreatedAtIsoUtc));
            DrawStatRow("Duration", session.DurationSeconds.ToString("F1") + "s");
            DrawStatRow("Movement Samples", session.MovementPoints.Count.ToString());

            EditorGUILayout.EndVertical();
            GUILayout.Space(10f);
        }

        private void DrawLegend()
        {
            Color oldColor = GUI.color;
            GUI.color = new Color(0.18f, 0.2f, 0.22f, 1f);
            EditorGUILayout.BeginVertical(_cardStyle);
            GUI.color = oldColor;

            GUILayout.Label("Legend", _sectionTitleStyle);
            GUILayout.Space(6f);

            DrawLegendRow(new Color(0.18f, 0.82f, 0.32f, 0.95f), "Low traffic");
            DrawLegendRow(new Color(0.98f, 0.84f, 0.18f, 0.95f), "Medium traffic");
            DrawLegendRow(new Color(0.92f, 0.22f, 0.18f, 0.95f), "Highest player density");

            EditorGUILayout.EndVertical();
        }

        private void OnSceneGUI(SceneView sceneView)
        {
            PlaytestSessionData session = GetSelectedSession();
            if (session == null)
            {
                return;
            }

            DrawMovementHeat(session);
        }

        private void DrawMovementHeat(PlaytestSessionData session)
        {
            var buckets = BuildBuckets(session.MovementPoints, _movementDiscSize);
            if (buckets.Count == 0)
            {
                return;
            }

            int maxCount = 1;
            for (int i = 0; i < buckets.Count; i++)
            {
                if (buckets[i].Count > maxCount)
                {
                    maxCount = buckets[i].Count;
                }
            }

            buckets.Sort((a, b) => a.Count.CompareTo(b.Count));
            foreach (var bucket in buckets)
            {
                float intensity = Mathf.Clamp01(bucket.Count / (float)maxCount);
                Color col = EvaluateHeatColor(intensity);
                Handles.color = col;
                Handles.DrawSolidDisc(bucket.Center, Vector3.forward, _movementDiscSize + intensity * _movementDiscSize * 2.4f);
            }
        }

        private Color EvaluateHeatColor(float intensity)
        {
            intensity = Mathf.Clamp01(intensity);

            Color green = new Color(0.18f, 0.82f, 0.32f, 0.14f);
            Color yellow = new Color(0.98f, 0.84f, 0.18f, 0.22f);
            Color red = new Color(0.92f, 0.22f, 0.18f, 0.34f);

            if (intensity < 0.5f)
            {
                return Color.Lerp(green, yellow, intensity / 0.5f);
            }

            return Color.Lerp(yellow, red, (intensity - 0.5f) / 0.5f);
        }

        private List<BucketPoint> BuildBuckets(List<PlaytestPoint> points, float cellSize)
        {
            var buckets = new Dictionary<Vector2Int, BucketPoint>();
            if (cellSize <= 0f)
            {
                cellSize = 0.1f;
            }

            for (int i = 0; i < points.Count; i++)
            {
                Vector3 pos = points[i].Position;
                Vector2Int key = new Vector2Int(Mathf.RoundToInt(pos.x / cellSize), Mathf.RoundToInt(pos.y / cellSize));
                BucketPoint bucket;
                if (!buckets.TryGetValue(key, out bucket))
                {
                    bucket = new BucketPoint
                    {
                        Center = new Vector3(key.x * cellSize, key.y * cellSize, 0f),
                        Count = 0
                    };
                }

                bucket.Count++;
                buckets[key] = bucket;
            }

            return new List<BucketPoint>(buckets.Values);
        }

        private void ReloadSessions()
        {
            _sessions.Clear();
            string directory = GetSessionDirectory();
            if (!Directory.Exists(directory))
            {
                Repaint();
                return;
            }

            string[] files = Directory.GetFiles(directory, "*.json");
            for (int i = 0; i < files.Length; i++)
            {
                try
                {
                    string json = File.ReadAllText(files[i]);
                    PlaytestSessionData session = JsonUtility.FromJson<PlaytestSessionData>(json);
                    if (session != null)
                    {
                        _sessions.Add(session);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning("SmartProfiler playtest session load failed: " + files[i] + "\n" + ex.Message);
                }
            }

            _sessions.Sort((a, b) => string.CompareOrdinal(b.CreatedAtIsoUtc, a.CreatedAtIsoUtc));
            _selectedSessionIndex = Mathf.Clamp(_selectedSessionIndex, 0, Mathf.Max(0, _sessions.Count - 1));
            Repaint();
            SceneView.RepaintAll();
        }

        private void DeleteSelectedSession()
        {
            PlaytestSessionData session = GetSelectedSession();
            if (session == null)
            {
                return;
            }

            bool confirmed = EditorUtility.DisplayDialog(
                "Delete Playtest Session",
                "Delete the selected playtest session permanently?\n\n" + session.SceneName + "  |  " + FormatDate(session.CreatedAtIsoUtc),
                "Delete",
                "Cancel");

            if (!confirmed)
            {
                return;
            }

            string filePath = Path.Combine(GetSessionDirectory(), session.SessionId + ".json");
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }

            ReloadSessions();
        }

        private PlaytestSessionData GetSelectedSession()
        {
            if (_sessions.Count == 0)
            {
                return null;
            }

            _selectedSessionIndex = Mathf.Clamp(_selectedSessionIndex, 0, _sessions.Count - 1);
            return _sessions[_selectedSessionIndex];
        }

        private string[] BuildSessionLabels()
        {
            if (_sessions.Count == 0)
            {
                return Array.Empty<string>();
            }

            string[] labels = new string[_sessions.Count];
            for (int i = 0; i < _sessions.Count; i++)
            {
                PlaytestSessionData session = _sessions[i];
                labels[i] = session.SceneName + "  |  " + FormatDate(session.CreatedAtIsoUtc) + "  |  " + session.MovementPoints.Count + " pts";
            }

            return labels;
        }

        private string FormatDate(string isoDate)
        {
            DateTime parsed;
            if (DateTime.TryParse(isoDate, out parsed))
            {
                return parsed.ToLocalTime().ToString("g");
            }

            return isoDate;
        }

        private string GetSessionDirectory()
        {
            return Path.Combine(Directory.GetCurrentDirectory(), "ProjectSettings", "SmartProfilerPlaytests");
        }

        private void DrawStatRow(string label, string value)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label(label, _mutedLabelStyle, GUILayout.Width(120f));
                GUILayout.Label(value, _statValueStyle);
            }
        }

        private void DrawLegendRow(Color color, string label)
        {
            Rect rowRect = GUILayoutUtility.GetRect(1f, 18f, GUILayout.ExpandWidth(true));
            Rect swatchRect = new Rect(rowRect.x, rowRect.y + 3f, 12f, 12f);
            Rect labelRect = new Rect(rowRect.x + 20f, rowRect.y + 1f, rowRect.width - 20f, rowRect.height);
            EditorGUI.DrawRect(swatchRect, color);
            GUI.Label(labelRect, label, _mutedLabelStyle);
        }

        private void EnsureStyles()
        {
            if (_heroTitleStyle != null)
            {
                return;
            }

            _heroTitleStyle = new GUIStyle(EditorStyles.boldLabel);
            _heroTitleStyle.fontSize = 18;
            _heroTitleStyle.normal.textColor = Color.white;

            _heroBodyStyle = new GUIStyle(EditorStyles.wordWrappedLabel);
            _heroBodyStyle.normal.textColor = new Color(1f, 1f, 1f, 0.72f);

            _cardStyle = new GUIStyle("HelpBox");
            _cardStyle.padding = new RectOffset(12, 12, 12, 12);
            _cardStyle.margin = new RectOffset(0, 0, 0, 0);

            _cardTitleStyle = new GUIStyle(EditorStyles.boldLabel);
            _cardTitleStyle.fontSize = 12;
            _cardTitleStyle.normal.textColor = Color.white;

            _sectionTitleStyle = new GUIStyle(_cardTitleStyle);

            _mutedLabelStyle = new GUIStyle(EditorStyles.label);
            _mutedLabelStyle.normal.textColor = new Color(1f, 1f, 1f, 0.7f);

            _statValueStyle = new GUIStyle(EditorStyles.boldLabel);
            _statValueStyle.normal.textColor = Color.white;
        }

        private struct BucketPoint
        {
            public Vector3 Center;
            public int Count;
        }
    }
}
