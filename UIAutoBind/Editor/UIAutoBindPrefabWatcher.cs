using UIAutoBind;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace UIAutoBind.Editor
{
    /// <summary>
    /// 监听 Prefab Mode 保存事件，自动重新扫描 UIAutoBinder 绑定。
    /// 设置 AutoScanOnSave = false 可关闭此行为。
    /// </summary>
    [InitializeOnLoad]
    public static class UIAutoBindPrefabWatcher
    {
        public static bool AutoScanOnSave = true;

        static UIAutoBindPrefabWatcher()
        {
            PrefabStage.prefabSaving += OnPrefabSaving;
        }

        private static void OnPrefabSaving(GameObject prefab)
        {
            if (!AutoScanOnSave) return;

            var binder = prefab.GetComponent<UIAutoBinder>();
            if (binder == null) return;

            UIAutoBinderScanner.ScanAndApply(binder);
            Debug.Log($"[UIAutoBinder] Auto-scanned on save: {prefab.name} ({binder.Bindings.Count} bindings).");
        }
    }
}
