using System;
using UnityEngine;

namespace UIAutoBind
{
    [Serializable]
    public class UIReference
    {
        [SerializeField] private string m_Key;
        [SerializeField] private string m_OriginalName;
        [SerializeField] private string m_HierarchyPath;
        [SerializeField] private Component m_Component;
        [SerializeField] private string m_ComponentTypeName;

        public string Key => m_Key;
        public string OriginalName => m_OriginalName;
        public string HierarchyPath => m_HierarchyPath;
        public Component Component => m_Component;

        public UIReference() { }

        public UIReference(string key, string originalName, string hierarchyPath, Component component)
        {
            m_Key = key;
            m_OriginalName = originalName;
            m_HierarchyPath = hierarchyPath;
            m_Component = component;
            m_ComponentTypeName = component != null ? component.GetType().Name : string.Empty;
        }

        public void SetKey(string key) { m_Key = key; }
    }
}