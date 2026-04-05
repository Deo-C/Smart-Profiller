using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace SmartProfiler.Editor
{
    public enum SceneHealthLevel
    {
        Good,
        Warning,
        Critical
    }

    public struct SceneMetric
    {
        public string Label;
        public string Value;
        public string Hint;
        public SceneHealthLevel Level;
    }

    public class SceneGroupSummary
    {
        public string Name;
        public int ObjectCount;
        public int ActiveCount;
        public int TriangleCount;
    }

    public class SceneOrganizerReport
    {
        public string SceneName;
        public int TotalObjects;
        public int ActiveObjects;
        public int RootObjects;
        public int TriangleCount;
        public int ActiveLights;
        public int ColliderCount;
        public int RendererCount;
        public int RigidbodyCount;
        public int AudioSourceCount;
        public int CanvasCount;
        public int CameraCount;
        public readonly List<SceneMetric> Metrics = new List<SceneMetric>();
        public readonly List<SceneGroupSummary> Groups = new List<SceneGroupSummary>();
    }

    public static class SceneOrganizerAnalyzer
    {
        public static SceneOrganizerReport AnalyzeActiveScene()
        {
            Scene scene = SceneManager.GetActiveScene();
            var report = new SceneOrganizerReport
            {
                SceneName = string.IsNullOrEmpty(scene.name) ? "Untitled Scene" : scene.name
            };

            var groups = new Dictionary<string, SceneGroupSummary>();
            GameObject[] roots = scene.GetRootGameObjects();
            report.RootObjects = roots.Length;

            for (int i = 0; i < roots.Length; i++)
            {
                Traverse(roots[i], report, groups);
            }

            foreach (var pair in groups)
            {
                report.Groups.Add(pair.Value);
            }

            report.Groups.Sort((a, b) =>
            {
                int objectCompare = b.ObjectCount.CompareTo(a.ObjectCount);
                if (objectCompare != 0)
                {
                    return objectCompare;
                }

                return b.TriangleCount.CompareTo(a.TriangleCount);
            });

            report.Metrics.Add(CreateMetric("Polygons", report.TriangleCount.ToString("N0"), "Approx. triangle count from visible meshes.", 150000, 400000));
            report.Metrics.Add(CreateMetric("Active Lights", report.ActiveLights.ToString(), "Realtime and enabled lights in hierarchy.", 6, 12));
            report.Metrics.Add(CreateMetric("Colliders", report.ColliderCount.ToString(), "Physics surface count in the active scene.", 150, 400));
            report.Metrics.Add(CreateMetric("Renderers", report.RendererCount.ToString(), "Visible render components.", 200, 600));
            report.Metrics.Add(CreateMetric("Rigidbodies", report.RigidbodyCount.ToString(), "Dynamic physics bodies.", 50, 150));
            report.Metrics.Add(CreateMetric("Canvas", report.CanvasCount.ToString(), "UI root canvases.", 4, 10));

            return report;
        }

        private static void Traverse(GameObject go, SceneOrganizerReport report, Dictionary<string, SceneGroupSummary> groups)
        {
            report.TotalObjects++;
            if (go.activeInHierarchy)
            {
                report.ActiveObjects++;
            }

            string groupName = ResolveGroup(go);
            SceneGroupSummary group;
            if (!groups.TryGetValue(groupName, out group))
            {
                group = new SceneGroupSummary { Name = groupName };
                groups.Add(groupName, group);
            }

            group.ObjectCount++;
            if (go.activeInHierarchy)
            {
                group.ActiveCount++;
            }

            Light light = go.GetComponent<Light>();
            if (light != null && light.enabled && go.activeInHierarchy)
            {
                report.ActiveLights++;
            }

            Light2D light2D = go.GetComponent<Light2D>();
            if (light2D != null && light2D.enabled && go.activeInHierarchy)
            {
                report.ActiveLights++;
            }

            report.ColliderCount += go.GetComponents<Collider>().Length;
            report.ColliderCount += go.GetComponents<Collider2D>().Length;

            Renderer renderer = go.GetComponent<Renderer>();
            if (renderer != null)
            {
                report.RendererCount++;
            }

            report.RigidbodyCount += go.GetComponents<Rigidbody>().Length;
            report.RigidbodyCount += go.GetComponents<Rigidbody2D>().Length;

            AudioSource audioSource = go.GetComponent<AudioSource>();
            if (audioSource != null)
            {
                report.AudioSourceCount++;
            }

            Canvas canvas = go.GetComponent<Canvas>();
            if (canvas != null)
            {
                report.CanvasCount++;
            }

            Camera camera = go.GetComponent<Camera>();
            if (camera != null)
            {
                report.CameraCount++;
            }

            int triangleCount = GetTriangleCount(go);
            report.TriangleCount += triangleCount;
            group.TriangleCount += triangleCount;

            Transform transform = go.transform;
            for (int i = 0; i < transform.childCount; i++)
            {
                Traverse(transform.GetChild(i).gameObject, report, groups);
            }
        }

        private static int GetTriangleCount(GameObject go)
        {
            int triangles = 0;

            MeshFilter meshFilter = go.GetComponent<MeshFilter>();
            if (meshFilter != null && meshFilter.sharedMesh != null)
            {
                triangles += meshFilter.sharedMesh.triangles.Length / 3;
            }

            SkinnedMeshRenderer skinnedMesh = go.GetComponent<SkinnedMeshRenderer>();
            if (skinnedMesh != null && skinnedMesh.sharedMesh != null)
            {
                triangles += skinnedMesh.sharedMesh.triangles.Length / 3;
            }

            SpriteRenderer spriteRenderer = go.GetComponent<SpriteRenderer>();
            if (spriteRenderer != null && spriteRenderer.sprite != null)
            {
                triangles += spriteRenderer.sprite.triangles.Length / 3;
            }

            Tilemap tilemap = go.GetComponent<Tilemap>();
            if (tilemap != null)
            {
                triangles += EstimateTilemapTriangles(tilemap);
            }

            return triangles;
        }

        private static int EstimateTilemapTriangles(Tilemap tilemap)
        {
            int triangles = 0;
            BoundsInt bounds = tilemap.cellBounds;

            foreach (Vector3Int position in bounds.allPositionsWithin)
            {
                Sprite sprite = tilemap.GetSprite(position);
                if (sprite != null)
                {
                    triangles += sprite.triangles.Length / 3;
                }
            }

            return triangles;
        }

        private static string ResolveGroup(GameObject go)
        {
            if (go.GetComponent<Light>() != null || go.GetComponent<Light2D>() != null || go.GetComponent<ReflectionProbe>() != null)
            {
                return "Lighting";
            }

            if (go.GetComponent<Canvas>() != null || go.GetComponent<UnityEngine.UI.Graphic>() != null)
            {
                return "UI";
            }

            if (go.GetComponent<Camera>() != null || go.GetComponent<AudioListener>() != null)
            {
                return "Cameras";
            }

            if (go.GetComponent<ParticleSystem>() != null || go.GetComponent<TrailRenderer>() != null || go.GetComponent<LineRenderer>() != null)
            {
                return "VFX";
            }

            if (go.GetComponent<AudioSource>() != null)
            {
                return "Audio";
            }

            if (go.GetComponent<Collider>() != null ||
                go.GetComponent<Collider2D>() != null ||
                go.GetComponent<Rigidbody>() != null ||
                go.GetComponent<Rigidbody2D>() != null ||
                go.GetComponent<CharacterController>() != null)
            {
                return "Gameplay";
            }

            if (go.GetComponent<MeshRenderer>() != null ||
                go.GetComponent<SkinnedMeshRenderer>() != null ||
                go.GetComponent<SpriteRenderer>() != null ||
                go.GetComponent<TilemapRenderer>() != null ||
                go.GetComponent<Terrain>() != null)
            {
                return "Geometry";
            }

            return "Utility";
        }

        private static SceneMetric CreateMetric(string label, string value, string hint, int warningThreshold, int criticalThreshold)
        {
            int numericValue;
            int.TryParse(value.Replace(",", string.Empty), out numericValue);

            SceneHealthLevel level = SceneHealthLevel.Good;
            if (numericValue >= criticalThreshold)
            {
                level = SceneHealthLevel.Critical;
            }
            else if (numericValue >= warningThreshold)
            {
                level = SceneHealthLevel.Warning;
            }

            return new SceneMetric
            {
                Label = label,
                Value = value,
                Hint = hint,
                Level = level
            };
        }
    }
}
