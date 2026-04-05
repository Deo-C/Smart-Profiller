using UnityEngine;
using System.Collections.Generic;

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
            if (samples == null || samples.Length == 0) return alerts;

            int count = samples.Length;
            int gcCount = 0;
            float totalPhysics = 0;
            float totalTime = 0;
            
            int unbatchedDrawCalls = 0;
            int totalDrawCalls = 0;

            for (int i = 0; i < count; i++)
            {
                var s = samples[i];
                if (s.GcAllocBytes > 0) gcCount++;
                
                totalPhysics += s.PhysicsTimeMs;
                totalTime += s.FrameTimeMs;

                totalDrawCalls += s.DrawCalls;
                unbatchedDrawCalls += Mathf.Max(0, s.DrawCalls - s.Batches);
            }

            // 1. GC Warning
            float gcRatio = (float)gcCount / count;
            if (gcRatio > 0.5f)
            {
                alerts.Add(new SmartAlert {
                    Level = AlertLevel.Critical,
                    Title = "Her Frame'de GC Alloc",
                    Message = "Karelerin %50'sinden fazlasında Garbage Collection çöpü üretiliyor. Update() içinde 'new' kullanımı veya obje yaratımı olabilir. Object Pooling kullanmayı düşünün."
                });
            }

            // 2. Physics Timestep Warning
            float physicsRatio = totalTime > 0 ? (totalPhysics / totalTime) : 0;
            if (physicsRatio > 0.4f)
            {
                alerts.Add(new SmartAlert {
                    Level = AlertLevel.Warning,
                    Title = "Physics Frame Time'ı Domine Ediyor",
                    Message = $"Fiziğin CPU kullanımı tüm çerçevenin %{physicsRatio*100:F0}'unu alıyor. Edit > Project Settings > Time altından 'Fixed Timestep' değerini 0.02'den 0.04'e çekmeyi düşünebilirsiniz."
                });
            }

            // 3. Batching Warning
            float unbatchedRatio = totalDrawCalls > 0 ? ((float)unbatchedDrawCalls / totalDrawCalls) : 0;
            if (unbatchedRatio > 0.7f && totalDrawCalls > 100)
            {
                alerts.Add(new SmartAlert {
                    Level = AlertLevel.Info,
                    Title = "Düşük Batching Oranı (\u00D6neri)",
                    Message = $"Draw call'larınızın %{unbatchedRatio*100:F0}'i unbatched. Aynı materyali kullanan statik objeleriniz varsa 'Static Batching' açarak veya GPU Instancing kullanarak CPU'dan büyük oranda tasarruf edebilirsiniz."
                });
            }

            // 4. Memory Leak
            if (count > 200)
            {
                long startHeap = samples[0].TotalHeapBytes;
                long endHeap = samples[count - 1].TotalHeapBytes;
                long delta = endHeap - startHeap;
                
                if (delta > 5 * 1024 * 1024) // 5MB büyüme varsa
                {
                    alerts.Add(new SmartAlert {
                        Level = AlertLevel.Critical,
                        Title = "Potansiyel Memory Leak",
                        Message = $"Sistemin kullandığı RAM (Heap) son 300 karede {delta / 1048576f:F1} MB büyüdü. Destroy edilip referansı listede unutulan objeler olabilir."
                    });
                }
            }

            return alerts;
        }
    }
}
