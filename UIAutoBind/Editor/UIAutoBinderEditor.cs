using UIAutoBind;
using UnityEditor;
using UnityEngine;

namespace UIAutoBind.Editor
{
    [CustomEditor(typeof(UIAutoBinder))]
    public class UIAutoBinderEditor : UnityEditor.Editor
    {
        private SerializedProperty m_BindingsProp;
        private SerializedProperty m_NamingSchemeProp;
        private SerializedProperty m_ConflictResolutionProp;
        private SerializedProperty m_PrefixRulesProp;
        private SerializedProperty m_AutoBuildOnAwakeProp;
        private bool m_ShowRules = true;
        private bool m_ShowBindings = true;

        void OnEnable()
        {
            m_BindingsProp = serializedObject.FindProperty("m_Bindings");
            m_NamingSchemeProp = serializedObject.FindProperty("m_NamingScheme");
            m_ConflictResolutionProp = serializedObject.FindProperty("m_ConflictResolution");
            m_PrefixRulesProp = serializedObject.FindProperty("m_PrefixRules");
            m_AutoBuildOnAwakeProp = serializedObject.FindProperty("m_AutoBuildOnAwake");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(m_AutoBuildOnAwakeProp);
            EditorGUILayout.PropertyField(m_NamingSchemeProp);
            EditorGUILayout.PropertyField(m_ConflictResolutionProp);
            EditorGUILayout.Space();

            m_ShowRules = EditorGUILayout.Foldout(m_ShowRules, "Prefix Rules");
            if (m_ShowRules)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(m_PrefixRulesProp, new GUIContent("Rules"), true);
                if (GUILayout.Button("Load Default Rules", GUILayout.Width(140)))
                {
                    LoadDefaultRules();
                }
                EditorGUI.indentLevel--;
            }

            // Scan button
            var oldColor = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.4f, 0.8f, 0.4f);
            if (GUILayout.Button("Scan Child Hierarchy Now", GUILayout.Height(30)))
            {
                var binder = (UIAutoBinder)target;
                UIAutoBinderScanner.ScanAndApply(binder);
                EditorUtility.SetDirty(target);
                serializedObject.Update();
            }
            GUI.backgroundColor = oldColor;
            EditorGUILayout.Space();

            // Bindings list
            m_ShowBindings = EditorGUILayout.Foldout(m_ShowBindings,
                $"Bindings ({m_BindingsProp.arraySize})");
            if (m_ShowBindings)
            {
                EditorGUI.indentLevel++;
                GUI.enabled = false;
                for (int i = 0; i < m_BindingsProp.arraySize; i++)
                {
                    var element = m_BindingsProp.GetArrayElementAtIndex(i);
                    var keyProp = element.FindPropertyRelative("m_Key");
                    var compProp = element.FindPropertyRelative("m_Component");
                    var typeProp = element.FindPropertyRelative("m_ComponentTypeName");

                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField($"[{i}]", GUILayout.Width(30));
                    EditorGUILayout.LabelField(keyProp.stringValue, GUILayout.Width(140));
                    EditorGUILayout.ObjectField(compProp.objectReferenceValue, typeof(Component), true);
                    EditorGUILayout.LabelField(typeProp.stringValue, GUILayout.Width(80));
                    EditorGUILayout.EndHorizontal();
                }
                GUI.enabled = true;
                EditorGUI.indentLevel--;
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void LoadDefaultRules()
        {
            var rules = UIAutoBinder.GetDefaultRules();
            m_PrefixRulesProp.ClearArray();
            for (int i = 0; i < rules.Count; i++)
            {
                m_PrefixRulesProp.InsertArrayElementAtIndex(i);
                var element = m_PrefixRulesProp.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("Prefix").stringValue = rules[i].Prefix;
                element.FindPropertyRelative("Optional").boolValue = rules[i].Optional;

                var typeNamesProp = element.FindPropertyRelative("ComponentTypeNames");
                typeNamesProp.ClearArray();
                for (int j = 0; j < rules[i].ComponentTypeNames.Count; j++)
                {
                    typeNamesProp.InsertArrayElementAtIndex(j);
                    typeNamesProp.GetArrayElementAtIndex(j).stringValue = rules[i].ComponentTypeNames[j];
                }
            }
            serializedObject.Update();
        }
    }
}
