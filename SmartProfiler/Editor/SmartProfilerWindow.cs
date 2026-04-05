using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using SmartProfiler.Runtime;

namespace SmartProfiler.Editor
{
    public class SmartProfilerWindow : EditorWindow
    {
        private const string NoSnapshotOption = "Select snapshot";

        private DataCollector _collector;
        private ChartRenderer _chartRenderer;

        private VisualElement _boxFps;
        private Label _lblFps;
        private VisualElement _boxGc;
        private Label _lblGc;
        private VisualElement _boxDc;
        private Label _lblDc;
        private VisualElement _boxBatch;
        private Label _lblBatch;

        private VisualElement _alertsContainer;

        private TextField _snapshotNameField;
        private PopupField<string> _baselinePopup;
        private PopupField<string> _currentPopup;
        private Label _baselineMetaLabel;
        private Label _currentMetaLabel;
        private VisualElement _snapshotEmptyState;
        private VisualElement _snapshotComparisonContainer;
        private List<ProfilerSnapshot> _snapshots = new List<ProfilerSnapshot>();

        private double _lastAnalysisTime;

        [MenuItem("Smart Profiler/Open Profiler", priority = 100)]
        public static void ShowWindow()
        {
            var wnd = GetWindow<SmartProfilerWindow>();
            wnd.titleContent = new GUIContent("Smart Profiler");
            wnd.minSize = new Vector2(700, 540);
        }

        private void OnEnable()
        {
            EditorApplication.update += OnEditorUpdate;
            ConnectCollector();
        }

        private void OnDisable()
        {
            EditorApplication.update -= OnEditorUpdate;
            DisconnectCollector();
        }

        public void CreateGUI()
        {
            var root = rootVisualElement;
            root.Clear();

            var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/SmartProfiler/Editor/ProfilerUI.uxml");
            if (visualTree != null)
            {
                visualTree.CloneTree(root);
            }

            var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/SmartProfiler/Editor/ProfilerUI.uss");
            if (styleSheet != null)
            {
                root.styleSheets.Add(styleSheet);
            }

            _boxFps = root.Q<VisualElement>("box-fps");
            _lblFps = root.Q<Label>("lbl-fps");
            _boxGc = root.Q<VisualElement>("box-gc");
            _lblGc = root.Q<Label>("lbl-gc");
            _boxDc = root.Q<VisualElement>("box-dc");
            _lblDc = root.Q<Label>("lbl-dc");
            _boxBatch = root.Q<VisualElement>("box-batch");
            _lblBatch = root.Q<Label>("lbl-batch");
            _alertsContainer = root.Q<VisualElement>("alerts-container");

            _snapshotNameField = root.Q<TextField>("txt-snapshot-name");
            _baselineMetaLabel = root.Q<Label>("lbl-baseline-meta");
            _currentMetaLabel = root.Q<Label>("lbl-current-meta");
            _snapshotEmptyState = root.Q<VisualElement>("snapshot-empty-state");
            _snapshotComparisonContainer = root.Q<VisualElement>("snapshot-comparison-container");

            CreateSnapshotPopups(root);
            BindSnapshotControls(root);
            BindChartSettings(root);

            var chartContainer = root.Q<IMGUIContainer>("chart-container");
            if (chartContainer != null && _collector != null && _chartRenderer == null)
            {
                _chartRenderer = new ChartRenderer(chartContainer, _collector);
            }

            SyncChartSettings(root);
            ReloadSnapshotsUI();
        }

        private void ConnectCollector()
        {
            if (_collector != null)
            {
                return;
            }

#pragma warning disable CS0618
            _collector = FindObjectOfType<DataCollector>();
#pragma warning restore CS0618

            if (_collector == null)
            {
                var go = new GameObject("SmartProfiler_Collector", typeof(DataCollector));
                go.hideFlags = HideFlags.HideAndDontSave;
                _collector = go.GetComponent<DataCollector>();
            }

            _collector.OnFrameRecorded += HandleFrameRecorded;

            var chartContainer = rootVisualElement?.Q<IMGUIContainer>("chart-container");
            if (chartContainer != null && _chartRenderer == null)
            {
                _chartRenderer = new ChartRenderer(chartContainer, _collector);
                SyncChartSettings(rootVisualElement);
            }
        }

