using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using SmartProfiler.Runtime;

namespace SmartProfiler.Editor
{
    [Serializable]
    public class ProfilerSnapshot
    {
        public string DisplayName;
        public string FileName;
        public string CreatedAtIsoUtc;
        public int SampleCount;
        public SnapshotMetrics Metrics;
    }

    [Serializable]
    public struct SnapshotMetrics
    {
        public float AvgFrameTimeMs;
        public float P95FrameTimeMs;
        public float AvgGcAllocBytes;
        public float AvgDrawCalls;
        public int SpikeCount;
    }

    public enum SnapshotTrend
    {
        Improved,
        Regressed,
        Unchanged
    }

    public struct SnapshotMetricDelta
    {
        public string Label;
        public string BaselineValue;
        public string CurrentValue;
        public string DeltaText;
        public SnapshotTrend Trend;
    }

    public static class SnapshotComparison
    {
        private const float SpikeThresholdMs = 33.3f;
        private const float UnchangedEpsilonPercent = 0.5f;

        public static string SnapshotDirectory
        {
            get { return Path.Combine(Directory.GetCurrentDirectory(), "ProjectSettings", "SmartProfilerSnapshots"); }
        }

        public static SnapshotMetrics CalculateMetrics(FrameSample[] samples)
        {
            var metrics = new SnapshotMetrics();
            if (samples == null || samples.Length == 0)
            {
                return metrics;
            }

            int count = samples.Length;
            float totalFrameTime = 0f;
            float totalGcAlloc = 0f;
            float totalDrawCalls = 0f;
            var sortedFrameTimes = new float[count];

            for (int i = 0; i < count; i++)
            {
                FrameSample sample = samples[i];
                totalFrameTime += sample.FrameTimeMs;
                totalGcAlloc += sample.GcAllocBytes;
                totalDrawCalls += sample.DrawCalls;
                sortedFrameTimes[i] = sample.FrameTimeMs;

                if (sample.FrameTimeMs > SpikeThresholdMs)
                {
                    metrics.SpikeCount++;
                }
            }

            Array.Sort(sortedFrameTimes);

            metrics.AvgFrameTimeMs = totalFrameTime / count;
            metrics.P95FrameTimeMs = Percentile(sortedFrameTimes, 0.95f);
            metrics.AvgGcAllocBytes = totalGcAlloc / count;
            metrics.AvgDrawCalls = totalDrawCalls / count;
            return metrics;
        }

        public static ProfilerSnapshot CreateSnapshot(string displayName, FrameSample[] samples)
        {
            string resolvedName = string.IsNullOrWhiteSpace(displayName) ? "Snapshot" : displayName.Trim();
            return new ProfilerSnapshot
            {
                DisplayName = resolvedName,
                FileName = MakeSafeFileName(resolvedName),
                CreatedAtIsoUtc = DateTime.UtcNow.ToString("o"),
                SampleCount = samples != null ? samples.Length : 0,
                Metrics = CalculateMetrics(samples)
            };
        }

        public static void SaveSnapshot(ProfilerSnapshot snapshot)
        {
            if (snapshot == null)
            {
                return;
            }

            Directory.CreateDirectory(SnapshotDirectory);
            string filePath = GetSnapshotPath(snapshot.FileName);
            string json = JsonUtility.ToJson(snapshot, true);
            File.WriteAllText(filePath, json);
        }

        public static List<ProfilerSnapshot> LoadAllSnapshots()
        {
            var snapshots = new List<ProfilerSnapshot>();
            if (!Directory.Exists(SnapshotDirectory))
            {
                return snapshots;
            }

            string[] files = Directory.GetFiles(SnapshotDirectory, "*.json");
            for (int i = 0; i < files.Length; i++)
            {
                try
                {
                    string json = File.ReadAllText(files[i]);
                    var snapshot = JsonUtility.FromJson<ProfilerSnapshot>(json);
                    if (snapshot == null)
                    {
                        continue;
                    }

                    if (string.IsNullOrEmpty(snapshot.FileName))
                    {
                        snapshot.FileName = Path.GetFileNameWithoutExtension(files[i]);
                    }

                    snapshots.Add(snapshot);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning("SmartProfiler snapshot load failed: " + files[i] + "\n" + ex.Message);
                }
            }

            snapshots.Sort((a, b) => string.CompareOrdinal(b.CreatedAtIsoUtc, a.CreatedAtIsoUtc));
            return snapshots;
        }

