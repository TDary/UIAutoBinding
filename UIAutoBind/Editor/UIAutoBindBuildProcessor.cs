using UIAutoBind;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace UIAutoBind.Editor
{
    public class UIAutoBindBuildProcessor : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            Debug.Log("[UIAutoBinder] Pre-build scan starting...");
            string[] guids = AssetDatabase.FindAssets("t:Prefab");
            int refreshed = 0;
            int warned = 0;
            int total = guids.Length;

            try
            {
                for (int i = 0; i < guids.Length; i++)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                    if (i % 50 == 0)
                        EditorUtility.DisplayProgressBar("UIAutoBind Pre-build", path, (float)i / total);

                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    if (prefab == null) continue;

                    var binder = prefab.GetComponent<UIAutoBinder>();
                    if (binder == null) continue;

                    // Auto-refresh if bindings are empty
                    if (binder.Bindings == null || binder.Bindings.Count == 0)
                    {
                        var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                        if (instance == null) continue;
                        var instanceBinder = instance.GetComponent<UIAutoBinder>();
                        if (instanceBinder != null)
                        {
                            UIAutoBinderScanner.ScanAndApply(instanceBinder);
                            PrefabUtility.SaveAsPrefabAsset(instance, path);
                            refreshed++;
                        }
                        UnityEngine.Object.DestroyImmediate(instance);
                    }

                    // Validate: check for broken references
                    foreach (var b in binder.Bindings)
                    {
                        if (b.Component == null)
                        {
                            Debug.LogWarning(
                                $"[UIAutoBinder] Null reference in prefab '{path}', key '{b.Key}'. " +
                                $"Original object '{b.OriginalName}' may have been deleted.");
                            warned++;
                        }
                    }
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            if (refreshed > 0)
                AssetDatabase.Refresh();

            Debug.Log($"[UIAutoBinder] Pre-build done: {refreshed} refreshed, {warned} warnings.");
        }
    }
}
