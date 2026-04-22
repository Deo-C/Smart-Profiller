using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using SmartProfiler.Runtime;

namespace SmartProfiler.Editor
{
    public class SmartProfilerWindow : EditorWindow
    {
        private DataCollector _collector;
        private ChartRenderer _chartRenderer;
        private FrameSample _lastSample;
        private bool _hasLastSample;

        private VisualElement _boxFps;
        private Label _lblFps;
        private VisualElement _boxGc;
        private Label _lblGc;
        private VisualElement _boxDc;
        private Label _lblDc;
        private VisualElement _boxBatch;
        private Label _lblBatch;
        private VisualElement _alertsContainer;

        private PopupField<string> _languagePopup;
        private Button _btnShowFpsCanvas;
        private TextField _snapshotNameField;
        private PopupField<string> _baselinePopup;
        private PopupField<string> _currentPopup;
        private Label _baselineMetaLabel;
        private Label _currentMetaLabel;
        private VisualElement _snapshotEmptyState;
        private Label _snapshotEmptyLabel;
        private VisualElement _snapshotComparisonContainer;
        private readonly List<ProfilerSnapshot> _snapshots = new List<ProfilerSnapshot>();

        private double _lastAnalysisTime;

        private string NoSnapshotOption
        {
            get { return SmartProfilerLocalization.Get("profiler.snapshot.none"); }
        }

        [MenuItem("Smart Profiler/Open Profiler", priority = 100)]
        public static void ShowWindow()
        {
            SmartProfilerWindow window = GetWindow<SmartProfilerWindow>();
            window.titleContent = new GUIContent(SmartProfilerLocalization.Get("profiler.window.title"));
            window.minSize = new Vector2(700f, 540f);
        }

        private void OnEnable()
        {
            EditorApplication.update += OnEditorUpdate;
            SmartProfilerLocalization.LanguageChanged += HandleLanguageChanged;
            ConnectCollector();
            UpdateWindowTitle();
        }

        private void OnDisable()
        {
            EditorApplication.update -= OnEditorUpdate;
            SmartProfilerLocalization.LanguageChanged -= HandleLanguageChanged;
            DisconnectCollector();
        }

        public void CreateGUI()
        {
            VisualElement root = rootVisualElement;
            root.Clear();

            VisualTreeAsset visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/SmartProfiler/Editor/ProfilerUI.uxml");
            if (visualTree != null)
            {
                visualTree.CloneTree(root);
            }

            StyleSheet styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/SmartProfiler/Editor/ProfilerUI.uss");
            if (styleSheet != null)
            {
                root.styleSheets.Add(styleSheet);
            }

            CacheUI(root);
            CreateLanguagePopup(root);
            CreateSnapshotPopups(root);
            BindSnapshotControls(root);
            BindChartSettings(root);

            IMGUIContainer chartContainer = root.Q<IMGUIContainer>("chart-container");
            if (chartContainer != null && _collector != null && _chartRenderer == null)
            {
                _chartRenderer = new ChartRenderer(chartContainer, _collector);
            }

            SyncChartSettings(root);
            ApplyLocalization();
            ReloadSnapshotsUI();

            if (_hasLastSample)
            {
                UpdateTopBar(_lastSample);
            }
        }

        private void CacheUI(VisualElement root)
        {
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
            _snapshotEmptyLabel = root.Q<Label>("snapshot-empty-label");
            _snapshotComparisonContainer = root.Q<VisualElement>("snapshot-comparison-container");
        }

        private void CreateLanguagePopup(VisualElement root)
        {
            List<string> choices = BuildLanguageChoices();
            int currentIndex = (int)SmartProfilerLocalization.CurrentLanguage;
            _languagePopup = new PopupField<string>(SmartProfilerLocalization.Get("language.label"), choices, Mathf.Clamp(currentIndex, 0, choices.Count - 1));
            _languagePopup.style.minWidth = 170f;
            _languagePopup.RegisterValueChangedCallback(_ =>
            {
                int selectedIndex = Mathf.Max(0, _languagePopup.choices.IndexOf(_languagePopup.value));
                SmartProfilerLocalization.CurrentLanguage = (SmartProfilerLanguage)Mathf.Clamp(selectedIndex, 0, 1);
            });

            VisualElement host = root.Q<VisualElement>("language-popup-host");
            host?.Add(_languagePopup);
        }

