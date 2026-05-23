using System;
using System.Collections.Generic;
using UIAutoBind;
using UnityEditor;
using UnityEngine;

namespace UIAutoBind.Editor
{
    public static class UIAutoBinderScanner
    {
        // 在此配置项目中 UI Prefab 所在的搜索路径
        public static readonly string[] UIPrefabSearchPaths = new[]
        {
            "Assets/GameMain/UI/UIForms",
        };

        [MenuItem("Tools/UIAutoBind/Scan Current Prefab", false, 50)]
        private static void ScanCurrentPrefab()
        {
            var go = Selection.activeGameObject;
            if (go == null)
            {
                Debug.LogWarning("[UIAutoBinder] No GameObject selected.");
                return;
            }

            var binder = go.GetComponent<UIAutoBinder>();
            if (binder == null)
            {
                Debug.LogWarning("[UIAutoBinder] Selected GameObject has no UIAutoBinder component.");
                return;
            }

            ScanAndApply(binder);
            EditorUtility.SetDirty(go);
            Debug.Log($"[UIAutoBinder] Scan complete: {binder.Bindings.Count} binding(s) on '{go.name}'.");
        }

        [MenuItem("Tools/UIAutoBind/Scan All Prefabs", false, 51)]
        private static void ScanAllPrefabs()
        {
            string[] guids = AssetDatabase.FindAssets("t:Prefab");
            int scanned = 0;
            int total = guids.Length;

            try
            {
                for (int i = 0; i < guids.Length; i++)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                    EditorUtility.DisplayProgressBar("UIAutoBind - Scan All Prefabs",
                        path, (float)i / total);

                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    if (prefab == null) continue;

                    var binder = prefab.GetComponent<UIAutoBinder>();
                    if (binder == null) continue;

                    var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                    if (instance == null) continue;
                    var instanceBinder = instance.GetComponent<UIAutoBinder>();
                    if (instanceBinder != null)
                    {
                        RunScan(instanceBinder);
                        PrefabUtility.SaveAsPrefabAsset(instance, path);
                        scanned++;
                    }
                    UnityEngine.Object.DestroyImmediate(instance);
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            AssetDatabase.Refresh();
            Debug.Log($"[UIAutoBinder] Scanned {scanned} prefab(s) with UIAutoBinder.");
        }

        [MenuItem("Tools/UIAutoBind/Setup All UI Form Prefabs", false, 53)]
        private static void SetupAllUIFormPrefabs()
        {
            string[] guids = AssetDatabase.FindAssets("t:Prefab", UIPrefabSearchPaths);
            int added = 0;
            int total = guids.Length;

            try
            {
                for (int i = 0; i < guids.Length; i++)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                    EditorUtility.DisplayProgressBar("UIAutoBind - Setup UI Forms",
                        path, (float)i / total);

                    var instance = PrefabUtility.LoadPrefabContents(path);
                    if (instance == null) continue;

                    var binder = instance.GetComponent<UIAutoBinder>();
                    if (binder == null)
                    {
                        binder = instance.AddComponent<UIAutoBinder>();
                        added++;
                    }

                    // RunScan 内部已处理规则为空时自动加载默认值，此处无需额外设置
                    var bindings = RunScan(binder);
                    binder.ReplaceBindings(bindings);
                    binder.Invalidate();

                    PrefabUtility.SaveAsPrefabAsset(instance, path);
                    PrefabUtility.UnloadPrefabContents(instance);
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            AssetDatabase.Refresh();
            Debug.Log($"[UIAutoBinder] Setup done: {added} component(s) added, {total} prefab(s) scanned.");
        }

        [MenuItem("Tools/UIAutoBind/Remove All UIAutoBinder Components", false, 52)]
        private static void RemoveAllBinders()
        {
            string[] guids = AssetDatabase.FindAssets("t:Prefab");
            int removed = 0;

            try
            {
                for (int i = 0; i < guids.Length; i++)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                    EditorUtility.DisplayProgressBar("UIAutoBind - Remove Components",
                        path, (float)i / guids.Length);

                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    if (prefab == null) continue;

                    var binder = prefab.GetComponent<UIAutoBinder>();
                    if (binder == null) continue;

                    UnityEngine.Object.DestroyImmediate(binder, true);
                    EditorUtility.SetDirty(prefab);
                    removed++;
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            AssetDatabase.Refresh();
            Debug.Log($"[UIAutoBinder] Removed UIAutoBinder from {removed} prefab(s).");
        }

        /// <summary>Run scan on a binder instance (must be a scene instance, not a prefab asset).</summary>
        public static void ScanAndApply(UIAutoBinder binder)
        {
            var bindings = RunScan(binder);
            binder.ReplaceBindings(bindings);
            binder.Invalidate();
            binder.Initialize();
        }

        private static List<UIReference> RunScan(UIAutoBinder binder)
        {
            var rules = binder.PrefixRules;
            if (rules == null || rules.Count == 0)
                rules = UIAutoBinder.GetDefaultRules();

            var bindings = new List<UIReference>();
            ScanRecursive(binder.transform, string.Empty, binder, rules, bindings);
            ResolveConflicts(bindings, binder.CurrentConflictResolution);
            return bindings;
        }

        private static void ScanRecursive(
            Transform current, string parentPath,
            UIAutoBinder rootBinder,
            IReadOnlyList<PrefixRule> rules,
            List<UIReference> results)
        {
            // Skip subtrees that have their own UIAutoBinder (nested prefab)
            if (current != rootBinder.transform)
            {
                var childBinder = current.GetComponent<UIAutoBinder>();
                if (childBinder != null)
                    return;
            }

            string relativePath = string.IsNullOrEmpty(parentPath)
                ? current.name
                : parentPath + "/" + current.name;

            foreach (var rule in rules)
            {
                if (string.IsNullOrEmpty(rule.Prefix)) continue;
                if (!current.name.StartsWith(rule.Prefix, StringComparison.Ordinal)) continue;

                Component matched = FindComponent(current, rule.ComponentTypeNames);
                if (matched != null)
                {
                    string key = GenerateKey(current.name, relativePath, rootBinder.CurrentNamingScheme, rule.Prefix);
                    results.Add(new UIReference(key, current.name, relativePath, matched));
                }
                else if (!rule.Optional)
                {
                    Debug.LogWarning(
                        $"[UIAutoBinder] '{current.name}' matches prefix '{rule.Prefix}' " +
                        $"but has no matching component. Expected types: {string.Join(", ", rule.ComponentTypeNames)}.",
                        rootBinder);
                }
                break; // first matching prefix wins
            }

            foreach (Transform child in current)
            {
                ScanRecursive(child, relativePath, rootBinder, rules, results);
            }
        }

        private static readonly Dictionary<string, Type> s_TypeCache = new Dictionary<string, Type>();

        internal static Component FindComponent(Transform t, List<string> typeNames)
        {
            foreach (string typeName in typeNames)
            {
                if (!s_TypeCache.TryGetValue(typeName, out Type type))
                {
                    type = Type.GetType(typeName)
                        ?? Type.GetType(typeName + ", UnityEngine.UI");
                    s_TypeCache[typeName] = type; // null is fine as cache sentinel
                }
                if (type == null) continue;

                Component comp = t.GetComponent(type);
                if (comp != null) return comp;
            }
            return null;
        }

        internal static string GenerateKey(string gameObjectName, string relativePath,
            NamingScheme scheme, string prefix)
        {
            switch (scheme)
            {
                case NamingScheme.StripPrefix:
                    return gameObjectName.Substring(prefix.Length);
                case NamingScheme.FullPath:
                    return relativePath;
                case NamingScheme.FullName:
                    return gameObjectName;
                default:
                    return gameObjectName.Substring(prefix.Length);
            }
        }

        internal static void ResolveConflicts(List<UIReference> bindings, ConflictResolution resolution)
        {
            if (resolution == ConflictResolution.UseHierarchicalPath)
            {
                // Pass 1: count key occurrences
                var counts = new Dictionary<string, int>();
                foreach (var b in bindings)
                {
                    if (counts.ContainsKey(b.Key))
                        counts[b.Key]++;
                    else
                        counts[b.Key] = 1;
                }

                // Pass 2: re-key all items with duplicate keys to hierarchy path
                for (int i = 0; i < bindings.Count; i++)
                {
                    if (counts[bindings[i].Key] > 1)
                        bindings[i].SetKey(bindings[i].HierarchyPath);
                }
            }
            else // SkipDuplicates
            {
                var seen = new HashSet<string>();
                for (int i = bindings.Count - 1; i >= 0; i--)
                {
                    if (!seen.Add(bindings[i].Key))
                    {
                        Debug.LogWarning(
                            $"[UIAutoBinder] Duplicate key '{bindings[i].Key}', skipping '{bindings[i].OriginalName}' " +
                            $"at '{bindings[i].HierarchyPath}'.");
                        bindings.RemoveAt(i);
                    }
                }
            }
        }
    }
}
