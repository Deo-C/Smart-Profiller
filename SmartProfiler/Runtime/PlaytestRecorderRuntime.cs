using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SmartProfiler.Runtime
{
    [Serializable]
    public struct PlaytestPoint
    {
        public Vector3 Position;
        public float Time;
    }

    [Serializable]
    public class PlaytestSessionData
    {
        public string SessionId;
        public string SceneName;
        public string CreatedAtIsoUtc;
        public float DurationSeconds;
        public List<PlaytestPoint> MovementPoints = new List<PlaytestPoint>();
    }

    public class PlaytestRecorderRuntime : MonoBehaviour
    {
        private const float SampleInterval = 0.2f;

        private static PlaytestRecorderRuntime _instance;

        private PlaytestSessionData _session;
        private Transform _trackedTarget;
        private float _nextSampleTime;
        private Vector3 _lastKnownPosition;

        public static bool IsRecording
        {
            get { return _instance != null && _instance._session != null; }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (_instance != null)
            {
                return;
            }

            var go = new GameObject("SmartProfiler_PlaytestRecorder");
            go.hideFlags = HideFlags.HideAndDontSave;
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<PlaytestRecorderRuntime>();
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                DestroyImmediate(gameObject);
                return;
            }

            _instance = this;
            StartSession();
            SceneManager.sceneLoaded += HandleSceneLoaded;
            Application.quitting += HandleApplicationQuitting;
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                SaveSession();
                SceneManager.sceneLoaded -= HandleSceneLoaded;
                Application.quitting -= HandleApplicationQuitting;
                _instance = null;
            }
        }

        private void HandleApplicationQuitting()
        {
            SaveSession();
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (_session != null)
            {
                _session.SceneName = scene.name;
            }

            _trackedTarget = null;
        }

        private void StartSession()
        {
            _session = new PlaytestSessionData
            {
                SessionId = Guid.NewGuid().ToString("N"),
                SceneName = SceneManager.GetActiveScene().name,
                CreatedAtIsoUtc = DateTime.UtcNow.ToString("o"),
                DurationSeconds = 0f
            };

            _nextSampleTime = Time.unscaledTime + SampleInterval;
            _trackedTarget = null;
        }

        private void Update()
        {
            if (_session == null)
            {
                return;
            }

            _session.DurationSeconds += Time.unscaledDeltaTime;

            EnsureTrackedTarget();
            SampleMovement();
        }

        private void EnsureTrackedTarget()
        {
            if (_trackedTarget != null)
            {
                return;
            }

            GameObject candidate = GameObject.FindWithTag("Player");
            if (candidate == null)
            {
                candidate = FindByScriptName("CharTopDown");
            }

            if (candidate == null)
            {
                candidate = FindByScriptName("Char");
            }

            if (candidate == null)
            {
                GameObject[] roots = SceneManager.GetActiveScene().GetRootGameObjects();
                for (int i = 0; i < roots.Length && candidate == null; i++)
                {
                    candidate = FindLikelyPlayer(roots[i].transform);
                }
            }

            if (candidate != null)
            {
                _trackedTarget = candidate.transform;
                _lastKnownPosition = _trackedTarget.position;
            }
        }

        private GameObject FindLikelyPlayer(Transform root)
        {
            string lowerName = root.name.ToLowerInvariant();
            if ((lowerName.Contains("player") || lowerName.Contains("char")) &&
                (root.GetComponent<Rigidbody2D>() != null || root.GetComponent<Rigidbody>() != null))
            {
                return root.gameObject;
            }

            for (int i = 0; i < root.childCount; i++)
            {
                GameObject result = FindLikelyPlayer(root.GetChild(i));
                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }

        private GameObject FindByScriptName(string typeName)
        {
            MonoBehaviour[] behaviours = GetAllMonoBehaviours();
            for (int i = 0; i < behaviours.Length; i++)
            {
                MonoBehaviour behaviour = behaviours[i];
                if (behaviour == null)
                {
                    continue;
                }

                if (behaviour.GetType().Name == typeName)
                {
                    return behaviour.gameObject;
                }
            }

            return null;
        }

        private MonoBehaviour[] GetAllMonoBehaviours()
        {
#if UNITY_2023_1_OR_NEWER
            return FindObjectsByType<MonoBehaviour>();
#else
            return FindObjectsOfType<MonoBehaviour>();
#endif
        }

        private void SampleMovement()
        {
            if (_trackedTarget == null || Time.unscaledTime < _nextSampleTime)
            {
                return;
            }

            _nextSampleTime = Time.unscaledTime + SampleInterval;
            _lastKnownPosition = _trackedTarget.position;
            _session.MovementPoints.Add(new PlaytestPoint
            {
                Position = _lastKnownPosition,
                Time = Time.unscaledTime
            });
        }


        private void SaveSession()
        {
            if (_session == null)
            {
                return;
            }

            if (_session.MovementPoints.Count == 0)
            {
                _session = null;
                return;
            }

            string directory = Path.Combine(Directory.GetCurrentDirectory(), "ProjectSettings", "SmartProfilerPlaytests");
            Directory.CreateDirectory(directory);
            string path = Path.Combine(directory, _session.SessionId + ".json");
            File.WriteAllText(path, JsonUtility.ToJson(_session, true));
            _session = null;
        }
    }
}
