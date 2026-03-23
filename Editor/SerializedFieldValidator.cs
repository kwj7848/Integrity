using System;
using System.Collections.Generic;
using System.Reflection;
using Integrity;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Integrity.Editor
{
    [InitializeOnLoad]
    public class SerializedFieldValidator : IPreprocessBuildWithReport, IProcessSceneWithReport
    {
        public int callbackOrder => 0;

        static readonly Dictionary<string, List<string>> _prefabErrorCache = new();
        static bool _cacheInitialized;

        static readonly Dictionary<Type, FieldInfo[]> _fieldInfoCache = new();

        static SerializedFieldValidator()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.ExitingEditMode)
                return;

            var settings = IntegritySettings.Instance;
            if (!settings.validateSerializeFields || !settings.blockPlayOnMissing)
                return;

            var errors = new List<(string message, UnityEngine.Object context)>();
            ValidateLoadedScenes(errors);
            CollectPrefabErrors(errors);

            if (errors.Count > 0)
            {
                foreach (var (message, context) in errors)
                    Debug.LogError(message, context);

                EditorApplication.isPlaying = false;
            }
        }

        public void OnPreprocessBuild(BuildReport report)
        {
            var settings = IntegritySettings.Instance;
            if (!settings.validateSerializeFields || !settings.blockBuildOnMissing)
                return;

            var errors = new List<(string message, UnityEngine.Object context)>();
            CollectPrefabErrors(errors);

            if (errors.Count > 0)
            {
                foreach (var (message, context) in errors)
                    Debug.LogError(message, context);

                throw new BuildFailedException(
                    $"Found {errors.Count} unassigned SerializeField(s) in prefabs. Check the Console.");
            }
        }

        public void OnProcessScene(Scene scene, BuildReport report)
        {
            if (report == null)
                return;

            var settings = IntegritySettings.Instance;
            if (!settings.validateSerializeFields || !settings.blockBuildOnMissing)
                return;

            var errors = new List<(string message, UnityEngine.Object context)>();
            ValidateScene(scene, errors);

            if (errors.Count > 0)
            {
                foreach (var (message, context) in errors)
                    Debug.LogError(message, context);

                throw new BuildFailedException(
                    $"Found {errors.Count} unassigned SerializeField(s). Check the Console.");
            }
        }

        static void EnsureCacheInitialized()
        {
            if (_cacheInitialized)
                return;

            _cacheInitialized = true;

            _prefabErrorCache.Clear();
            var guids = AssetDatabase.FindAssets("t:Prefab");
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                ValidatePrefabAtPath(path);
            }
        }

        static void CollectPrefabErrors(List<(string message, UnityEngine.Object context)> errors)
        {
            EnsureCacheInitialized();

            foreach (var kvp in _prefabErrorCache)
            {
                if (kvp.Value.Count == 0)
                    continue;

                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(kvp.Key);
                foreach (var msg in kvp.Value)
                    errors.Add((msg, prefab));
            }
        }

        internal static void OnPrefabChanged(string[] importedPaths, string[] deletedPaths, string[] movedFromPaths, string[] movedToPaths)
        {
            if (!_cacheInitialized)
                return;

            foreach (var path in deletedPaths)
                if (path.EndsWith(".prefab"))
                    _prefabErrorCache.Remove(path);

            foreach (var path in movedFromPaths)
                if (path.EndsWith(".prefab"))
                    _prefabErrorCache.Remove(path);

            foreach (var path in importedPaths)
                if (path.EndsWith(".prefab"))
                    ValidatePrefabAtPath(path);

            foreach (var path in movedToPaths)
                if (path.EndsWith(".prefab"))
                    ValidatePrefabAtPath(path);
        }

        static void ValidatePrefabAtPath(string path)
        {
            var errors = new List<string>();
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab != null)
            {
                foreach (var mb in prefab.GetComponentsInChildren<MonoBehaviour>(true))
                {
                    if (mb == null)
                        continue;

                    var type = mb.GetType();
                    if (type.Assembly.GetName().Name != "Assembly-CSharp")
                        continue;

                    ValidateFieldsCached(mb, type, path, errors);
                }
            }

            _prefabErrorCache[path] = errors;
        }

        static FieldInfo[] GetValidatableFields(Type type)
        {
            if (_fieldInfoCache.TryGetValue(type, out var cached))
                return cached;

            var result = new List<FieldInfo>();
            var t = type;
            while (t != null && t != typeof(MonoBehaviour))
            {
                if (t.Assembly.GetName().Name == "Assembly-CSharp")
                {
                    var fields = t.GetFields(
                        BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);

                    foreach (var field in fields)
                    {
                        if (!field.IsDefined(typeof(SerializeField), false))
                            continue;
                        if (field.FieldType.IsValueType)
                            continue;
                        if (!typeof(UnityEngine.Object).IsAssignableFrom(field.FieldType))
                            continue;
                        if (field.IsDefined(typeof(HideInInspector), false))
                            continue;
                        if (field.IsDefined(typeof(AllowEmptyAttribute), false))
                            continue;
                        result.Add(field);
                    }
                }

                t = t.BaseType;
            }

            var array = result.ToArray();
            _fieldInfoCache[type] = array;
            return array;
        }

        static void ValidateFieldsCached(MonoBehaviour mb, Type type, string location, List<string> errors)
        {
            foreach (var field in GetValidatableFields(type))
            {
                var value = field.GetValue(mb);
                if (value == null || (value is UnityEngine.Object obj && obj == null))
                {
                    var goPath = GetGameObjectPath(mb.gameObject);
                    errors.Add(
                        $"[Missing SerializeField] [{location}/{goPath}] " +
                        $"{type.Name}.{field.Name} ({field.FieldType.Name}) is not assigned.");
                }
            }
        }

        static void ValidateLoadedScenes(List<(string message, UnityEngine.Object context)> errors)
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (scene.isLoaded)
                    ValidateScene(scene, errors);
            }
        }

        static void ValidateScene(Scene scene, List<(string message, UnityEngine.Object context)> errors)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var mb in root.GetComponentsInChildren<MonoBehaviour>(true))
                {
                    if (mb == null)
                        continue;

                    var type = mb.GetType();
                    if (type.Assembly.GetName().Name != "Assembly-CSharp")
                        continue;

                    ValidateFields(mb, type, scene.name, errors);
                }
            }
        }

        static void ValidateFields(MonoBehaviour mb, Type type, string location, List<(string message, UnityEngine.Object context)> errors)
        {
            foreach (var field in GetValidatableFields(type))
            {
                var value = field.GetValue(mb);
                if (value == null || (value is UnityEngine.Object obj && obj == null))
                {
                    var goPath = GetGameObjectPath(mb.gameObject);
                    errors.Add((
                        $"[Missing SerializeField] [{location}/{goPath}] " +
                        $"{type.Name}.{field.Name} ({field.FieldType.Name}) is not assigned.",
                        mb));
                }
            }
        }

        static string GetGameObjectPath(GameObject go)
        {
            var path = go.name;
            var t = go.transform.parent;
            while (t != null)
            {
                path = t.name + "/" + path;
                t = t.parent;
            }
            return path;
        }
    }

    public class SerializedFieldValidatorPostprocessor : AssetPostprocessor
    {
        static void OnPostprocessAllAssets(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            SerializedFieldValidator.OnPrefabChanged(importedAssets, deletedAssets, movedFromAssetPaths, movedAssets);
        }
    }
}