        private void CreateSnapshotPopups(VisualElement root)
        {
            List<string> choices = new List<string> { NoSnapshotOption };
            _baselinePopup = new PopupField<string>(string.Empty, choices, 0);
            _currentPopup = new PopupField<string>(string.Empty, choices, 0);

            root.Q<VisualElement>("baseline-popup-host")?.Add(_baselinePopup);
            root.Q<VisualElement>("current-popup-host")?.Add(_currentPopup);
        }

        private void BindSnapshotControls(VisualElement root)
        {
            Button btnSaveBaseline = root.Q<Button>("btn-save-baseline");
            if (btnSaveBaseline != null)
            {
                btnSaveBaseline.clicked += () => SaveSnapshot(true);
            }

            Button btnSaveCurrent = root.Q<Button>("btn-save-current");
            if (btnSaveCurrent != null)
            {
                btnSaveCurrent.clicked += () => SaveSnapshot(false);
            }

            Button btnExportCsv = root.Q<Button>("btn-export-csv");
            if (btnExportCsv != null)
            {
                btnExportCsv.clicked += () => ExportReport(false);
            }

            Button btnExportMarkdown = root.Q<Button>("btn-export-md");
            if (btnExportMarkdown != null)
            {
                btnExportMarkdown.clicked += () => ExportReport(true);
            }

            Button btnShowFpsCanvas = root.Q<Button>("btn-show-fps-canvas");
            if (btnShowFpsCanvas != null)
            {
                _btnShowFpsCanvas = btnShowFpsCanvas;
                btnShowFpsCanvas.clicked += ShowFpsCanvas;
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
            Toggle autoScaleToggle = root.Q<Toggle>("tog-autoscale");
            if (autoScaleToggle != null)
            {
                autoScaleToggle.RegisterValueChangedCallback(evt =>
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

            Toggle autoScaleToggle = root.Q<Toggle>("tog-autoscale");
            if (autoScaleToggle != null)
            {
                _chartRenderer.AutoScale = autoScaleToggle.value;
            }
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
                GameObject go = new GameObject("SmartProfiler_Collector", typeof(DataCollector));
                go.hideFlags = HideFlags.HideAndDontSave;
                _collector = go.GetComponent<DataCollector>();
            }

            _collector.OnFrameRecorded += HandleFrameRecorded;

            IMGUIContainer chartContainer = rootVisualElement?.Q<IMGUIContainer>("chart-container");
            if (chartContainer != null && _chartRenderer == null)
            {
                _chartRenderer = new ChartRenderer(chartContainer, _collector);
                SyncChartSettings(rootVisualElement);
            }
        }

        private void DisconnectCollector()
        {
            if (_collector == null)
            {
                return;
            }

            _collector.OnFrameRecorded -= HandleFrameRecorded;
            _collector = null;
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
            _lastSample = sample;
            _hasLastSample = true;
            UpdateTopBar(sample);
        }

        private void HandleLanguageChanged()
        {
            ApplyLocalization();
            ReloadSnapshotsUI();
            RefreshAlerts();

            if (_hasLastSample)
            {
                UpdateTopBar(_lastSample);
            }
            else
            {
                RefreshSnapshotComparison();
            }

            Repaint();
        }

        private void ApplyLocalization()
        {
            UpdateWindowTitle();

            VisualElement root = rootVisualElement;
            if (root == null)
            {
                return;
            }

            SetLabelText(root, "title-fps", SmartProfilerLocalization.Get("profiler.stat.frame"));
            SetLabelText(root, "title-gc", SmartProfilerLocalization.Get("profiler.stat.gc"));
            SetLabelText(root, "title-dc", SmartProfilerLocalization.Get("profiler.stat.drawcalls"));
            SetLabelText(root, "title-batch", SmartProfilerLocalization.Get("profiler.stat.batching"));
            SetLabelText(root, "snapshot-title", SmartProfilerLocalization.Get("profiler.snapshot.title"));
            SetLabelText(root, "snapshot-subtitle", SmartProfilerLocalization.Get("profiler.snapshot.subtitle"));
            SetLabelText(root, "baseline-title", SmartProfilerLocalization.Get("profiler.snapshot.baseline"));
            SetLabelText(root, "current-title", SmartProfilerLocalization.Get("profiler.snapshot.current"));

            if (_snapshotEmptyLabel != null)
            {
                _snapshotEmptyLabel.text = SmartProfilerLocalization.Get("profiler.snapshot.empty");
            }

            if (_snapshotNameField != null)
            {
                _snapshotNameField.label = SmartProfilerLocalization.Get("profiler.snapshot.field");
            }

            Button btnSaveBaseline = root.Q<Button>("btn-save-baseline");
            if (btnSaveBaseline != null)
            {
                btnSaveBaseline.text = SmartProfilerLocalization.Get("profiler.snapshot.saveBaseline");
            }

            Button btnSaveCurrent = root.Q<Button>("btn-save-current");
            if (btnSaveCurrent != null)
            {
                btnSaveCurrent.text = SmartProfilerLocalization.Get("profiler.snapshot.saveCurrent");
            }

            Button btnExportCsv = root.Q<Button>("btn-export-csv");
            if (btnExportCsv != null)
            {
                btnExportCsv.text = SmartProfilerLocalization.Get("profiler.export.csv");
            }

            Button btnExportMarkdown = root.Q<Button>("btn-export-md");
            if (btnExportMarkdown != null)
            {
                btnExportMarkdown.text = SmartProfilerLocalization.Get("profiler.export.md");
            }

            Toggle autoScaleToggle = root.Q<Toggle>("tog-autoscale");
            if (autoScaleToggle != null)
            {
                autoScaleToggle.label = SmartProfilerLocalization.Get("profiler.toggle.autoscale");
                autoScaleToggle.tooltip = SmartProfilerLocalization.Get("profiler.toggle.autoscale.tooltip");
            }

            if (_btnShowFpsCanvas != null)
            {
                _btnShowFpsCanvas.text = SmartProfilerLocalization.Get("profiler.fpsCanvas.show");
            }

            UpdateLanguagePopupChoices();
            UpdateSnapshotMeta(_baselineMetaLabel, FindSnapshot(_baselinePopup != null ? _baselinePopup.value : null));
            UpdateSnapshotMeta(_currentMetaLabel, FindSnapshot(_currentPopup != null ? _currentPopup.value : null));
        }

        private void UpdateWindowTitle()
        {
            titleContent = new GUIContent(SmartProfilerLocalization.Get("profiler.window.title"));
        }

        private void UpdateTopBar(FrameSample sample)
        {
            if (_lblFps == null)
            {
                return;
            }

            rootVisualElement?.Q<IMGUIContainer>("chart-container")?.MarkDirtyRepaint();

            if (EditorApplication.timeSinceStartup - _lastAnalysisTime > 1.0d)
            {
                _lastAnalysisTime = EditorApplication.timeSinceStartup;
                RefreshAlerts();
            }

            float fps = sample.FrameTimeMs > 0f ? 1000f / sample.FrameTimeMs : 0f;
            _lblFps.text = sample.FrameTimeMs.ToString("F1") + " ms / " + Mathf.RoundToInt(fps) + " fps";
            SetState(_boxFps, sample.FrameTimeMs <= 16.7f ? "state-good" : (sample.FrameTimeMs <= 33.4f ? "state-warn" : "state-bad"));

            _lblGc.text = FormatBytes(sample.GcAllocBytes);
            SetState(_boxGc, sample.GcAllocBytes == 0 ? "state-good" : "state-bad");

            _lblDc.text = sample.DrawCalls.ToString();
            SetState(_boxDc, sample.DrawCalls <= 100 ? "state-good" : (sample.DrawCalls <= 500 ? "state-warn" : "state-bad"));

            float batchRatio = sample.DrawCalls > 0 ? (float)sample.Batches / sample.DrawCalls : 1f;
            int savedPercent = Mathf.RoundToInt((1f - Mathf.Clamp01(batchRatio)) * 100f);
            _lblBatch.text = SmartProfilerLocalization.Format("profiler.batch.saved", savedPercent);
            SetState(_boxBatch, savedPercent >= 50 ? "state-good" : (savedPercent >= 20 ? "state-warn" : "state-bad"));
        }

        private void RefreshAlerts()
        {
            if (_alertsContainer == null || _collector == null)
            {
                return;
            }

            FrameSample[] samples = _collector.GetLastN(DataCollector.Capacity);
            List<SmartAlert> alerts = AnalyzerEngine.Analyze(samples);

            _alertsContainer.Clear();

            if (alerts.Count == 0)
            {
                _alertsContainer.Add(new Label(SmartProfilerLocalization.Get("profiler.alerts.healthy"))
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
                SmartAlert alert = alerts[i];
                VisualElement element = new VisualElement();
                element.AddToClassList("alert-item");

                string styleClass = "alert-item-info";
                if (alert.Level == AlertLevel.Critical)
                {
                    styleClass = "alert-item-critical";
                }
                else if (alert.Level == AlertLevel.Warning)
                {
                    styleClass = "alert-item-warn";
                }

                element.AddToClassList(styleClass);
                element.Add(new Label(alert.Title)
                {
                    style =
                    {
                        fontSize = 13,
                        unityFontStyleAndWeight = FontStyle.Bold,
                        color = Color.white,
                        marginBottom = 2
                    }
                });

                element.Add(new Label(alert.Message)
                {
                    style =
                    {
                        fontSize = 11,
                        color = new Color(1f, 1f, 1f, 0.7f),
                        whiteSpace = WhiteSpace.Normal
                    }
                });

                _alertsContainer.Add(element);
            }
        }

        private void SaveSnapshot(bool selectAsBaseline)
        {
            if (_collector == null)
            {
                return;
            }

            FrameSample[] samples = _collector.GetLastN(DataCollector.Capacity);
            if (samples == null || samples.Length == 0)
            {
                ShowNotification(new GUIContent(SmartProfilerLocalization.Get("profiler.snapshot.waitFrames")));
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

            ProfilerSnapshot snapshot = SnapshotComparison.CreateSnapshot(displayName, samples);
            SnapshotComparison.SaveSnapshot(snapshot);

            ReloadSnapshotsUI(snapshot.FileName, selectAsBaseline);
            ShowNotification(new GUIContent(SmartProfilerLocalization.Format("profiler.snapshot.saved", snapshot.DisplayName)));
        }

        private void ReloadSnapshotsUI(string preferredFileName = null, bool preferredForBaseline = false)
        {
            List<string> previousBaselineChoices = _baselinePopup != null ? new List<string>(_baselinePopup.choices) : null;
            List<string> previousCurrentChoices = _currentPopup != null ? new List<string>(_currentPopup.choices) : null;
            string previousBaselineValue = _baselinePopup != null ? _baselinePopup.value : null;
            string previousCurrentValue = _currentPopup != null ? _currentPopup.value : null;

            _snapshots.Clear();
            _snapshots.AddRange(SnapshotComparison.LoadAllSnapshots());

            List<string> choices = new List<string> { NoSnapshotOption };
            for (int i = 0; i < _snapshots.Count; i++)
            {
                choices.Add(_snapshots[i].FileName);
            }

            if (_baselinePopup != null)
            {
                _baselinePopup.choices = choices;
                _baselinePopup.value = RemapSnapshotSelection(previousBaselineValue, previousBaselineChoices, choices);
            }

            if (_currentPopup != null)
            {
                _currentPopup.choices = choices;
                _currentPopup.value = RemapSnapshotSelection(previousCurrentValue, previousCurrentChoices, choices);
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

        private string RemapSnapshotSelection(string value, List<string> previousChoices, List<string> newChoices)
        {
            if (string.IsNullOrEmpty(value))
            {
                return NoSnapshotOption;
            }

            if (previousChoices != null && previousChoices.Count > 0 && value == previousChoices[0])
            {
                return NoSnapshotOption;
            }

            return newChoices.Contains(value) ? value : NoSnapshotOption;
        }

        private void EnsureSnapshotSelection(PopupField<string> popup, int fallbackIndex)
        {
            if (popup == null || popup.choices == null || popup.choices.Count == 0)
            {
                return;
            }

            if (!popup.choices.Contains(popup.value))
            {
                popup.value = popup.choices[Mathf.Clamp(fallbackIndex, 0, popup.choices.Count - 1)];
            }
        }

        private void RefreshSnapshotComparison()
        {
            ProfilerSnapshot baseline = FindSnapshot(_baselinePopup != null ? _baselinePopup.value : null);
            ProfilerSnapshot current = FindSnapshot(_currentPopup != null ? _currentPopup.value : null);

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

            List<SnapshotMetricDelta> rows = SnapshotComparison.BuildComparison(baseline, current);
            for (int i = 0; i < rows.Count; i++)
            {
                _snapshotComparisonContainer.Add(CreateComparisonRow(rows[i]));
            }
        }

        private void ExportReport(bool markdown)
        {
            ProfilerSnapshot baseline = FindSnapshot(_baselinePopup != null ? _baselinePopup.value : null);
            ProfilerSnapshot current = FindSnapshot(_currentPopup != null ? _currentPopup.value : null);
            if (baseline == null || current == null || baseline.FileName == current.FileName)
            {
                ShowNotification(new GUIContent(SmartProfilerLocalization.Get("profiler.export.needComparison")));
                return;
            }

            List<SnapshotMetricDelta> comparisonRows = SnapshotComparison.BuildComparison(baseline, current);
            FrameSample[] samples = _collector != null ? _collector.GetLastN(DataCollector.Capacity) : Array.Empty<FrameSample>();
            List<SmartAlert> alerts = AnalyzerEngine.Analyze(samples);

            string extension = markdown ? "md" : "csv";
            string defaultName = "smart_profiler_report_" + DateTime.Now.ToString("yyyyMMdd_HHmm");
            string titleKey = markdown ? "profiler.export.title.md" : "profiler.export.title.csv";
            string path = EditorUtility.SaveFilePanel(
                SmartProfilerLocalization.Get(titleKey),
                Directory.GetCurrentDirectory(),
                defaultName,
                extension);

            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            if (markdown)
            {
                SmartProfilerReportExporter.ExportMarkdown(path, baseline, current, comparisonRows, _hasLastSample, _lastSample, alerts);
            }
            else
            {
                SmartProfilerReportExporter.ExportCsv(path, baseline, current, comparisonRows, _hasLastSample, _lastSample, alerts);
            }

            ShowNotification(new GUIContent(SmartProfilerLocalization.Format("profiler.export.saved", Path.GetFileName(path))));
        }

        private void UpdateSnapshotMeta(Label targetLabel, ProfilerSnapshot snapshot)
        {
            if (targetLabel == null)
            {
                return;
            }

            if (snapshot == null)
            {
                targetLabel.text = SmartProfilerLocalization.Get("profiler.snapshot.noneSelected");
                return;
            }

            string dateText = snapshot.CreatedAtIsoUtc;
            DateTime parsedDate;
            if (!string.IsNullOrEmpty(snapshot.CreatedAtIsoUtc) && DateTime.TryParse(snapshot.CreatedAtIsoUtc, out parsedDate))
            {
                dateText = parsedDate.ToLocalTime().ToString("g");
            }

            targetLabel.text = SmartProfilerLocalization.Format("profiler.snapshot.meta.frames", dateText, snapshot.SampleCount);
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
            VisualElement element = new VisualElement();
            element.AddToClassList("comparison-row");

            Label label = new Label(row.Label);
            label.AddToClassList("comparison-label");
            element.Add(label);

            Label baselineValue = new Label(row.BaselineValue);
            baselineValue.AddToClassList("comparison-value");
            element.Add(baselineValue);

            Label currentValue = new Label(row.CurrentValue);
            currentValue.AddToClassList("comparison-value");
            element.Add(currentValue);

            Label delta = new Label(row.DeltaText);
            delta.AddToClassList("comparison-delta");
            AddTrendClass(delta, row.Trend);
            element.Add(delta);

            Label status = new Label(GetTrendIcon(row.Trend));
            status.AddToClassList("comparison-status");
            AddTrendClass(status, row.Trend);
            element.Add(status);

            return element;
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
                    return SmartProfilerLocalization.Get("profiler.trend.ok");
                case SnapshotTrend.Regressed:
                    return SmartProfilerLocalization.Get("profiler.trend.bad");
                default:
                    return SmartProfilerLocalization.Get("profiler.trend.neutral");
            }
        }

        private List<string> BuildLanguageChoices()
        {
            return new List<string>
            {
                SmartProfilerLocalization.GetLanguageDisplayName(SmartProfilerLanguage.English),
                SmartProfilerLocalization.GetLanguageDisplayName(SmartProfilerLanguage.Turkish)
            };
        }

        private void UpdateLanguagePopupChoices()
        {
            if (_languagePopup == null)
            {
                return;
            }

            List<string> choices = BuildLanguageChoices();
            _languagePopup.label = SmartProfilerLocalization.Get("language.label");
            _languagePopup.choices = choices;
            _languagePopup.value = choices[(int)SmartProfilerLocalization.CurrentLanguage];
        }

        private void SetLabelText(VisualElement root, string name, string value)
        {
            Label label = root.Q<Label>(name);
            if (label != null)
            {
                label.text = value;
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

        private void ShowFpsCanvas()
        {
            GameObject canvasGO = FPSCanvasCreator.CreateFpsCanvas();

            if (canvasGO == null)
            {
                EditorUtility.DisplayDialog("Error", SmartProfilerLocalization.Get("profiler.fpsCanvas.noScene"), "OK");
                return;
            }

            Undo.RegisterCreatedObjectUndo(canvasGO, "Create FPS Canvas");
            string msg = SmartProfilerLocalization.Get("profiler.fpsCanvas.createdMsg").Replace("{0}", canvasGO.name);
            EditorUtility.DisplayDialog(SmartProfilerLocalization.Get("profiler.fpsCanvas.created"), msg, "OK");
        }
    }
}
