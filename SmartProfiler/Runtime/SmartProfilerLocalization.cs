using System;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace SmartProfiler.Runtime
{
    public enum SmartProfilerLanguage
    {
        English,
        Turkish
    }

    public static class SmartProfilerLocalization
    {
        private const string LanguagePrefKey = "SmartProfiler.Language";

        private static readonly Dictionary<string, string> English = new Dictionary<string, string>
        {
            ["language.label"] = "Language",
            ["language.english"] = "English",
            ["language.turkish"] = "Turkish",

            ["profiler.window.title"] = "Smart Profiler",
            ["profiler.stat.frame"] = "FRAME TIME (FPS)",
            ["profiler.stat.gc"] = "GC ALLOC / FRAME",
            ["profiler.stat.drawcalls"] = "DRAW CALLS",
            ["profiler.stat.batching"] = "BATCHING",
            ["profiler.batch.saved"] = "{0}% saved",
            ["chart.tooltip.frame"] = "Frame {0}",
            ["chart.tooltip.frameTime"] = "Frame Time",
            ["chart.tooltip.heap"] = "Heap Memory",
            ["chart.tooltip.drawCalls"] = "Draw Calls",
            ["chart.tooltip.biggest"] = "Biggest Subsystem",
            ["chart.tooltip.none"] = "None",
            ["chart.subsystem.physics"] = "Physics",
            ["chart.subsystem.camera"] = "Camera",
            ["chart.subsystem.animator"] = "Animator",
            ["chart.subsystem.gcCollect"] = "GC.Collect",
            ["chart.overlay.fps"] = "FPS: {0:F0} (Max: {1:F0})",
            ["chart.overlay.memory"] = "HEAP MEMORY: {0:F1} MB (Max: {1:F0} MB)",
            ["chart.overlay.drawCalls"] = "DRAW CALLS: {0} (Max: {1:F0})",
            ["profiler.toggle.autoscale"] = "Auto Scale Y-Axis",
            ["profiler.toggle.autoscale.tooltip"] = "When auto scale is disabled, you can use the mouse wheel over the charts to zoom the Y axis.",
            ["profiler.fpsCanvas.show"] = "Show FPS Canvas",
            ["profiler.fpsCanvas.created"] = "FPS Canvas Created",
            ["profiler.fpsCanvas.createdMsg"] = "FPS Canvas oluşturuldu!\\nGameObject: {0}",
            ["profiler.fpsCanvas.noScene"] = "Aktif sahne bulunamadı!",
            ["profiler.snapshot.title"] = "SNAPSHOT COMPARISON",
            ["profiler.snapshot.subtitle"] = "See whether optimization work actually helped.",
            ["profiler.snapshot.field"] = "Snapshot",
            ["profiler.snapshot.saveBaseline"] = "Save Baseline",
            ["profiler.snapshot.saveCurrent"] = "Save Current",
            ["profiler.snapshot.baseline"] = "BASELINE",
            ["profiler.snapshot.current"] = "CURRENT",
            ["profiler.snapshot.none"] = "Select snapshot",
            ["profiler.snapshot.noneSelected"] = "No snapshot selected",
            ["profiler.snapshot.empty"] = "Save two snapshots or pick existing ones to compare builds.",
            ["profiler.snapshot.saved"] = "Snapshot saved: {0}",
            ["profiler.snapshot.waitFrames"] = "Wait for a few frames before saving a snapshot.",
            ["profiler.export.csv"] = "Export CSV",
            ["profiler.export.md"] = "Export Markdown",
            ["profiler.export.needComparison"] = "Select different baseline and current snapshots first.",
            ["profiler.export.saved"] = "Report exported: {0}",
            ["profiler.export.title.csv"] = "Export CSV Report",
            ["profiler.export.title.md"] = "Export Markdown Report",
            ["profiler.alerts.healthy"] = "System looks healthy. No critical alerts right now.",
            ["profiler.comparison.avgFrame"] = "Avg frame time",
            ["profiler.comparison.p95"] = "P95 frame time",
            ["profiler.comparison.gc"] = "GC alloc/frame",
            ["profiler.comparison.drawCalls"] = "Draw calls",
            ["profiler.comparison.spikes"] = "Spikes",
            ["profiler.snapshot.meta.frames"] = "{0}  -  {1} frames",
            ["profiler.trend.ok"] = "OK",
            ["profiler.trend.bad"] = "!!",
            ["profiler.trend.neutral"] = "-",
            ["report.title"] = "Smart Profiler Report",
            ["report.generated"] = "Generated At",
            ["report.language"] = "Language",
            ["report.baseline"] = "Baseline",
            ["report.current"] = "Current",
            ["report.section.comparison"] = "Snapshot Comparison",
            ["report.section.live"] = "Live Frame Summary",
            ["report.section.alerts"] = "Alerts",
            ["report.none"] = "None",
            ["report.comparison.metric"] = "Metric",
            ["report.comparison.baseline"] = "Baseline",
            ["report.comparison.current"] = "Current",
            ["report.comparison.delta"] = "Delta",
            ["report.comparison.trend"] = "Trend",
            ["report.trend.improved"] = "Improved",
            ["report.trend.regressed"] = "Regressed",
            ["report.trend.unchanged"] = "Unchanged",
            ["report.live.frameTime"] = "Frame Time",
            ["report.live.fps"] = "FPS",
            ["report.live.gcAlloc"] = "GC Alloc / Frame",
            ["report.live.drawCalls"] = "Draw Calls",
            ["report.live.batches"] = "Batches",
            ["report.live.heap"] = "Heap Memory",
            ["report.live.physics"] = "Physics",
            ["report.live.camera"] = "Camera",
            ["report.live.animator"] = "Animator",
            ["report.live.gcCollect"] = "GC.Collect",
            ["report.alert.level"] = "Level",
            ["report.alert.title"] = "Title",
            ["report.alert.message"] = "Message",
            ["report.level.info"] = "Info",
            ["report.level.warning"] = "Warning",
            ["report.level.critical"] = "Critical",

            ["playtest.window.title"] = "Playtest Recorder",
            ["playtest.header.title"] = "PLAYTEST RECORDER",
            ["playtest.header.body"] = "Record player paths in Play Mode and turn them into a scene heatmap. Use it to spot choke points, loops, and places where players linger.",
            ["playtest.toolbar.reload"] = "Reload Sessions",
            ["playtest.toolbar.delete"] = "Delete Session",
            ["playtest.recording.active"] = "REC  Recording in Play Mode",
            ["playtest.recording.idle"] = "REC  Waiting for Play Mode",
            ["playtest.noSessions"] = "No playtest sessions found yet. Enter Play Mode and move around to generate data.",
            ["playtest.controls.title"] = "Session Controls",
            ["playtest.controls.body"] = "Pick a run and tune how wide the painted movement cells should appear in Scene view.",
            ["playtest.controls.session"] = "Session",
            ["playtest.controls.size"] = "Movement Size",
            ["playtest.controls.hint"] = "Smaller values show sharper paths. Larger values blend nearby movement into wider hot zones.",
            ["playtest.summary.title"] = "Session Summary",
            ["playtest.summary.scene"] = "Scene",
            ["playtest.summary.created"] = "Created",
            ["playtest.summary.duration"] = "Duration",
            ["playtest.summary.samples"] = "Movement Samples",
            ["playtest.legend.title"] = "Legend",
            ["playtest.legend.low"] = "Low traffic",
            ["playtest.legend.medium"] = "Medium traffic",
            ["playtest.legend.high"] = "Highest player density",
            ["playtest.delete.title"] = "Delete Playtest Session",
            ["playtest.delete.message"] = "Delete the selected playtest session permanently?\n\n{0}  |  {1}",
            ["playtest.delete.confirm"] = "Delete",
            ["playtest.delete.cancel"] = "Cancel",
            ["playtest.session.label"] = "{0}  |  {1}  |  {2} pts",

            ["scene.window.title"] = "Scene Organizer",
            ["scene.page.title"] = "SMART SCENE ORGANIZER",
            ["scene.toolbar.refresh"] = "Refresh",
            ["scene.health"] = "Health: {0}",
            ["scene.summary"] = "{0}  |  {1} objects  |  {2} active",
            ["scene.groups.title"] = "Scene Groups",
            ["scene.groups.subtitle"] = "Automatic grouping and color coding for the active scene.",
            ["scene.group.row"] = "{0} objs  |  {1} active  |  {2} tris",
            ["scene.metric.polygons"] = "Polygons",
            ["scene.metric.polygons.hint"] = "Approx. triangle count from visible meshes.",
            ["scene.metric.lights"] = "Active Lights",
            ["scene.metric.lights.hint"] = "Realtime and enabled lights in hierarchy.",
            ["scene.metric.colliders"] = "Colliders",
            ["scene.metric.colliders.hint"] = "Physics surface count in the active scene.",
            ["scene.metric.renderers"] = "Renderers",
            ["scene.metric.renderers.hint"] = "Visible render components.",
            ["scene.metric.rigidbodies"] = "Rigidbodies",
            ["scene.metric.rigidbodies.hint"] = "Dynamic physics bodies.",
            ["scene.metric.canvas"] = "Canvas",
            ["scene.metric.canvas.hint"] = "UI root canvases.",
            ["scene.health.good"] = "Good",
            ["scene.health.warning"] = "Warning",
            ["scene.health.critical"] = "Critical",
            ["scene.group.lighting"] = "Lighting",
            ["scene.group.geometry"] = "Geometry",
            ["scene.group.gameplay"] = "Gameplay",
            ["scene.group.ui"] = "UI",
            ["scene.group.audio"] = "Audio",
            ["scene.group.vfx"] = "VFX",
            ["scene.group.cameras"] = "Cameras",
            ["scene.group.utility"] = "Utility",
            ["scene.untitled"] = "Untitled Scene",

            ["alert.gc.title"] = "GC Allocation Happens Almost Every Frame",
            ["alert.gc.message"] = "More than 50% of sampled frames allocate garbage. You may be creating objects or using 'new' inside Update(). Consider object pooling.",
            ["alert.physics.title"] = "Physics Dominates Frame Time",
            ["alert.physics.message"] = "Physics is taking {0:F0}% of total frame time. You may want to increase Fixed Timestep from 0.02 to 0.04 in Project Settings > Time.",
            ["alert.batching.title"] = "Low Batching Ratio (Suggestion)",
            ["alert.batching.message"] = "{0:F0}% of your draw calls are unbatched. If static objects share materials, try Static Batching or GPU Instancing to save CPU time.",
            ["alert.memory.title"] = "Potential Memory Leak",
            ["alert.memory.message"] = "Heap usage grew by {0:F1} MB over the last 300 frames. Objects may be kept alive by lingering references after they should be destroyed."
        };

        private static readonly Dictionary<string, string> Turkish = new Dictionary<string, string>
        {
            ["language.label"] = "Dil",
            ["language.english"] = "Ingilizce",
            ["language.turkish"] = "Turkce",

            ["profiler.window.title"] = "Smart Profiler",
            ["profiler.stat.frame"] = "FRAME SURESI (FPS)",
            ["profiler.stat.gc"] = "KARE BASI GC ALLOC",
            ["profiler.stat.drawcalls"] = "DRAW CALL",
            ["profiler.stat.batching"] = "BATCHING",
            ["profiler.batch.saved"] = "%{0} kazanc",
            ["chart.tooltip.frame"] = "Frame {0}",
            ["chart.tooltip.frameTime"] = "Frame Suresi",
            ["chart.tooltip.heap"] = "Heap Memory",
            ["chart.tooltip.drawCalls"] = "Draw Call",
            ["chart.tooltip.biggest"] = "En Yuksek Alt Sistem",
            ["chart.tooltip.none"] = "Yok",
            ["chart.subsystem.physics"] = "Physics",
            ["chart.subsystem.camera"] = "Camera",
            ["chart.subsystem.animator"] = "Animator",
            ["chart.subsystem.gcCollect"] = "GC.Collect",
            ["chart.overlay.fps"] = "FPS: {0:F0} (Maks: {1:F0})",
            ["chart.overlay.memory"] = "HEAP MEMORY: {0:F1} MB (Maks: {1:F0} MB)",
            ["chart.overlay.drawCalls"] = "DRAW CALL: {0} (Maks: {1:F0})",
            ["profiler.toggle.autoscale"] = "Y Eksenini Otomatik Olcekle",
            ["profiler.toggle.autoscale.tooltip"] = "Otomatik olcek kapaliyken grafiklerin uzerinde fare tekeri ile Y eksenini yakinlastirabilirsiniz.",
            ["profiler.fpsCanvas.show"] = "FPS Canvas Goster",
            ["profiler.fpsCanvas.created"] = "FPS Canvas Olusturuldu",
            ["profiler.fpsCanvas.createdMsg"] = "FPS Canvas oluşturuldu!\\nGameObject: {0}",
            ["profiler.fpsCanvas.noScene"] = "Aktif sahne bulunamadi!",
            ["profiler.snapshot.title"] = "SNAPSHOT KARSILASTIRMA",
            ["profiler.snapshot.subtitle"] = "Optimizasyon calismasinin gercekten fayda saglayip saglamadigini gorun.",
            ["profiler.snapshot.field"] = "Snapshot",
            ["profiler.snapshot.saveBaseline"] = "Baseline Kaydet",
            ["profiler.snapshot.saveCurrent"] = "Current Kaydet",
            ["profiler.snapshot.baseline"] = "BASELINE",
            ["profiler.snapshot.current"] = "CURRENT",
            ["profiler.snapshot.none"] = "Snapshot sec",
            ["profiler.snapshot.noneSelected"] = "Snapshot secilmedi",
            ["profiler.snapshot.empty"] = "Karsilastirma icin iki snapshot kaydedin veya mevcut snapshotlari secin.",
            ["profiler.snapshot.saved"] = "Snapshot kaydedildi: {0}",
            ["profiler.snapshot.waitFrames"] = "Snapshot kaydetmeden once birkac frame bekleyin.",
            ["profiler.export.csv"] = "CSV Aktar",
            ["profiler.export.md"] = "Markdown Aktar",
            ["profiler.export.needComparison"] = "Once farkli bir baseline ve current snapshot secin.",
            ["profiler.export.saved"] = "Rapor disa aktarildi: {0}",
            ["profiler.export.title.csv"] = "CSV Raporu Disa Aktar",
            ["profiler.export.title.md"] = "Markdown Raporu Disa Aktar",
            ["profiler.alerts.healthy"] = "Sistem saglikli gorunuyor. Su anda kritik bir uyari yok.",
            ["profiler.comparison.avgFrame"] = "Ortalama frame suresi",
            ["profiler.comparison.p95"] = "P95 frame suresi",
            ["profiler.comparison.gc"] = "Kare basi GC alloc",
            ["profiler.comparison.drawCalls"] = "Draw call",
            ["profiler.comparison.spikes"] = "Spike",
            ["profiler.snapshot.meta.frames"] = "{0}  -  {1} frame",
            ["profiler.trend.ok"] = "IYI",
            ["profiler.trend.bad"] = "!!",
            ["profiler.trend.neutral"] = "-",
            ["report.title"] = "Smart Profiler Raporu",
            ["report.generated"] = "Olusturma Zamani",
            ["report.language"] = "Dil",
            ["report.baseline"] = "Baseline",
            ["report.current"] = "Current",
            ["report.section.comparison"] = "Snapshot Karsilastirma",
            ["report.section.live"] = "Canli Frame Ozeti",
            ["report.section.alerts"] = "Uyarilar",
            ["report.none"] = "Yok",
            ["report.comparison.metric"] = "Metrik",
            ["report.comparison.baseline"] = "Baseline",
            ["report.comparison.current"] = "Current",
            ["report.comparison.delta"] = "Fark",
            ["report.comparison.trend"] = "Yon",
            ["report.trend.improved"] = "Iyilesti",
            ["report.trend.regressed"] = "Geriledi",
            ["report.trend.unchanged"] = "Degismedi",
            ["report.live.frameTime"] = "Frame Suresi",
            ["report.live.fps"] = "FPS",
            ["report.live.gcAlloc"] = "Kare Basi GC Alloc",
            ["report.live.drawCalls"] = "Draw Call",
            ["report.live.batches"] = "Batch",
            ["report.live.heap"] = "Heap Memory",
            ["report.live.physics"] = "Physics",
            ["report.live.camera"] = "Camera",
            ["report.live.animator"] = "Animator",
            ["report.live.gcCollect"] = "GC.Collect",
            ["report.alert.level"] = "Seviye",
            ["report.alert.title"] = "Baslik",
            ["report.alert.message"] = "Mesaj",
            ["report.level.info"] = "Bilgi",
            ["report.level.warning"] = "Uyari",
            ["report.level.critical"] = "Kritik",

            ["playtest.window.title"] = "Playtest Kaydedici",
            ["playtest.header.title"] = "PLAYTEST KAYDEDICI",
            ["playtest.header.body"] = "Play Mode sirasinda oyuncu rotalarini kaydedin ve bunlari sahne isi haritasina donusturun. Darbogazlari, donguleri ve oyuncularin oyalandigi alanlari fark etmek icin kullanin.",
            ["playtest.toolbar.reload"] = "Oturumlari Yenile",
            ["playtest.toolbar.delete"] = "Oturumu Sil",
            ["playtest.recording.active"] = "REC  Play Mode kayit suruyor",
            ["playtest.recording.idle"] = "REC  Play Mode bekleniyor",
            ["playtest.noSessions"] = "Henuz playtest oturumu yok. Veri olusturmak icin Play Mode'a girip hareket edin.",
            ["playtest.controls.title"] = "Oturum Kontrolleri",
            ["playtest.controls.body"] = "Bir kosu secin ve boyali hareket hucrelerinin Scene view'da ne kadar genis gorunecegini ayarlayin.",
            ["playtest.controls.session"] = "Oturum",
            ["playtest.controls.size"] = "Hareket Boyutu",
            ["playtest.controls.hint"] = "Kucuk degerler daha keskin yollar gosterir. Buyuk degerler yakindaki hareketleri daha genis sicak alanlara donusturur.",
            ["playtest.summary.title"] = "Oturum Ozeti",
            ["playtest.summary.scene"] = "Sahne",
            ["playtest.summary.created"] = "Olusturma",
            ["playtest.summary.duration"] = "Sure",
            ["playtest.summary.samples"] = "Hareket Ornegi",
            ["playtest.legend.title"] = "Aciklama",
            ["playtest.legend.low"] = "Dusuk trafik",
            ["playtest.legend.medium"] = "Orta trafik",
            ["playtest.legend.high"] = "En yuksek oyuncu yogunlugu",
            ["playtest.delete.title"] = "Playtest Oturumunu Sil",
            ["playtest.delete.message"] = "Secili playtest oturumu kalici olarak silinsin mi?\n\n{0}  |  {1}",
            ["playtest.delete.confirm"] = "Sil",
            ["playtest.delete.cancel"] = "Iptal",
            ["playtest.session.label"] = "{0}  |  {1}  |  {2} nokta",

            ["scene.window.title"] = "Scene Organizer",
            ["scene.page.title"] = "AKILLI SAHNE DUZENLEYICI",
            ["scene.toolbar.refresh"] = "Yenile",
            ["scene.health"] = "Saglik: {0}",
            ["scene.summary"] = "{0}  |  {1} nesne  |  {2} aktif",
            ["scene.groups.title"] = "Sahne Gruplari",
            ["scene.groups.subtitle"] = "Aktif sahne icin otomatik gruplama ve renk kodlama.",
            ["scene.group.row"] = "{0} nesne  |  {1} aktif  |  {2} ucgen",
            ["scene.metric.polygons"] = "Poligon",
            ["scene.metric.polygons.hint"] = "Gorunen mesh'lerden tahmini ucgen sayisi.",
            ["scene.metric.lights"] = "Aktif Isiklar",
            ["scene.metric.lights.hint"] = "Hierarchy icindeki realtime ve etkin isiklar.",
            ["scene.metric.colliders"] = "Collider",
            ["scene.metric.colliders.hint"] = "Aktif sahnedeki fizik yuzey sayisi.",
            ["scene.metric.renderers"] = "Renderer",
            ["scene.metric.renderers.hint"] = "Gorunen render component sayisi.",
            ["scene.metric.rigidbodies"] = "Rigidbody",
            ["scene.metric.rigidbodies.hint"] = "Dinamik fizik govdeleri.",
            ["scene.metric.canvas"] = "Canvas",
            ["scene.metric.canvas.hint"] = "UI kok canvas sayisi.",
            ["scene.health.good"] = "Iyi",
            ["scene.health.warning"] = "Uyari",
            ["scene.health.critical"] = "Kritik",
            ["scene.group.lighting"] = "Isiklandirma",
            ["scene.group.geometry"] = "Geometri",
            ["scene.group.gameplay"] = "Oynanis",
            ["scene.group.ui"] = "UI",
            ["scene.group.audio"] = "Ses",
            ["scene.group.vfx"] = "VFX",
            ["scene.group.cameras"] = "Kameralar",
            ["scene.group.utility"] = "Yardimci",
            ["scene.untitled"] = "Adsiz Sahne",

            ["alert.gc.title"] = "Neredeyse Her Frame GC Allocation Var",
            ["alert.gc.message"] = "Orneklenen frame'lerin %50'sinden fazlasi cop uretiyor. Update() icinde obje olusturuyor veya 'new' kullaniyor olabilirsiniz. Object pooling dusunun.",
            ["alert.physics.title"] = "Physics Frame Suresini Domine Ediyor",
            ["alert.physics.message"] = "Physics toplam frame suresinin %{0:F0}'ini kullaniyor. Project Settings > Time altinda Fixed Timestep degerini 0.02'den 0.04'e cikarmayi dusunebilirsiniz.",
            ["alert.batching.title"] = "Dusuk Batching Orani (Oneri)",
            ["alert.batching.message"] = "Draw call'larinizin %{0:F0}'i unbatched. Ayni materyali kullanan statik objelerde Static Batching veya GPU Instancing CPU suresinden tasarruf saglayabilir.",
            ["alert.memory.title"] = "Potansiyel Memory Leak",
            ["alert.memory.message"] = "Heap kullanimi son 300 frame boyunca {0:F1} MB artti. Destroy edilmesi gereken objeler referanslar yuzunden yasiyor olabilir."
        };

        private static SmartProfilerLanguage _currentLanguage = LoadLanguage();

        public static event Action LanguageChanged;

        public static SmartProfilerLanguage CurrentLanguage
        {
            get { return _currentLanguage; }
            set
            {
                if (_currentLanguage == value)
                {
                    return;
                }

                _currentLanguage = value;
                SaveLanguage(value);
                LanguageChanged?.Invoke();
            }
        }

        public static string Get(string key)
        {
            string value;
            if (GetTable().TryGetValue(key, out value))
            {
                return value;
            }

            if (English.TryGetValue(key, out value))
            {
                return value;
            }

            return key;
        }

        public static string Format(string key, params object[] args)
        {
            return string.Format(Get(key), args);
        }

        public static string GetLanguageDisplayName(SmartProfilerLanguage language)
        {
            switch (language)
            {
                case SmartProfilerLanguage.Turkish:
                    return Get("language.turkish");
                default:
                    return Get("language.english");
            }
        }

        private static Dictionary<string, string> GetTable()
        {
            return _currentLanguage == SmartProfilerLanguage.Turkish ? Turkish : English;
        }

        private static SmartProfilerLanguage LoadLanguage()
        {
#if UNITY_EDITOR
            return (SmartProfilerLanguage)EditorPrefs.GetInt(LanguagePrefKey, (int)SmartProfilerLanguage.English);
#else
            return (SmartProfilerLanguage)PlayerPrefs.GetInt(LanguagePrefKey, (int)SmartProfilerLanguage.English);
#endif
        }

        private static void SaveLanguage(SmartProfilerLanguage language)
        {
#if UNITY_EDITOR
            EditorPrefs.SetInt(LanguagePrefKey, (int)language);
#else
            PlayerPrefs.SetInt(LanguagePrefKey, (int)language);
            PlayerPrefs.Save();
#endif
        }
    }
}