        public static List<SnapshotMetricDelta> BuildComparison(ProfilerSnapshot baseline, ProfilerSnapshot current)
        {
            var rows = new List<SnapshotMetricDelta>();
            if (baseline == null || current == null)
            {
                return rows;
            }

            rows.Add(BuildLowerIsBetterRow(SmartProfilerLocalization.Get("profiler.comparison.avgFrame"), baseline.Metrics.AvgFrameTimeMs, current.Metrics.AvgFrameTimeMs, FormatMilliseconds));
            rows.Add(BuildLowerIsBetterRow(SmartProfilerLocalization.Get("profiler.comparison.p95"), baseline.Metrics.P95FrameTimeMs, current.Metrics.P95FrameTimeMs, FormatMilliseconds));
            rows.Add(BuildLowerIsBetterRow(SmartProfilerLocalization.Get("profiler.comparison.gc"), baseline.Metrics.AvgGcAllocBytes, current.Metrics.AvgGcAllocBytes, FormatBytes));
            rows.Add(BuildLowerIsBetterRow(SmartProfilerLocalization.Get("profiler.comparison.drawCalls"), baseline.Metrics.AvgDrawCalls, current.Metrics.AvgDrawCalls, FormatRounded));
            rows.Add(BuildLowerIsBetterRow(SmartProfilerLocalization.Get("profiler.comparison.spikes"), baseline.Metrics.SpikeCount, current.Metrics.SpikeCount, FormatInteger));
            return rows;
        }

        public static string GetSnapshotPath(string fileName)
        {
            string resolvedName = string.IsNullOrWhiteSpace(fileName) ? "snapshot" : fileName;
            return Path.Combine(SnapshotDirectory, resolvedName + ".json");
        }

        private static SnapshotMetricDelta BuildLowerIsBetterRow(string label, float baseline, float current, Func<float, string> formatter)
        {
            float deltaPercent = CalculatePercentDelta(baseline, current);
            SnapshotTrend trend = ResolveTrend(deltaPercent);

            return new SnapshotMetricDelta
            {
                Label = label,
                BaselineValue = formatter(baseline),
                CurrentValue = formatter(current),
                DeltaText = FormatDelta(deltaPercent),
                Trend = trend
            };
        }

        private static SnapshotMetricDelta BuildLowerIsBetterRow(string label, int baseline, int current, Func<float, string> formatter)
        {
            return BuildLowerIsBetterRow(label, (float)baseline, (float)current, formatter);
        }

        private static float Percentile(float[] sortedValues, float percentile)
        {
            if (sortedValues == null || sortedValues.Length == 0)
            {
                return 0f;
            }

            percentile = Mathf.Clamp01(percentile);
            int index = Mathf.Clamp(Mathf.CeilToInt(sortedValues.Length * percentile) - 1, 0, sortedValues.Length - 1);
            return sortedValues[index];
        }

        private static float CalculatePercentDelta(float baseline, float current)
        {
            if (Mathf.Approximately(baseline, 0f))
            {
                if (Mathf.Approximately(current, 0f))
                {
                    return 0f;
                }

                return current < baseline ? 100f : -100f;
            }

            return ((baseline - current) / baseline) * 100f;
        }

        private static SnapshotTrend ResolveTrend(float deltaPercent)
        {
            if (Mathf.Abs(deltaPercent) < UnchangedEpsilonPercent)
            {
                return SnapshotTrend.Unchanged;
            }

            return deltaPercent > 0f ? SnapshotTrend.Improved : SnapshotTrend.Regressed;
        }

        private static string FormatDelta(float deltaPercent)
        {
            if (Mathf.Abs(deltaPercent) < UnchangedEpsilonPercent)
            {
                return "0%";
            }

            if (deltaPercent > 0f)
            {
                return "v %" + Mathf.RoundToInt(deltaPercent);
            }

            return "^ %" + Mathf.RoundToInt(Mathf.Abs(deltaPercent));
        }

        private static string MakeSafeFileName(string displayName)
        {
            string trimmed = string.IsNullOrWhiteSpace(displayName) ? "snapshot" : displayName.Trim();
            char[] invalid = Path.GetInvalidFileNameChars();
            for (int i = 0; i < invalid.Length; i++)
            {
                trimmed = trimmed.Replace(invalid[i], '_');
            }

            return trimmed.Replace(' ', '_');
        }

        private static string FormatMilliseconds(float value)
        {
            return value.ToString("F1") + "ms";
        }

        private static string FormatBytes(float value)
        {
            if (value <= 0f)
            {
                return "0 B";
            }

            if (value < 1024f)
            {
                return value.ToString("F0") + " B";
            }

            if (value < 1048576f)
            {
                return (value / 1024f).ToString("F1") + " KB";
            }

            return (value / 1048576f).ToString("F1") + " MB";
        }

        private static string FormatRounded(float value)
        {
            return Mathf.RoundToInt(value).ToString();
        }

        private static string FormatInteger(float value)
        {
            return Mathf.RoundToInt(value).ToString();
        }
    }
}
