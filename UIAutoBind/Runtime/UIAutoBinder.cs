using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace UIAutoBind
{
    public enum NamingScheme
    {
        StripPrefix,
        FullPath,
        FullName,
    }

    public enum ConflictResolution
    {
        UseHierarchicalPath,
        SkipDuplicates,
    }

    [AddComponentMenu("UI/UIAutoBinder")]
    public class UIAutoBinder : MonoBehaviour
    {
        [SerializeField] private List<UIReference> m_Bindings = new List<UIReference>();
        [SerializeField] private NamingScheme m_NamingScheme = NamingScheme.StripPrefix;
        [SerializeField] private ConflictResolution m_ConflictResolution = ConflictResolution.UseHierarchicalPath;
        [SerializeField] private List<PrefixRule> m_PrefixRules = new List<PrefixRule>();
        [SerializeField] private bool m_AutoBuildOnAwake = true;

        private Dictionary<string, Component> m_Lookup;
        private bool m_Initialized;

        public IReadOnlyList<UIReference> Bindings => m_Bindings;
        public NamingScheme CurrentNamingScheme => m_NamingScheme;
        public IReadOnlyList<PrefixRule> PrefixRules => m_PrefixRules;
        public ConflictResolution CurrentConflictResolution => m_ConflictResolution;

        // ---- Public API ----

        void Awake()
        {
            if (m_AutoBuildOnAwake)
                BuildLookup();
        }

        public void Initialize()
        {
            BuildLookup();
        }

        public T GetUIComponent<T>(string name) where T : Component
        {
            if (!m_Initialized) BuildLookup();
            if (m_Lookup == null) return null;
            if (m_Lookup.TryGetValue(name, out Component c) && c is T typed)
                return typed;
            return null;
        }

        public Component GetUIComponent(string name, Type componentType)
        {
            if (!m_Initialized) BuildLookup();
            if (m_Lookup == null) return null;
            if (m_Lookup.TryGetValue(name, out Component c) && componentType.IsInstanceOfType(c))
                return c;
            return null;
        }

        public bool HasKey(string name)
        {
            if (!m_Initialized) BuildLookup();
            return m_Lookup != null && m_Lookup.ContainsKey(name);
        }

        public List<T> GetAllOfType<T>() where T : Component
        {
            if (!m_Initialized) BuildLookup();
            var results = new List<T>();
            if (m_Lookup == null) return results;
            foreach (var kv in m_Lookup)
                if (kv.Value is T t)
                    results.Add(t);
            return results;
        }

        // ---- Convenience Getters ----

        public Button GetButton(string name) => GetUIComponent<Button>(name);
        public Text GetText(string name) => GetUIComponent<Text>(name);
        public Image GetImage(string name) => GetUIComponent<Image>(name);
        public Toggle GetToggle(string name) => GetUIComponent<Toggle>(name);
        public Slider GetSlider(string name) => GetUIComponent<Slider>(name);
        public InputField GetInputField(string name) => GetUIComponent<InputField>(name);
        public ScrollRect GetScrollRect(string name) => GetUIComponent<ScrollRect>(name);
        public Dropdown GetDropdown(string name) => GetUIComponent<Dropdown>(name);
        public RawImage GetRawImage(string name) => GetUIComponent<RawImage>(name);

        // ---- Internal ----

        private void BuildLookup()
        {
            if (m_Initialized) return;
            m_Lookup = new Dictionary<string, Component>(m_Bindings.Count);
            foreach (var b in m_Bindings)
            {
                if (b.Component != null && !string.IsNullOrEmpty(b.Key))
                    m_Lookup[b.Key] = b.Component;
            }
            m_Initialized = true;
        }

        public void ReplaceBindings(List<UIReference> newBindings)
        {
            m_Bindings.Clear();
            m_Bindings.AddRange(newBindings);
        }

        public void Invalidate()
        {
            m_Lookup = null;
            m_Initialized = false;
        }

        #region Default Prefix Rules

        public static List<PrefixRule> GetDefaultRules()
        {
            return new List<PrefixRule>
            {
                new PrefixRule("btn_", new List<string> { "UnityEngine.UI.Button" }),
                new PrefixRule("txt_", new List<string> { "TMPro.TextMeshProUGUI", "UnityEngine.UI.Text" }),
                new PrefixRule("img_", new List<string> { "UnityEngine.UI.Image" }),
                new PrefixRule("tog_", new List<string> { "UnityEngine.UI.Toggle" }),
                new PrefixRule("sld_", new List<string> { "UnityEngine.UI.Slider" }),
                new PrefixRule("scr_", new List<string> { "UnityEngine.UI.ScrollRect" }),
                new PrefixRule("scc_", new List<string> { "UnityEngine.UI.Scrollbar" }),
                new PrefixRule("inp_", new List<string> { "TMPro.TMP_InputField", "UnityEngine.UI.InputField" }),
                new PrefixRule("drp_", new List<string> { "TMPro.TMP_Dropdown", "UnityEngine.UI.Dropdown" }),
                new PrefixRule("raw_", new List<string> { "UnityEngine.UI.RawImage" }),
                new PrefixRule("mask_", new List<string> { "UnityEngine.UI.Mask", "UnityEngine.UI.RectMask2D" }, optional: true),
            };
        }

        #endregion
    }
}