        private void DisconnectCollector()
        {
            if (_collector != null)
            {
                _collector.OnFrameRecorded -= HandleFrameRecorded;
                _collector = null;
            }
        }

        private void CreateSnapshotPopups(VisualElement root)
        {
            var choices = new List<string> { NoSnapshotOption };
            _baselinePopup = new PopupField<string>("", choices, 0);
            _currentPopup = new PopupField<string>("", choices, 0);

            var baselineHost = root.Q<VisualElement>("baseline-popup-host");
            baselineHost?.Add(_baselinePopup);

            var currentHost = root.Q<VisualElement>("current-popup-host");
            currentHost?.Add(_currentPopup);
        }

        private void BindSnapshotControls(VisualElement root)
        {
            var btnSaveBaseline = root.Q<Button>("btn-save-baseline");
            if (btnSaveBaseline != null)
            {
                btnSaveBaseline.clicked += () => SaveSnapshot(true);
            }

            var btnSaveCurrent = root.Q<Button>("btn-save-current");
            if (btnSaveCurrent != null)
            {
                btnSaveCurrent.clicked += () => SaveSnapshot(false);
            }

            if (_baselinePopup != null)
            {
                _baselinePopup.RegisterValueChangedCallback(_ => RefreshSnapshotComparison());
            }

            if (_currentPopup != null)
            {
                _currentPopup.RegisterValueChangedCallback(_ => RefreshSnapshotComparison());
            }
        }

        private void BindChartSettings(VisualElement root)
        {
            var togAuto = root.Q<Toggle>("tog-autoscale");
            if (togAuto != null)
            {
                togAuto.RegisterValueChangedCallback(evt =>
                {
                    if (_chartRenderer != null)
                    {
                        _chartRenderer.AutoScale = evt.newValue;
                    }
                });
            }
        }

        private void SyncChartSettings(VisualElement root)
        {
            if (_chartRenderer == null || root == null)
            {
                return;
            }

            var togAuto = root.Q<Toggle>("tog-autoscale");
            if (togAuto != null)
            {
                _chartRenderer.AutoScale = togAuto.value;
            }
        }

        private void OnEditorUpdate()
        {
            if (_collector == null)
            {
                ConnectCollector();
            }
        }

        private void HandleFrameRecorded(FrameSample sample)
        {
            UpdateTopBar(sample);
        }

        private void UpdateTopBar(FrameSample sample)
        {
            if (_lblFps == null)
            {
                return;
            }

            var chartContainer = rootVisualElement?.Q<IMGUIContainer>("chart-container");
            chartContainer?.MarkDirtyRepaint();

            if (EditorApplication.timeSinceStartup - _lastAnalysisTime > 1.0d)
            {
                _lastAnalysisTime = EditorApplication.timeSinceStartup;
                RefreshAlerts();
            }

            float fps = sample.FrameTimeMs > 0 ? 1000f / sample.FrameTimeMs : 0f;
            _lblFps.text = sample.FrameTimeMs.ToString("F1") + " ms / " + Mathf.RoundToInt(fps) + " fps";
            SetState(_boxFps, sample.FrameTimeMs <= 16.7f ? "state-good" : (sample.FrameTimeMs <= 33.4f ? "state-warn" : "state-bad"));

            _lblGc.text = FormatBytes(sample.GcAllocBytes);
            SetState(_boxGc, sample.GcAllocBytes == 0 ? "state-good" : "state-bad");

            _lblDc.text = sample.DrawCalls.ToString();
            SetState(_boxDc, sample.DrawCalls <= 100 ? "state-good" : (sample.DrawCalls <= 500 ? "state-warn" : "state-bad"));

            float batchRatio = sample.DrawCalls > 0 ? (float)sample.Batches / sample.DrawCalls : 1f;
            int savedPercent = Mathf.RoundToInt((1f - Mathf.Clamp01(batchRatio)) * 100f);
            _lblBatch.text = savedPercent + "% saved";
            SetState(_boxBatch, savedPercent >= 50 ? "state-good" : (savedPercent >= 20 ? "state-warn" : "state-bad"));
        }

