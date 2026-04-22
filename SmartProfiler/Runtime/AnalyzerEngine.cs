using System.Collections.Generic;
using UnityEngine;

namespace SmartProfiler.Runtime
{
    public enum AlertLevel
    {
        Info,
        Warning,
        Critical
    }

    public struct SmartAlert
    {
        public AlertLevel Level;
        public string Title;
        public string Message;
    }

    public static class AnalyzerEngine
    {
        public static List<SmartAlert> Analyze(FrameSample[] samples)
        {
            var alerts = new List<SmartAlert>();
            if (samples == null || samples.Length == 0)
            {
                return alerts;
            }

            int count = samples.Length;
            int gcCount = 0;
            float totalPhysics = 0f;
            float totalTime = 0f;
            int unbatchedDrawCalls = 0;
            int totalDrawCalls = 0;

            for (int i = 0; i < count; i++)
            {
                FrameSample sample = samples[i];
                if (sample.GcAllocBytes > 0)
                {
                    gcCount++;
                }

                totalPhysics += sample.PhysicsTimeMs;
                totalTime += sample.FrameTimeMs;
                totalDrawCalls += sample.DrawCalls;
                unbatchedDrawCalls += Mathf.Max(0, sample.DrawCalls - sample.Batches);
            }

            float gcRatio = (float)gcCount / count;
            if (gcRatio > 0.5f)
            {
                alerts.Add(new SmartAlert
                {
                    Level = AlertLevel.Critical,
                    Title = SmartProfilerLocalization.Get("alert.gc.title"),
                    Message = SmartProfilerLocalization.Get("alert.gc.message")
                });
            }

            float physicsRatio = totalTime > 0f ? totalPhysics / totalTime : 0f;
            if (physicsRatio > 0.4f)
            {
                alerts.Add(new SmartAlert
                {
                    Level = AlertLevel.Warning,
                    Title = SmartProfilerLocalization.Get("alert.physics.title"),
                    Message = SmartProfilerLocalization.Format("alert.physics.message", physicsRatio * 100f)
                });
            }

            float unbatchedRatio = totalDrawCalls > 0 ? (float)unbatchedDrawCalls / totalDrawCalls : 0f;
            if (unbatchedRatio > 0.7f && totalDrawCalls > 100)
            {
                alerts.Add(new SmartAlert
                {
                    Level = AlertLevel.Info,
                    Title = SmartProfilerLocalization.Get("alert.batching.title"),
                    Message = SmartProfilerLocalization.Format("alert.batching.message", unbatchedRatio * 100f)
                });
            }

            if (count > 200)
            {
                long startHeap = samples[0].TotalHeapBytes;
                long endHeap = samples[count - 1].TotalHeapBytes;
                long delta = endHeap - startHeap;
                if (delta > 5 * 1024 * 1024)
                {
                    alerts.Add(new SmartAlert
                    {
                        Level = AlertLevel.Critical,
                        Title = SmartProfilerLocalization.Get("alert.memory.title"),
                        Message = SmartProfilerLocalization.Format("alert.memory.message", delta / 1048576f)
                    });
                }
            }

            return alerts;
        }
    }
}
