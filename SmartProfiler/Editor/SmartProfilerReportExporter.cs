using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using SmartProfiler.Runtime;

namespace SmartProfiler.Editor
{
    public static class SmartProfilerReportExporter
    {
        public static void ExportCsv(
            string path,
            ProfilerSnapshot baseline,
            ProfilerSnapshot current,
            List<SnapshotMetricDelta> comparisonRows,
            bool hasLiveSample,
            FrameSample liveSample,
            List<SmartAlert> alerts)
        {
            var sb = new StringBuilder();

            AppendCsvPair(sb, SmartProfilerLocalization.Get("report.title"), SmartProfilerLocalization.Get("profiler.window.title"));
            AppendCsvPair(sb, SmartProfilerLocalization.Get("report.generated"), DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            AppendCsvPair(sb, SmartProfilerLocalization.Get("report.language"), SmartProfilerLocalization.GetLanguageDisplayName(SmartProfilerLocalization.CurrentLanguage));
            AppendCsvPair(sb, SmartProfilerLocalization.Get("report.baseline"), baseline != null ? baseline.DisplayName : SmartProfilerLocalization.Get("report.none"));
            AppendCsvPair(sb, SmartProfilerLocalization.Get("report.current"), current != null ? current.DisplayName : SmartProfilerLocalization.Get("report.none"));
            sb.AppendLine();

            sb.AppendLine(ToCsv(
                SmartProfilerLocalization.Get("report.comparison.metric"),
                SmartProfilerLocalization.Get("report.comparison.baseline"),
                SmartProfilerLocalization.Get("report.comparison.current"),
                SmartProfilerLocalization.Get("report.comparison.delta"),
                SmartProfilerLocalization.Get("report.comparison.trend")));

            if (comparisonRows != null)
            {
                for (int i = 0; i < comparisonRows.Count; i++)
                {
                    SnapshotMetricDelta row = comparisonRows[i];
                    sb.AppendLine(ToCsv(row.Label, row.BaselineValue, row.CurrentValue, row.DeltaText, GetTrendLabel(row.Trend)));
                }
            }

            sb.AppendLine();
            sb.AppendLine(ToCsv(SmartProfilerLocalization.Get("report.section.live"), string.Empty));
            if (hasLiveSample)
            {
                AppendCsvPair(sb, SmartProfilerLocalization.Get("report.live.frameTime"), liveSample.FrameTimeMs.ToString("F1") + " ms");
                float fps = liveSample.FrameTimeMs > 0f ? 1000f / liveSample.FrameTimeMs : 0f;
                AppendCsvPair(sb, SmartProfilerLocalization.Get("report.live.fps"), fps.ToString("F1"));
                AppendCsvPair(sb, SmartProfilerLocalization.Get("report.live.gcAlloc"), FormatBytes(liveSample.GcAllocBytes));
                AppendCsvPair(sb, SmartProfilerLocalization.Get("report.live.drawCalls"), liveSample.DrawCalls.ToString());
                AppendCsvPair(sb, SmartProfilerLocalization.Get("report.live.batches"), liveSample.Batches.ToString());
                AppendCsvPair(sb, SmartProfilerLocalization.Get("report.live.heap"), FormatBytes(liveSample.TotalHeapBytes));
                AppendCsvPair(sb, SmartProfilerLocalization.Get("report.live.physics"), liveSample.PhysicsTimeMs.ToString("F2") + " ms");
                AppendCsvPair(sb, SmartProfilerLocalization.Get("report.live.camera"), liveSample.CameraRenderMs.ToString("F2") + " ms");
                AppendCsvPair(sb, SmartProfilerLocalization.Get("report.live.animator"), liveSample.AnimatorUpdateMs.ToString("F2") + " ms");
                AppendCsvPair(sb, SmartProfilerLocalization.Get("report.live.gcCollect"), liveSample.GcCollectMs.ToString("F2") + " ms");
            }
            else
            {
                AppendCsvPair(sb, SmartProfilerLocalization.Get("report.none"), string.Empty);
            }

            sb.AppendLine();
            sb.AppendLine(ToCsv(
                SmartProfilerLocalization.Get("report.alert.level"),
                SmartProfilerLocalization.Get("report.alert.title"),
                SmartProfilerLocalization.Get("report.alert.message")));

            if (alerts != null && alerts.Count > 0)
            {
                for (int i = 0; i < alerts.Count; i++)
                {
                    SmartAlert alert = alerts[i];
                    sb.AppendLine(ToCsv(GetAlertLevelLabel(alert.Level), alert.Title, alert.Message));
                }
            }
            else
            {
                sb.AppendLine(ToCsv(SmartProfilerLocalization.Get("report.none"), string.Empty, string.Empty));
            }

            File.WriteAllText(path, sb.ToString(), new UTF8Encoding(true));
        }

        public static void ExportMarkdown(
            string path,
            ProfilerSnapshot baseline,
            ProfilerSnapshot current,
            List<SnapshotMetricDelta> comparisonRows,
            bool hasLiveSample,
            FrameSample liveSample,
            List<SmartAlert> alerts)
        {
            var sb = new StringBuilder();

            sb.AppendLine("# " + SmartProfilerLocalization.Get("report.title"));
            sb.AppendLine();
            sb.AppendLine("- " + SmartProfilerLocalization.Get("report.generated") + ": " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            sb.AppendLine("- " + SmartProfilerLocalization.Get("report.language") + ": " + SmartProfilerLocalization.GetLanguageDisplayName(SmartProfilerLocalization.CurrentLanguage));
            sb.AppendLine("- " + SmartProfilerLocalization.Get("report.baseline") + ": " + (baseline != null ? baseline.DisplayName : SmartProfilerLocalization.Get("report.none")));
            sb.AppendLine("- " + SmartProfilerLocalization.Get("report.current") + ": " + (current != null ? current.DisplayName : SmartProfilerLocalization.Get("report.none")));
            sb.AppendLine();

            sb.AppendLine("## " + SmartProfilerLocalization.Get("report.section.comparison"));
            sb.AppendLine();
            sb.AppendLine("| " + SmartProfilerLocalization.Get("report.comparison.metric") + " | " + SmartProfilerLocalization.Get("report.comparison.baseline") + " | " + SmartProfilerLocalization.Get("report.comparison.current") + " | " + SmartProfilerLocalization.Get("report.comparison.delta") + " | " + SmartProfilerLocalization.Get("report.comparison.trend") + " |");
            sb.AppendLine("|---|---:|---:|---:|---|");

            if (comparisonRows != null && comparisonRows.Count > 0)
            {
                for (int i = 0; i < comparisonRows.Count; i++)
                {
                    SnapshotMetricDelta row = comparisonRows[i];
                    sb.AppendLine("| " + EscapeMarkdown(row.Label) + " | " + EscapeMarkdown(row.BaselineValue) + " | " + EscapeMarkdown(row.CurrentValue) + " | " + EscapeMarkdown(row.DeltaText) + " | " + EscapeMarkdown(GetTrendLabel(row.Trend)) + " |");
                }
            }
            else
            {
                sb.AppendLine("| " + SmartProfilerLocalization.Get("report.none") + " | - | - | - | - |");
            }

            sb.AppendLine();
            sb.AppendLine("## " + SmartProfilerLocalization.Get("report.section.live"));
            sb.AppendLine();

            if (hasLiveSample)
            {
                float fps = liveSample.FrameTimeMs > 0f ? 1000f / liveSample.FrameTimeMs : 0f;
                sb.AppendLine("- " + SmartProfilerLocalization.Get("report.live.frameTime") + ": " + liveSample.FrameTimeMs.ToString("F1") + " ms");
                sb.AppendLine("- " + SmartProfilerLocalization.Get("report.live.fps") + ": " + fps.ToString("F1"));
                sb.AppendLine("- " + SmartProfilerLocalization.Get("report.live.gcAlloc") + ": " + FormatBytes(liveSample.GcAllocBytes));
                sb.AppendLine("- " + SmartProfilerLocalization.Get("report.live.drawCalls") + ": " + liveSample.DrawCalls);
                sb.AppendLine("- " + SmartProfilerLocalization.Get("report.live.batches") + ": " + liveSample.Batches);
                sb.AppendLine("- " + SmartProfilerLocalization.Get("report.live.heap") + ": " + FormatBytes(liveSample.TotalHeapBytes));
                sb.AppendLine("- " + SmartProfilerLocalization.Get("report.live.physics") + ": " + liveSample.PhysicsTimeMs.ToString("F2") + " ms");
                sb.AppendLine("- " + SmartProfilerLocalization.Get("report.live.camera") + ": " + liveSample.CameraRenderMs.ToString("F2") + " ms");
                sb.AppendLine("- " + SmartProfilerLocalization.Get("report.live.animator") + ": " + liveSample.AnimatorUpdateMs.ToString("F2") + " ms");
                sb.AppendLine("- " + SmartProfilerLocalization.Get("report.live.gcCollect") + ": " + liveSample.GcCollectMs.ToString("F2") + " ms");
            }
            else
            {
                sb.AppendLine("- " + SmartProfilerLocalization.Get("report.none"));
            }

            sb.AppendLine();
            sb.AppendLine("## " + SmartProfilerLocalization.Get("report.section.alerts"));
            sb.AppendLine();

            if (alerts != null && alerts.Count > 0)
            {
                for (int i = 0; i < alerts.Count; i++)
                {
                    SmartAlert alert = alerts[i];
                    sb.AppendLine("- **" + EscapeMarkdown(GetAlertLevelLabel(alert.Level)) + "** - " + EscapeMarkdown(alert.Title));
                    sb.AppendLine("  " + EscapeMarkdown(alert.Message));
                }
            }
            else
            {
                sb.AppendLine("- " + SmartProfilerLocalization.Get("report.none"));
            }

            File.WriteAllText(path, sb.ToString(), new UTF8Encoding(true));
        }

        private static void AppendCsvPair(StringBuilder sb, string key, string value)
        {
            sb.AppendLine(ToCsv(key, value));
        }

        private static string ToCsv(params string[] columns)
        {
            var escaped = new string[columns.Length];
            for (int i = 0; i < columns.Length; i++)
            {
                string value = columns[i] ?? string.Empty;
                value = value.Replace("\"", "\"\"");
                escaped[i] = "\"" + value + "\"";
            }

            return string.Join(",", escaped);
        }

        private static string EscapeMarkdown(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return string.Empty;
            }

            return text.Replace("|", "\\|");
        }

        private static string GetTrendLabel(SnapshotTrend trend)
        {
            switch (trend)
            {
                case SnapshotTrend.Improved:
                    return SmartProfilerLocalization.Get("report.trend.improved");
                case SnapshotTrend.Regressed:
                    return SmartProfilerLocalization.Get("report.trend.regressed");
                default:
                    return SmartProfilerLocalization.Get("report.trend.unchanged");
            }
        }

        private static string GetAlertLevelLabel(AlertLevel level)
        {
            switch (level)
            {
                case AlertLevel.Critical:
                    return SmartProfilerLocalization.Get("report.level.critical");
                case AlertLevel.Warning:
                    return SmartProfilerLocalization.Get("report.level.warning");
                default:
                    return SmartProfilerLocalization.Get("report.level.info");
            }
        }

        private static string FormatBytes(long bytes)
        {
            if (bytes <= 0)
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
