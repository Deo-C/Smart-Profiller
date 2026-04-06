# Smart Profiler Dashboard

A Unity Editor extension that gives solo indie developers and small teams a **real-time performance analysis window** without having to learn the Unity Profiler internals.

![Unity](https://img.shields.io/badge/Unity-2022.3%2B-black?logo=unity) ![License](https://img.shields.io/badge/license-MIT-green) ![Status](https://img.shields.io/badge/status-active-brightgreen)

---

## Why This Exists

Unity's built-in Profiler is powerful but overwhelming. Finding a frame spike means recording a session, scrubbing through thousands of frames, cross-referencing multiple windows — easily 15-20 minutes per investigation.

Smart Profiler Dashboard reduces that to **under 2 minutes**:
- Spikes are detected automatically and ranked by severity
- The most expensive systems in that frame are listed immediately
- Human-readable warnings tell you *what to do*, not just what happened
- Snapshots let you compare before/after an optimization in seconds

---

## Features

### Real-Time Metrics
- Frame time (ms) with 30fps / 60fps threshold indicators
- GC allocation per frame (zero-alloc hot path)
- Draw call count with batching efficiency ratio
- All metrics updated every frame via `ProfilerRecorder` API

### Spike Detection
- Rolling average over last 60 frames
- Spike threshold: `frameTime > rollingAverage × 2.0` AND `frameTime > 33.3ms`
- On spike: captures timestamp, frame index, and top 5 heaviest systems
- Stores up to 50 spike records per session

### Chart Panel (separate window)
- Three stacked filled-area charts: frame time, GC alloc, draw calls
- Spike points marked in red directly on the frame time chart
- 30fps threshold line on frame time chart
- Adjustable visible range: last 60 / 150 / 300 frames
- Drawn via Unity GL — zero third-party dependencies

### Snapshot System
- Capture session snapshots as `ScriptableObject` assets
- Stores avg, P95, max frame time, total GC alloc, spike count
- Compare any two snapshots side by side
- Severity classification: `Improved / Neutral / Degraded / Critical`
- Output: human-readable summary line, e.g. `"Frame time +3.2ms (+18%) — Degraded. GC alloc +120KB. Spikes +2."`

### Team Features
- `ProfilerSnapshot` assets are version-control friendly (YAML ScriptableObjects)
- Snapshots can be committed alongside builds for historical comparison
- `SnapshotComparer` is a static class — trivially usable in CI scripts

---

## Requirements

- Unity **2022.3 LTS** or higher (tested on 2022.3, 2023.x, Unity 6)
- No third-party packages
- No unsafe code
- `.NET Standard 2.1` compatible

---

## Installation

1. Copy the `SmartProfiler` folder into your project's `Assets/` directory:
```
Assets/
└── SmartProfiler/
    ├── Editor/
    ├── Runtime/
    └── Data/
```

2. Unity will recompile automatically. No package manifest changes needed.

3. Open the dashboard:
```
Unity Menu → Smart Profiler → Open Dashboard
Unity Menu → Smart Profiler → Open Charts
```

> **Note:** Open Dashboard first. It creates the `DataCollector` GameObject that the Charts window reads from.

---

## Architecture

```
Runtime/
└── DataCollector.cs
      MonoBehaviour (ExecuteAlways)
      Runs ProfilerRecorder for 3 categories each frame.
      Writes into a RingBuffer<FrameSample>[300].
      Fires Action<FrameSample> OnFrameRecorded.

Editor/
├── SmartProfilerWindow.cs      Main EditorWindow — metrics, alerts, spike list, snapshots
├── ProfilerChartWindow.cs      Separate EditorWindow — GL-drawn chart panel
├── SpikeDetector.cs            Subscribes to DataCollector, fires Action<SpikeRecord> on spike
├── SnapshotComparer.cs         Static class — diffs two ProfilerSnapshot assets
└── ProfilerDataBridge.cs       [InitializeOnLoad] — auto-creates DataCollector on Play Mode enter

Data/
├── ProfilerSnapshot.cs         ScriptableObject — one saved session
└── *.asset                     Snapshot assets saved here
```

### Data Flow

```
Play Mode enters
    → ProfilerDataBridge creates DataCollector GameObject (HideFlags.DontDestroyOnLoad)
    → DataCollector.Update() samples ProfilerRecorder every frame
    → fires OnFrameRecorded(FrameSample)
        → SpikeDetector checks rolling average → fires OnSpikeDetected if threshold exceeded
        → SmartProfilerWindow updates metric labels
        → ProfilerChartWindow pushes to ring buffers → MarkDirtyRepaint()
Play Mode exits
    → ProfilerDataBridge destroys DataCollector GameObject
    → Chart windows show "Waiting for Play Mode..."
```

---

## API Reference

### FrameSample
```csharp
public struct FrameSample
{
    public int    frameIndex;
    public float  frameTimeMs;
    public long   gcAllocBytes;
    public int    drawCalls;
    public double timestamp;
}
```

### DataCollector
```csharp
// Get last N frames from any Editor script
FrameSample[] recent = DataCollector.Instance.GetLastN(60);

// Subscribe to live frame data
DataCollector.Instance.OnFrameRecorded += OnFrame;
```

### ProfilerSnapshot
```csharp
// Create a snapshot from current session data
var snapshot = ProfilerSnapshot.CreateFrom(
    collector.GetLastN(300),
    spikeRecords,
    label: "build_v12"
);
AssetDatabase.CreateAsset(snapshot, "Assets/SmartProfiler/Data/build_v12.asset");
```

### SnapshotComparer
```csharp
var baseline = AssetDatabase.LoadAssetAtPath<ProfilerSnapshot>("Assets/.../build_v11.asset");
var current  = AssetDatabase.LoadAssetAtPath<ProfilerSnapshot>("Assets/.../build_v12.asset");

CompareResult result = SnapshotComparer.Compare(baseline, current);
Debug.Log(SnapshotComparer.FormatSummary(result));
// → "Frame time -4.1ms (-22%) — Improved. GC alloc -2.3KB. Spikes -9."
```

---

## Snapshot Comparison Workflow

1. Before your optimization session, open Dashboard and click **Take Snapshot** → saved as `YYYY-MM-DD_HH-mm.asset`
2. Make your changes, run the game in Play Mode
3. Click **Take Snapshot** again
4. Click **Compare...**, select the baseline snapshot
5. Result appears in the comparison panel with severity rating

Snapshots are plain `ScriptableObject` YAML files. Commit them to version control to maintain a performance history across the project lifetime.

---

## Known Limitations

- `ProfilerRecorder` only captures data while the game is in **Play Mode** — Edit Mode profiling is not supported
- Top-5 heaviest systems in spike records are sampled from `ProfilerRecorder` metadata; deep call stacks require Unity's full Profiler for investigation
- GL chart rendering redraws every `MarkDirtyRepaint()` call — on very high frame rates (500+ fps in editor) this may itself consume measurable CPU time; consider capping editor frame rate via `Application.targetFrameRate` during profiling sessions

---

## Contributing

Pull requests are welcome. For larger changes please open an issue first.
New systems can then be developed using the same architecture.


---

## License

MIT — see [LICENSE](LICENSE) for details.
