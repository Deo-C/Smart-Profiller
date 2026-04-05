using System;
using Unity.Profiling;
using UnityEngine;

namespace SmartProfiler.Runtime
{
    [Serializable]
    public struct FrameSample
    {
        public int FrameIndex;
        public float FrameTimeMs;
        public long GcAllocBytes;
        public int DrawCalls;
        public int Batches;
        
        // Subsystems (Ms)
        public float PhysicsTimeMs;
        public float CameraRenderMs;
        public float AnimatorUpdateMs;
        public float GcCollectMs;
        
        // Memory
        public long TotalHeapBytes;
    }

    [ExecuteAlways]
    public class DataCollector : MonoBehaviour
    {
        public const int Capacity = 300;
        
        private ProfilerRecorder _mainThreadRecorder;
        private ProfilerRecorder _gcAllocRecorder;
        private ProfilerRecorder _drawCallsRecorder;
        private ProfilerRecorder _batchesRecorder;
        
        private ProfilerRecorder _physicsRecorder;
        private ProfilerRecorder _cameraRecorder;
        private ProfilerRecorder _animatorRecorder;
        private ProfilerRecorder _gcCollectRecorder;
        
        private ProfilerRecorder _heapMemoryRecorder;

        private FrameSample[] _ringBuffer = new FrameSample[Capacity];
        private int _writeIndex = 0;
        private int _count = 0;

        public event Action<FrameSample> OnFrameRecorded;

        private void OnEnable()
        {
            _mainThreadRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "Main Thread");
            _gcAllocRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Allocated In Frame");
            _drawCallsRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Draw Calls Count");
            _batchesRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Batches Count");

            _physicsRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Physics, "Physics.Processing");
            _cameraRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Camera.Render");
            _animatorRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "Animator.Update");
            _gcCollectRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "GC.Collect");

            _heapMemoryRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "Total Used Memory");
        }

        private void OnDisable()
        {
            _mainThreadRecorder.Dispose();
            _gcAllocRecorder.Dispose();
            _drawCallsRecorder.Dispose();
            _batchesRecorder.Dispose();
            
            _physicsRecorder.Dispose();
            _cameraRecorder.Dispose();
            _animatorRecorder.Dispose();
            _gcCollectRecorder.Dispose();

            _heapMemoryRecorder.Dispose();
        }

        private void Update()
        {
            if (!_mainThreadRecorder.Valid) return;

            var sample = new FrameSample
            {
                FrameIndex = Time.frameCount,
                FrameTimeMs = _mainThreadRecorder.LastValue * 1e-6f,
                GcAllocBytes = _gcAllocRecorder.LastValue,
                DrawCalls = (int)_drawCallsRecorder.LastValue,
                Batches = (int)_batchesRecorder.LastValue,
                
                PhysicsTimeMs = _physicsRecorder.LastValue * 1e-6f,
                CameraRenderMs = _cameraRecorder.LastValue * 1e-6f,
                AnimatorUpdateMs = _animatorRecorder.LastValue * 1e-6f,
                GcCollectMs = _gcCollectRecorder.LastValue * 1e-6f,
                
                TotalHeapBytes = _heapMemoryRecorder.LastValue
            };

            _ringBuffer[_writeIndex] = sample;
            _writeIndex = (_writeIndex + 1) % Capacity;
            if (_count < Capacity) _count++;

            OnFrameRecorded?.Invoke(sample);
        }

        public FrameSample[] GetLastN(int n)
        {
            int itemsToRetrieve = Mathf.Min(n, _count);
            var result = new FrameSample[itemsToRetrieve];
            for (int i = 0; i < itemsToRetrieve; i++)
            {
                int index = (_writeIndex - itemsToRetrieve + i + Capacity) % Capacity;
                result[i] = _ringBuffer[index];
            }
            return result;
        }
    }
}
