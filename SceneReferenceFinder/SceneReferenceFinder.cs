using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class SceneReferenceFinder : EditorWindow
{
    private Object targetObject;
    private Object replacementObject;

    private Vector2 scrollPos;
    private List<ReferenceData> references = new List<ReferenceData>();

    private bool enableReplace = false;

    private enum SearchMode
    {
        FindReferences,
        FindComponents
    }

    private SearchMode searchMode;

    private static HashSet<GameObject> highlightedObjects = new HashSet<GameObject>();

    [MenuItem("Tools/Scene Reference Finder")]
    public static void ShowWindow()
    {
        GetWindow<SceneReferenceFinder>("Reference Finder");
    }

    private void OnEnable()
    {
        EditorApplication.hierarchyWindowItemOnGUI += HighlightInHierarchy;
    }

    private void OnDisable()
    {
        EditorApplication.hierarchyWindowItemOnGUI -= HighlightInHierarchy;
        highlightedObjects.Clear();
    }

    private void OnGUI()
    {
        GUILayout.Label("Scene Reference Finder", EditorStyles.boldLabel);

        searchMode = (SearchMode)EditorGUILayout.EnumPopup("Search Mode", searchMode);

        targetObject = EditorGUILayout.ObjectField("Target", targetObject, typeof(Object), true);

        if (GUILayout.Button("Search"))
        {
            if (searchMode == SearchMode.FindReferences)
                FindReferences();
            else
                FindComponents();
        }

        if (GUILayout.Button("Clear Highlights"))
        {
            highlightedObjects.Clear();
            RepaintHierarchy();
        }

        EditorGUILayout.Space();

        GUILayout.Label($"Found {references.Count} results", EditorStyles.boldLabel);

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        foreach (var refData in references)
        {
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Ping", GUILayout.Width(50)))
            {
                EditorGUIUtility.PingObject(refData.gameObject);
                Selection.activeObject = refData.gameObject;
            }

            EditorGUILayout.LabelField(
                $"{refData.gameObject.name} | {refData.component?.GetType().Name} | {refData.propertyPath}"
            );

            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space();

        enableReplace = EditorGUILayout.Toggle("Enable Replace", enableReplace);

        if (enableReplace && searchMode == SearchMode.FindReferences)
        {
            replacementObject = EditorGUILayout.ObjectField("Replacement Object", replacementObject, typeof(Object), true);

            GUI.enabled = replacementObject != null && references.Count > 0;

            if (GUILayout.Button("Replace All"))
            {
                ReplaceAll();
            }

            GUI.enabled = true;
        }
    }

    // -------- FIND REFERENCES --------
    private void FindReferences()
    {
        references.Clear();
        highlightedObjects.Clear();

        GameObject[] allObjects = FindObjectsOfType<GameObject>(true);

        foreach (GameObject go in allObjects)
        {
            Component[] components = go.GetComponents<Component>();

            foreach (Component comp in components)
            {
                if (comp == null) continue;

                SerializedObject so = new SerializedObject(comp);
                SerializedProperty prop = so.GetIterator();

                while (prop.NextVisible(true))
                {
                    if (prop.propertyType == SerializedPropertyType.ObjectReference)
                    {
                        if (prop.objectReferenceValue == targetObject)
                        {
                            references.Add(new ReferenceData
                            {
                                gameObject = go,
                                component = comp,
                                propertyPath = prop.propertyPath
                            });

                            highlightedObjects.Add(go);
                        }
                    }
                }
            }
        }

        RepaintHierarchy();
    }

    // -------- FIND COMPONENTS --------
    private void FindComponents()
    {
        references.Clear();
        highlightedObjects.Clear();

        if (targetObject == null)
            return;

        MonoScript script = targetObject as MonoScript;

        if (script == null)
        {
            Debug.LogError("Please assign a script (MonoScript) to find components.");
            return;
        }

        System.Type type = script.GetClass();

        if (type == null)
        {
            Debug.LogError("Could not get class from script.");
            return;
        }

        Component[] allComponents = FindObjectsOfType(type, true) as Component[];

        foreach (Component comp in allComponents)
        {
            if (comp == null) continue;

            references.Add(new ReferenceData
            {
                gameObject = comp.gameObject,
                component = comp,
                propertyPath = "Component Attached"
            });

            highlightedObjects.Add(comp.gameObject);
        }

        RepaintHierarchy();
    }

    // -------- REPLACE --------
    private void ReplaceAll()
    {
        foreach (var refData in references)
        {
            SerializedObject so = new SerializedObject(refData.component);
            SerializedProperty prop = so.FindProperty(refData.propertyPath);

            if (prop != null && prop.propertyType == SerializedPropertyType.ObjectReference)
            {
                Undo.RecordObject(refData.component, "Replace Reference");

                prop.objectReferenceValue = replacementObject;

                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(refData.component);
            }
        }
    }

    // -------- HIGHLIGHT --------
    private static void HighlightInHierarchy(int instanceID, Rect selectionRect)
    {
        GameObject obj = EditorUtility.InstanceIDToObject(instanceID) as GameObject;

        if (obj != null && highlightedObjects.Contains(obj))
        {
            Color color = new Color(0.2f, 1f, 0.3f, 0.3f); // Green highlight
            EditorGUI.DrawRect(selectionRect, color);
        }
    }

    private void RepaintHierarchy()
    {
        EditorApplication.RepaintHierarchyWindow();
    }

    private class ReferenceData
    {
        public GameObject gameObject;
        public Component component;
        public string propertyPath;
    }
}