        private void RefreshAlerts()
        {
            if (_alertsContainer == null || _collector == null)
            {
                return;
            }

            var samples = _collector.GetLastN(DataCollector.Capacity);
            var alerts = AnalyzerEngine.Analyze(samples);

            _alertsContainer.Clear();

            if (alerts.Count == 0)
            {
                _alertsContainer.Add(new Label("System looks healthy. No critical alerts right now.")
                {
                    style =
                    {
                        color = new Color(1f, 1f, 1f, 0.5f),
                        unityFontStyleAndWeight = FontStyle.Italic,
                        paddingLeft = 4
                    }
                });
                return;
            }

            for (int i = 0; i < alerts.Count; i++)
            {
                var alert = alerts[i];
                var el = new VisualElement();
                el.AddToClassList("alert-item");

                string styleClass = "alert-item-info";
                if (alert.Level == AlertLevel.Critical)
                {
                    styleClass = "alert-item-critical";
                }
                else if (alert.Level == AlertLevel.Warning)
                {
                    styleClass = "alert-item-warn";
                }

                el.AddToClassList(styleClass);

                el.Add(new Label(alert.Title)
                {
                    name = "title",
                    style =
                    {
                        fontSize = 13,
                        unityFontStyleAndWeight = FontStyle.Bold,
                        color = Color.white,
                        marginBottom = 2
                    }
                });

                var msgLbl = new Label(alert.Message)
                {
                    name = "msg",
                    style =
                    {
                        fontSize = 11,
                        color = new Color(1f, 1f, 1f, 0.7f),
                        whiteSpace = WhiteSpace.Normal
                    }
                };

                el.Add(msgLbl);
                _alertsContainer.Add(el);
            }
        }

        private void SaveSnapshot(bool selectAsBaseline)
        {
            if (_collector == null)
            {
                return;
            }

            var samples = _collector.GetLastN(DataCollector.Capacity);
            if (samples == null || samples.Length == 0)
            {
                ShowNotification(new GUIContent("Wait for a few frames before saving a snapshot."));
                return;
            }

            string displayName = _snapshotNameField != null ? _snapshotNameField.value : string.Empty;
            if (string.IsNullOrWhiteSpace(displayName))
            {
                displayName = selectAsBaseline ? "baseline" : "current";
                if (_snapshotNameField != null)
                {
                    _snapshotNameField.value = displayName;
                }
            }

            var snapshot = SnapshotComparison.CreateSnapshot(displayName, samples);
            SnapshotComparison.SaveSnapshot(snapshot);

            ReloadSnapshotsUI(snapshot.FileName, selectAsBaseline);
            ShowNotification(new GUIContent("Snapshot saved: " + snapshot.DisplayName));
        }

        private void ReloadSnapshotsUI(string preferredFileName = null, bool preferredForBaseline = false)
        {
            _snapshots = SnapshotComparison.LoadAllSnapshots();
            var choices = new List<string> { NoSnapshotOption };

            for (int i = 0; i < _snapshots.Count; i++)
            {
                choices.Add(_snapshots[i].FileName);
            }

            if (_baselinePopup != null)
            {
                _baselinePopup.choices = choices;
            }

            if (_currentPopup != null)
            {
                _currentPopup.choices = choices;
            }

            if (!string.IsNullOrEmpty(preferredFileName))
            {
                if (preferredForBaseline && _baselinePopup != null)
                {
                    _baselinePopup.value = preferredFileName;
                }
                else if (!preferredForBaseline && _currentPopup != null)
                {
                    _currentPopup.value = preferredFileName;
                }
            }
            else
            {
                EnsureSnapshotSelection(_baselinePopup, 1);
                EnsureSnapshotSelection(_currentPopup, _snapshots.Count > 1 ? 2 : 1);
            }

            RefreshSnapshotComparison();
        }

        private void EnsureSnapshotSelection(PopupField<string> popup, int fallbackIndex)
        {
            if (popup == null || popup.choices == null || popup.choices.Count == 0)
            {
                return;
            }

            if (!popup.choices.Contains(popup.value))
            {
                int index = Mathf.Clamp(fallbackIndex, 0, popup.choices.Count - 1);
                popup.value = popup.choices[index];
            }
        }

