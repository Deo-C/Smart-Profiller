using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using SmartProfiler.Runtime;

namespace SmartProfiler.Editor
{
    public class SmartSceneOrganizerWindow : EditorWindow
    {
        private SceneOrganizerReport _report;
        private VisualElement _metricContainer;
        private VisualElement _groupContainer;
        private Label _titleLabel;
        private Label _sceneSummaryLabel;
        private Label _healthLabel;
        private Label _groupTitleLabel;
        private Label _groupSubtitleLabel;
        private PopupField<string> _languagePopup;

        [MenuItem("Smart Profiler/Scene Organizer", priority = 110)]
        public static void ShowWindow()
        {
            SmartSceneOrganizerWindow window = GetWindow<SmartSceneOrganizerWindow>();
            window.titleContent = new GUIContent(SmartProfilerLocalization.Get("scene.window.title"));
            window.minSize = new Vector2(720f, 480f);
        }

        private void OnEnable()
        {
            EditorApplication.hierarchyChanged += HandleHierarchyChanged;
            Selection.selectionChanged += HandleSelectionChanged;
            SmartProfilerLocalization.LanguageChanged += HandleLanguageChanged;
            BuildUI();
            RefreshReport();
        }

        private void OnDisable()
        {
            EditorApplication.hierarchyChanged -= HandleHierarchyChanged;
            Selection.selectionChanged -= HandleSelectionChanged;
            SmartProfilerLocalization.LanguageChanged -= HandleLanguageChanged;
        }

        private void HandleHierarchyChanged()
        {
            RefreshReport();
        }

        private void HandleSelectionChanged()
        {
            Repaint();
        }

        private void HandleLanguageChanged()
        {
            UpdateStaticTexts();
            RefreshReport();
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

            _titleLabel = new Label();
            _titleLabel.style.fontSize = 14;
            _titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            _titleLabel.style.color = Color.white;
            header.Add(_titleLabel);

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
            refreshButton.name = "refresh-button";
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
            _healthLabel.style.marginRight = 10;
            toolbar.Add(_healthLabel);

            _languagePopup = new PopupField<string>(SmartProfilerLocalization.Get("language.label"), BuildLanguageChoices(), (int)SmartProfilerLocalization.CurrentLanguage);
            _languagePopup.RegisterValueChangedCallback(_ =>
            {
                int selectedIndex = Mathf.Max(0, _languagePopup.choices.IndexOf(_languagePopup.value));
                SmartProfilerLocalization.CurrentLanguage = (SmartProfilerLanguage)Mathf.Clamp(selectedIndex, 0, 1);
            });
            toolbar.Add(_languagePopup);

            _metricContainer = new VisualElement();
            _metricContainer.style.flexDirection = FlexDirection.Row;
            _metricContainer.style.flexWrap = Wrap.Wrap;
            _metricContainer.style.marginBottom = 12;
            _metricContainer.style.flexShrink = 0f;
            page.Add(_metricContainer);

            var groupSection = new VisualElement();
            page.Add(groupSection);

            _groupTitleLabel = new Label();
            _groupTitleLabel.style.fontSize = 12;
            _groupTitleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            _groupTitleLabel.style.color = Color.white;
            _groupTitleLabel.style.marginBottom = 6;
            groupSection.Add(_groupTitleLabel);

            _groupSubtitleLabel = new Label();
            _groupSubtitleLabel.style.color = new Color(1f, 1f, 1f, 0.55f);
            _groupSubtitleLabel.style.marginBottom = 8;
            groupSection.Add(_groupSubtitleLabel);

            _groupContainer = new VisualElement();
            groupSection.Add(_groupContainer);

            UpdateStaticTexts();
        }

        private void UpdateStaticTexts()
        {
            titleContent = new GUIContent(SmartProfilerLocalization.Get("scene.window.title"));

            if (_titleLabel != null)
            {
                _titleLabel.text = SmartProfilerLocalization.Get("scene.page.title");
            }

            if (_groupTitleLabel != null)
            {
                _groupTitleLabel.text = SmartProfilerLocalization.Get("scene.groups.title");
            }

            if (_groupSubtitleLabel != null)
            {
                _groupSubtitleLabel.text = SmartProfilerLocalization.Get("scene.groups.subtitle");
            }

            Button refreshButton = rootVisualElement?.Q<Button>("refresh-button");
            if (refreshButton != null)
            {
                refreshButton.text = SmartProfilerLocalization.Get("scene.toolbar.refresh");
            }

            if (_languagePopup != null)
            {
                _languagePopup.label = SmartProfilerLocalization.Get("language.label");
                _languagePopup.choices = BuildLanguageChoices();
                _languagePopup.value = _languagePopup.choices[(int)SmartProfilerLocalization.CurrentLanguage];
            }
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

            _sceneSummaryLabel.text = SmartProfilerLocalization.Format("scene.summary", _report.SceneName, _report.TotalObjects, _report.ActiveObjects);

            SceneHealthLevel overallLevel = ResolveOverallLevel(_report);
            ApplyBadgeStyle(_healthLabel, overallLevel);
            _healthLabel.text = SmartProfilerLocalization.Format("scene.health", GetHealthDisplayName(overallLevel));

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

            var counts = new Label(SmartProfilerLocalization.Format("scene.group.row", group.ObjectCount, group.ActiveCount, group.TriangleCount.ToString("N0")));
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
            if (groupName == SmartProfilerLocalization.Get("scene.group.lighting"))
            {
                return new Color(1f, 0.78f, 0.26f);
            }

            if (groupName == SmartProfilerLocalization.Get("scene.group.geometry"))
            {
                return new Color(0.23f, 0.63f, 0.94f);
            }

            if (groupName == SmartProfilerLocalization.Get("scene.group.gameplay"))
            {
                return new Color(0.38f, 0.8f, 0.5f);
            }

            if (groupName == SmartProfilerLocalization.Get("scene.group.ui"))
            {
                return new Color(0.82f, 0.5f, 0.94f);
            }

            if (groupName == SmartProfilerLocalization.Get("scene.group.audio"))
            {
                return new Color(0.98f, 0.45f, 0.45f);
            }

            if (groupName == SmartProfilerLocalization.Get("scene.group.vfx"))
            {
                return new Color(0.95f, 0.5f, 0.18f);
            }

            if (groupName == SmartProfilerLocalization.Get("scene.group.cameras"))
            {
                return new Color(0.42f, 0.86f, 0.88f);
            }

            return new Color(0.65f, 0.65f, 0.7f);
        }

        private string GetHealthDisplayName(SceneHealthLevel level)
        {
            switch (level)
            {
                case SceneHealthLevel.Warning:
                    return SmartProfilerLocalization.Get("scene.health.warning");
                case SceneHealthLevel.Critical:
                    return SmartProfilerLocalization.Get("scene.health.critical");
                default:
                    return SmartProfilerLocalization.Get("scene.health.good");
            }
        }

        private System.Collections.Generic.List<string> BuildLanguageChoices()
        {
            return new System.Collections.Generic.List<string>
            {
                SmartProfilerLocalization.GetLanguageDisplayName(SmartProfilerLanguage.English),
                SmartProfilerLocalization.GetLanguageDisplayName(SmartProfilerLanguage.Turkish)
            };
        }
    }
}
