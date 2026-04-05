using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace SmartProfiler.Editor
{
    public class SmartSceneOrganizerWindow : EditorWindow
    {
        private SceneOrganizerReport _report;
        private VisualElement _metricContainer;
        private VisualElement _groupContainer;
        private Label _sceneSummaryLabel;
        private Label _healthLabel;

        [MenuItem("Smart Profiler/Scene Organizer", priority = 110)]
        public static void ShowWindow()
        {
            var wnd = GetWindow<SmartSceneOrganizerWindow>();
            wnd.titleContent = new GUIContent("Scene Organizer");
            wnd.minSize = new Vector2(720f, 480f);
        }

        private void OnEnable()
        {
            EditorApplication.hierarchyChanged += HandleHierarchyChanged;
            Selection.selectionChanged += HandleSelectionChanged;
            BuildUI();
            RefreshReport();
        }

        private void OnDisable()
        {
            EditorApplication.hierarchyChanged -= HandleHierarchyChanged;
            Selection.selectionChanged -= HandleSelectionChanged;
        }

        private void HandleHierarchyChanged()
        {
            RefreshReport();
        }

        private void HandleSelectionChanged()
        {
            Repaint();
        }

        private void BuildUI()
        {
            VisualElement root = rootVisualElement;
            root.Clear();
            root.style.backgroundColor = new Color(0.11f, 0.11f, 0.12f, 1f);

            var pageScroll = new ScrollView();
            pageScroll.style.flexGrow = 1f;
            pageScroll.style.flexShrink = 1f;
            pageScroll.style.minHeight = 0f;
            root.Add(pageScroll);

            var page = new VisualElement();
            page.style.flexDirection = FlexDirection.Column;
            page.style.flexGrow = 1f;
            page.style.paddingLeft = 10;
            page.style.paddingRight = 10;
            page.style.paddingTop = 10;
            page.style.paddingBottom = 10;
            pageScroll.Add(page);

            var header = new VisualElement();
            header.style.marginBottom = 10;
            page.Add(header);

            var title = new Label("SMART SCENE ORGANIZER");
            title.style.fontSize = 14;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.color = Color.white;
            header.Add(title);

            _sceneSummaryLabel = new Label();
            _sceneSummaryLabel.style.marginTop = 2;
            _sceneSummaryLabel.style.color = new Color(1f, 1f, 1f, 0.65f);
            header.Add(_sceneSummaryLabel);

            var toolbar = new VisualElement();
            toolbar.style.flexDirection = FlexDirection.Row;
            toolbar.style.alignItems = Align.Center;
            toolbar.style.marginBottom = 12;
            page.Add(toolbar);

            var refreshButton = new Button(RefreshReport);
            refreshButton.text = "Refresh";
            refreshButton.style.marginRight = 8;
            toolbar.Add(refreshButton);

            _healthLabel = new Label();
            _healthLabel.style.paddingLeft = 10;
            _healthLabel.style.paddingRight = 10;
            _healthLabel.style.paddingTop = 4;
            _healthLabel.style.paddingBottom = 4;
            _healthLabel.style.borderTopLeftRadius = 4;
            _healthLabel.style.borderTopRightRadius = 4;
            _healthLabel.style.borderBottomLeftRadius = 4;
            _healthLabel.style.borderBottomRightRadius = 4;
            _healthLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            toolbar.Add(_healthLabel);

            _metricContainer = new VisualElement();
            _metricContainer.style.flexDirection = FlexDirection.Row;
            _metricContainer.style.flexWrap = Wrap.Wrap;
            _metricContainer.style.marginBottom = 12;
            _metricContainer.style.flexShrink = 0f;
            page.Add(_metricContainer);

            var groupSection = new VisualElement();
            page.Add(groupSection);

            var groupTitle = new Label("Scene Groups");
            groupTitle.style.fontSize = 12;
            groupTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
            groupTitle.style.color = Color.white;
            groupTitle.style.marginBottom = 6;
            groupSection.Add(groupTitle);

            var groupSubtitle = new Label("Automatic grouping and color coding for the active scene.");
            groupSubtitle.style.color = new Color(1f, 1f, 1f, 0.55f);
            groupSubtitle.style.marginBottom = 8;
            groupSection.Add(groupSubtitle);

            _groupContainer = new VisualElement();
            groupSection.Add(_groupContainer);
        }

        private void RefreshReport()
        {
            _report = SceneOrganizerAnalyzer.AnalyzeActiveScene();
            RenderReport();
        }

        private void RenderReport()
        {
            if (_report == null)
            {
                return;
            }

            _sceneSummaryLabel.text = _report.SceneName + "  |  " + _report.TotalObjects + " objects  |  " + _report.ActiveObjects + " active";

            SceneHealthLevel overallLevel = ResolveOverallLevel(_report);
            ApplyBadgeStyle(_healthLabel, overallLevel);
            _healthLabel.text = "Health: " + overallLevel;

            _metricContainer.Clear();
            for (int i = 0; i < _report.Metrics.Count; i++)
            {
                _metricContainer.Add(CreateMetricCard(_report.Metrics[i]));
            }

            _groupContainer.Clear();
            for (int i = 0; i < _report.Groups.Count; i++)
            {
                _groupContainer.Add(CreateGroupRow(_report.Groups[i], i));
            }
        }

        private VisualElement CreateMetricCard(SceneMetric metric)
        {
            var card = new VisualElement();
            card.style.width = 210;
            card.style.marginRight = 8;
            card.style.marginBottom = 8;
            card.style.paddingLeft = 10;
            card.style.paddingRight = 10;
            card.style.paddingTop = 8;
            card.style.paddingBottom = 8;
            card.style.backgroundColor = new Color(1f, 1f, 1f, 0.04f);
            card.style.borderLeftWidth = 4;
            card.style.borderLeftColor = GetLevelColor(metric.Level);
            card.style.borderTopLeftRadius = 4;
            card.style.borderTopRightRadius = 4;
            card.style.borderBottomLeftRadius = 4;
            card.style.borderBottomRightRadius = 4;

            var label = new Label(metric.Label);
            label.style.fontSize = 10;
            label.style.color = new Color(1f, 1f, 1f, 0.6f);
            card.Add(label);

            var value = new Label(metric.Value);
            value.style.fontSize = 18;
            value.style.unityFontStyleAndWeight = FontStyle.Bold;
            value.style.color = Color.white;
            value.style.marginTop = 2;
            card.Add(value);

            var hint = new Label(metric.Hint);
            hint.style.fontSize = 10;
            hint.style.color = new Color(1f, 1f, 1f, 0.5f);
            hint.style.marginTop = 4;
            hint.style.whiteSpace = WhiteSpace.Normal;
            card.Add(hint);

            return card;
        }

        private VisualElement CreateGroupRow(SceneGroupSummary group, int index)
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.FlexStart;
            row.style.flexWrap = Wrap.Wrap;
            row.style.paddingLeft = 10;
            row.style.paddingRight = 10;
            row.style.paddingTop = 8;
            row.style.paddingBottom = 8;
            row.style.marginBottom = 6;
            row.style.backgroundColor = index % 2 == 0 ? new Color(1f, 1f, 1f, 0.04f) : new Color(1f, 1f, 1f, 0.02f);
            row.style.borderLeftWidth = 4;
            row.style.borderLeftColor = GetGroupColor(group.Name);
            row.style.borderTopLeftRadius = 4;
            row.style.borderTopRightRadius = 4;
            row.style.borderBottomLeftRadius = 4;
            row.style.borderBottomRightRadius = 4;

            var name = new Label(group.Name);
            name.style.flexGrow = 1f;
            name.style.minWidth = 120f;
            name.style.marginRight = 8f;
            name.style.unityFontStyleAndWeight = FontStyle.Bold;
            name.style.color = Color.white;
            row.Add(name);

            var counts = new Label(group.ObjectCount + " objs  |  " + group.ActiveCount + " active  |  " + group.TriangleCount.ToString("N0") + " tris");
            counts.style.flexShrink = 1f;
            counts.style.whiteSpace = WhiteSpace.Normal;
            counts.style.color = new Color(1f, 1f, 1f, 0.7f);
            row.Add(counts);

            return row;
        }

        private SceneHealthLevel ResolveOverallLevel(SceneOrganizerReport report)
        {
            SceneHealthLevel level = SceneHealthLevel.Good;
            for (int i = 0; i < report.Metrics.Count; i++)
            {
                if (report.Metrics[i].Level == SceneHealthLevel.Critical)
                {
                    return SceneHealthLevel.Critical;
                }

                if (report.Metrics[i].Level == SceneHealthLevel.Warning)
                {
                    level = SceneHealthLevel.Warning;
                }
            }

            return level;
        }

        private void ApplyBadgeStyle(Label label, SceneHealthLevel level)
        {
            Color color = GetLevelColor(level);
            label.style.color = Color.white;
            label.style.backgroundColor = new Color(color.r, color.g, color.b, 0.28f);
            label.style.borderLeftColor = color;
        }

        private Color GetLevelColor(SceneHealthLevel level)
        {
            switch (level)
            {
                case SceneHealthLevel.Warning:
                    return new Color(1f, 0.76f, 0.18f);
                case SceneHealthLevel.Critical:
                    return new Color(0.95f, 0.33f, 0.3f);
                default:
                    return new Color(0.36f, 0.78f, 0.43f);
            }
        }

        private Color GetGroupColor(string groupName)
        {
            switch (groupName)
            {
                case "Lighting":
                    return new Color(1f, 0.78f, 0.26f);
                case "Geometry":
                    return new Color(0.23f, 0.63f, 0.94f);
                case "Gameplay":
                    return new Color(0.38f, 0.8f, 0.5f);
                case "UI":
                    return new Color(0.82f, 0.5f, 0.94f);
                case "Audio":
                    return new Color(0.98f, 0.45f, 0.45f);
                case "VFX":
                    return new Color(0.95f, 0.5f, 0.18f);
                case "Cameras":
                    return new Color(0.42f, 0.86f, 0.88f);
                default:
                    return new Color(0.65f, 0.65f, 0.7f);
            }
        }
    }
}