        private void RefreshSnapshotComparison()
        {
            var baseline = FindSnapshot(_baselinePopup != null ? _baselinePopup.value : null);
            var current = FindSnapshot(_currentPopup != null ? _currentPopup.value : null);

            UpdateSnapshotMeta(_baselineMetaLabel, baseline);
            UpdateSnapshotMeta(_currentMetaLabel, current);

            if (_snapshotComparisonContainer == null)
            {
                return;
            }

            _snapshotComparisonContainer.Clear();

            bool hasComparison = baseline != null && current != null && baseline.FileName != current.FileName;
            if (_snapshotEmptyState != null)
            {
                _snapshotEmptyState.style.display = hasComparison ? DisplayStyle.None : DisplayStyle.Flex;
            }

            if (!hasComparison)
            {
                return;
            }

            var rows = SnapshotComparison.BuildComparison(baseline, current);
            for (int i = 0; i < rows.Count; i++)
            {
                _snapshotComparisonContainer.Add(CreateComparisonRow(rows[i]));
            }
        }

        private void UpdateSnapshotMeta(Label targetLabel, ProfilerSnapshot snapshot)
        {
            if (targetLabel == null)
            {
                return;
            }

            if (snapshot == null)
            {
                targetLabel.text = "No snapshot selected";
                return;
            }

            string dateText = snapshot.CreatedAtIsoUtc;
            DateTime parsedDate;
            if (!string.IsNullOrEmpty(snapshot.CreatedAtIsoUtc) && DateTime.TryParse(snapshot.CreatedAtIsoUtc, out parsedDate))
            {
                dateText = parsedDate.ToLocalTime().ToString("g");
            }

            targetLabel.text = dateText + "  -  " + snapshot.SampleCount + " frames";
        }

        private ProfilerSnapshot FindSnapshot(string fileName)
        {
            if (string.IsNullOrEmpty(fileName) || fileName == NoSnapshotOption)
            {
                return null;
            }

            for (int i = 0; i < _snapshots.Count; i++)
            {
                if (_snapshots[i].FileName == fileName)
                {
                    return _snapshots[i];
                }
            }

            return null;
        }

        private VisualElement CreateComparisonRow(SnapshotMetricDelta row)
        {
            var el = new VisualElement();
            el.AddToClassList("comparison-row");

            var label = new Label(row.Label);
            label.AddToClassList("comparison-label");
            el.Add(label);

            var baselineValue = new Label(row.BaselineValue);
            baselineValue.AddToClassList("comparison-value");
            el.Add(baselineValue);

            var currentValue = new Label(row.CurrentValue);
            currentValue.AddToClassList("comparison-value");
            el.Add(currentValue);

            var delta = new Label(row.DeltaText);
            delta.AddToClassList("comparison-delta");
            AddTrendClass(delta, row.Trend);
            el.Add(delta);

            var status = new Label(GetTrendIcon(row.Trend));
            status.AddToClassList("comparison-status");
            AddTrendClass(status, row.Trend);
            el.Add(status);

            return el;
        }

        private void AddTrendClass(VisualElement element, SnapshotTrend trend)
        {
            if (element == null)
            {
                return;
            }

            element.RemoveFromClassList("trend-good");
            element.RemoveFromClassList("trend-bad");
            element.RemoveFromClassList("trend-neutral");

            switch (trend)
            {
                case SnapshotTrend.Improved:
                    element.AddToClassList("trend-good");
                    break;
                case SnapshotTrend.Regressed:
                    element.AddToClassList("trend-bad");
                    break;
                default:
                    element.AddToClassList("trend-neutral");
                    break;
            }
        }

        private void SetState(VisualElement box, string stateClass)
        {
            if (box == null)
            {
                return;
            }

            box.RemoveFromClassList("state-good");
            box.RemoveFromClassList("state-warn");
            box.RemoveFromClassList("state-bad");
            box.AddToClassList(stateClass);
        }

        private string GetTrendIcon(SnapshotTrend trend)
        {
            switch (trend)
            {
                case SnapshotTrend.Improved:
                    return "OK";
                case SnapshotTrend.Regressed:
                    return "!!";
                default:
                    return "-";
            }
        }

        private string FormatBytes(long bytes)
        {
            if (bytes == 0)
            {
                return "0 B";
            }

            if (bytes < 1024)
            {
                return bytes + " B";
            }

            if (bytes < 1048576)
            {
                return (bytes / 1024f).ToString("F1") + " KB";
            }

            return (bytes / 1048576f).ToString("F1") + " MB";
        }
    }
}
