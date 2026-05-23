using System;
using System.Collections.Generic;
using UnityEngine;

namespace UIAutoBind
{
    [Serializable]
    public class PrefixRule
    {
        [Tooltip("GameObject name prefix, e.g. 'btn_'")]
        public string Prefix = string.Empty;

        [Tooltip("Priority-ordered fully qualified component type names")]
        public List<string> ComponentTypeNames = new List<string>();

        [Tooltip("If true, no warning when component is missing")]
        public bool Optional;

        public PrefixRule() { }

        public PrefixRule(string prefix, List<string> componentTypeNames, bool optional = false)
        {
            Prefix = prefix;
            ComponentTypeNames = componentTypeNames;
            Optional = optional;
        }
    }
}