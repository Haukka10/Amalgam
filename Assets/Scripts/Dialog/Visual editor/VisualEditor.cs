#if UNITY_EDITOR

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

#if UNITY_EDITOR
[CustomEditor(typeof(DialogData))]
public class DialogDataEditor : Editor
{
    SerializedProperty dialogNodesProp;
    SerializedProperty entryNodeProp;

    void OnEnable()
    {
        dialogNodesProp = serializedObject.FindProperty("dialogNodes");
        entryNodeProp = serializedObject.FindProperty("entryNode");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DialogData data = (DialogData)target;

        EditorGUILayout.HelpBox("Use the Visual Editor window to edit this dialog tree.", MessageType.Info);

        if (GUILayout.Button("Open Visual Editor", GUILayout.Height(40)))
        {
            DialogVisualEditor.OpenWindow(data);
        }

        if (GUILayout.Button("Reload Dialog DO NOT CLICK IN WIP", GUILayout.Height(25)))
        {
            // Force Unity to re-read the ScriptableObject
            AssetDatabase.Refresh();
            AssetDatabase.SaveAssets();

            // Reload inspector and visual editor
            serializedObject.Update();
            DialogVisualEditor.inspectorSelectedNode = null;

            // Repaint all editor windows that depend on this data
            EditorWindow.FocusWindowIfItsOpen<DialogVisualEditor>();
            var win = EditorWindow.GetWindow<DialogVisualEditor>();
            if (win != null)
            {
                win.Repaint();
            }

            Debug.Log($"Dialog '{data.name}' reloaded.");
        }


        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Quick Info", EditorStyles.boldLabel);
        EditorGUILayout.LabelField($"Total Nodes: {data.dialogNodes.Count}");
        EditorGUILayout.PropertyField(entryNodeProp, new GUIContent("Entry Node"));

        EditorGUILayout.Space();

        // Show the node selected in the visual editor (if any)
        var selNode = DialogVisualEditor.inspectorSelectedNode;
        if (selNode == null)
        {
            EditorGUILayout.LabelField("No node selected in Visual Editor.");
            serializedObject.ApplyModifiedProperties();
            return;
        }

        // ensure the selected node belongs to this DialogData
        int nodeIndex = data.dialogNodes.IndexOf(selNode);
        if (nodeIndex < 0)
        {
            EditorGUILayout.HelpBox("Selected node is not part of this DialogData asset.", MessageType.Warning);
            serializedObject.ApplyModifiedProperties();
            return;
        }

        EditorGUILayout.LabelField("Selected Node", EditorStyles.boldLabel);

        // Get SerializedProperty for the selected node
        SerializedProperty nodeProp = dialogNodesProp.GetArrayElementAtIndex(nodeIndex);

        EditorGUI.indentLevel++;
        // NPC Name & Dialog Text (use SerializedProperty so Undo works)
        var npcNameProp = nodeProp.FindPropertyRelative("npcName");
        var dialogTextProp = nodeProp.FindPropertyRelative("dialogText");
        EditorGUILayout.PropertyField(npcNameProp, new GUIContent("NPC Name"));
        EditorGUILayout.PropertyField(dialogTextProp, new GUIContent("Dialog Text"));

        // Options
        var optionsProp = nodeProp.FindPropertyRelative("options");
        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField($"Options ({optionsProp.arraySize})", EditorStyles.miniBoldLabel);
        EditorGUI.indentLevel++;

        // Build name array for popup (first entry = None)
        string[] nodeNames = new string[data.dialogNodes.Count + 1];
        nodeNames[0] = "<None>";
        for (int n = 0; n < data.dialogNodes.Count; n++)
            nodeNames[n + 1] = !string.IsNullOrEmpty(data.dialogNodes[n].npcName) ? data.dialogNodes[n].npcName : $"Node {n}";

        for (int i = 0; i < optionsProp.arraySize; i++)
        {
            var optProp = optionsProp.GetArrayElementAtIndex(i);
            var optionTextProp = optProp.FindPropertyRelative("optionText");

            EditorGUILayout.BeginHorizontal();
            // Option text (serialized so ApplyModifiedProperties handles it & Undo works)
            EditorGUILayout.PropertyField(optionTextProp, new GUIContent($"Option {i + 1}"));

            // Next node popup (we change the underlying data object directly to set nextNode)
            // Determine current selection index
            DialogOption optRef = data.dialogNodes[nodeIndex].options[i];
            int currentSelection = 0;
            if (optRef.nextNode != null)
            {
                int idx = data.dialogNodes.IndexOf(optRef.nextNode);
                if (idx >= 0) currentSelection = idx + 1; // +1 because 0 is None
            }

            int newSelection = EditorGUILayout.Popup(currentSelection, nodeNames, GUILayout.Width(150));
            if (newSelection != currentSelection)
            {
                Undo.RecordObject(data, "Set Option Next Node");
                optRef.nextNode = (newSelection == 0) ? null : data.dialogNodes[newSelection - 1];
                EditorUtility.SetDirty(data);
            }

            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.Space(4);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("+ Add Option"))
        {
            Undo.RecordObject(data, "Add Dialog Option");
            data.dialogNodes[nodeIndex].options.Add(new DialogOption { optionText = "New Option", nextNode = null });
            EditorUtility.SetDirty(data);
            // Update serialized object so inspector immediately shows the new prop
            serializedObject.Update();
            optionsProp = nodeProp.FindPropertyRelative("options");
        }

        if (optionsProp.arraySize > 0)
        {
            if (GUILayout.Button("- Remove Last Option"))
            {
                Undo.RecordObject(data, "Remove Dialog Option");
                data.dialogNodes[nodeIndex].options.RemoveAt(data.dialogNodes[nodeIndex].options.Count - 1);
                EditorUtility.SetDirty(data);
                serializedObject.Update();
                optionsProp = nodeProp.FindPropertyRelative("options");
            }
        }
        EditorGUILayout.EndHorizontal();

        EditorGUI.indentLevel -= 2;

        serializedObject.ApplyModifiedProperties();
    }
}
#endif


public class DialogVisualEditor : EditorWindow
{
    public static DialogNode inspectorSelectedNode;

    private DialogData currentData;
    private Vector2 offset;
    private Vector2 drag;
    private float zoom = 1f;

    private DialogNode selectedNode;
    private DialogNode connectingNode;
    private int connectingOptionIndex = -1;

    private const float NODE_WIDTH = 200f;
    private const float NODE_HEADER_HEIGHT = 30f;
    private const float OPTION_HEIGHT = 25f;
    private const float GRID_SIZE = 20f;

    private GUIStyle nodeStyle;
    private GUIStyle selectedNodeStyle;
    private GUIStyle entryNodeStyle;
    private GUIStyle optionStyle;


    [MenuItem("Window/Dialog Visual Editor")]
    public static void OpenWindow()
    {
        DialogVisualEditor window = GetWindow<DialogVisualEditor>("Dialog Editor");
        window.minSize = new Vector2(800, 600);
    }

    public static void OpenWindow(DialogData data)
    {
        DialogVisualEditor window = GetWindow<DialogVisualEditor>("Dialog Editor");
        window.minSize = new Vector2(800, 600);
        window.currentData = data;
        window.offset = data.editorOffset;
        window.zoom = data.editorZoom;
    }

    void OnEnable()
    {
        InitStyles();
    }

    void InitStyles()
    {
        nodeStyle = new GUIStyle();
        nodeStyle.normal.background = MakeTexture(2, 2, new Color(0.3f, 0.3f, 0.3f));
        nodeStyle.border = new RectOffset(12, 12, 12, 12);
        nodeStyle.padding = new RectOffset(10, 10, 10, 10);

        selectedNodeStyle = new GUIStyle(nodeStyle);
        selectedNodeStyle.normal.background = MakeTexture(2, 2, new Color(0.2f, 0.4f, 0.6f));

        entryNodeStyle = new GUIStyle(nodeStyle);
        entryNodeStyle.normal.background = MakeTexture(2, 2, new Color(0.2f, 0.6f, 0.3f));

        optionStyle = new GUIStyle();
        optionStyle.normal.background = MakeTexture(2, 2, new Color(0.4f, 0.4f, 0.4f));
        optionStyle.padding = new RectOffset(5, 5, 3, 3);
        optionStyle.margin = new RectOffset(0, 0, 2, 2);
        optionStyle.normal.textColor = Color.white;
        optionStyle.fontSize = 11;
    }

    Texture2D MakeTexture(int width, int height, Color col)
    {
        Color[] pix = new Color[width * height];
        for (int i = 0; i < pix.Length; i++)
            pix[i] = col;

        Texture2D result = new Texture2D(width, height);
        result.SetPixels(pix);
        result.Apply();
        return result;
    }

    void OnGUI()
    {
        if (nodeStyle == null) InitStyles();

        DrawToolbar();
        DrawGrid();

        if (currentData != null)
        {
            DrawConnections();
            DrawNodes();
            DrawConnectionLine();
        }

        ProcessEvents(Event.current);

        if (GUI.changed)
            Repaint();
    }

    void DrawToolbar()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

        if (GUILayout.Button("Select Dialog Data", EditorStyles.toolbarButton, GUILayout.Width(120)))
        {
            GenericMenu menu = new GenericMenu();
            string[] guids = AssetDatabase.FindAssets("t:DialogData");

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                DialogData data = AssetDatabase.LoadAssetAtPath<DialogData>(path);
                menu.AddItem(new GUIContent(data.name), data == currentData, () =>
                {
                    currentData = data;
                    offset = data.editorOffset;
                    zoom = data.editorZoom;
                });
            }

            if (guids.Length == 0)
                menu.AddDisabledItem(new GUIContent("No DialogData found"));

            menu.ShowAsContext();
        }

        EditorGUILayout.LabelField(currentData != null ? currentData.name : "No Dialog Selected", EditorStyles.toolbarButton);

        GUILayout.FlexibleSpace();

        if (currentData != null)
        {
            if (GUILayout.Button("Add Node", EditorStyles.toolbarButton, GUILayout.Width(80)))
            {
                AddNode(new Vector2(100, 100) - offset);
            }

            if (GUILayout.Button("Center View", EditorStyles.toolbarButton, GUILayout.Width(80)))
            {
                offset = Vector2.zero;
                zoom = 1f;
            }
        }

        EditorGUILayout.EndHorizontal();
    }

    void DrawGrid()
    {
        Handles.BeginGUI();
        Handles.color = new Color(0.5f, 0.5f, 0.5f, 0.2f);

        float gridSpacing = GRID_SIZE * zoom;
        int widthDivs = Mathf.CeilToInt(position.width / gridSpacing);
        int heightDivs = Mathf.CeilToInt(position.height / gridSpacing);

        Vector2 gridOffset = new Vector2(offset.x % gridSpacing, offset.y % gridSpacing);
        for (int i = 0; i < widthDivs; i++)
        {
            Handles.DrawLine(
                new Vector3(gridSpacing * i + gridOffset.x, 0, 0),
                new Vector3(gridSpacing * i + gridOffset.x, position.height, 0)
            );
        }

        for (int i = 0; i < heightDivs; i++)
        {
            Handles.DrawLine(
                new Vector3(0, gridSpacing * i + gridOffset.y, 0),
                new Vector3(position.width, gridSpacing * i + gridOffset.y, 0)
            );
        }

        Handles.EndGUI();
    }

    void DrawNodes()
    {
        if (currentData == null) return;

        for (int i = currentData.dialogNodes.Count - 1; i >= 0; i--)
        {
            DrawNode(currentData.dialogNodes[i]);
        }
    }

    void DrawNode(DialogNode node)
    {
        Vector2 pos = node.position * zoom + offset;
        float nodeHeight = NODE_HEADER_HEIGHT + (node.options.Count * OPTION_HEIGHT) + 60;
        Rect nodeRect = new Rect(pos.x, pos.y, NODE_WIDTH * zoom, nodeHeight * zoom);

        GUIStyle style = node == selectedNode ? selectedNodeStyle :
                        (node == currentData.entryNode ? entryNodeStyle : nodeStyle);

        GUI.Box(nodeRect, "", style);

        GUILayout.BeginArea(nodeRect);

        EditorGUILayout.BeginHorizontal();
        GUILayout.Label(node.npcName, EditorStyles.boldLabel);
        if (GUILayout.Button("×", GUILayout.Width(20)))
        {
            DeleteNode(node);
            GUILayout.EndArea();
            return;
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.LabelField(node.dialogText, EditorStyles.wordWrappedLabel);

        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField("Options:", EditorStyles.miniBoldLabel);

        for (int i = 0; i < node.options.Count; i++)
        {
            EditorGUILayout.BeginHorizontal(optionStyle);

            string optionLabel = string.IsNullOrEmpty(node.options[i].optionText) ?
                                $"Option {i + 1}" : node.options[i].optionText;

            if (GUILayout.Button(optionLabel, EditorStyles.label))
            {
                selectedNode = node;
                Selection.activeObject = currentData;
                inspectorSelectedNode = node;
            }

            if (GUILayout.Button("→", GUILayout.Width(25)))
            {
                connectingNode = node;
                connectingOptionIndex = i;
            }

            EditorGUILayout.EndHorizontal();

            node.options[i].connectionPoint = new Vector2(
                pos.x + NODE_WIDTH * zoom,
                pos.y + NODE_HEADER_HEIGHT * zoom + 60 * zoom + (i * OPTION_HEIGHT * zoom) + 12 * zoom
            );
        }

        if (GUILayout.Button("+ Add Option"))
        {
            Undo.RecordObject(currentData, "Add Dialog Option");
            node.options.Add(new DialogOption { optionText = "New Option" });
            EditorUtility.SetDirty(currentData);
        }

        GUILayout.EndArea();

        if (nodeRect.Contains(Event.current.mousePosition))
        {
            if (Event.current.type == EventType.MouseDown && Event.current.button == 0)
            {
                selectedNode = node;
                Selection.activeObject = currentData;
                inspectorSelectedNode = node;
                GUI.changed = true;
            }
        }
    }

    void DrawConnections()
    {
        if (currentData == null) return;

        Handles.BeginGUI();

        foreach (var node in currentData.dialogNodes)
        {
            for (int i = 0; i < node.options.Count; i++)
            {
                if (node.options[i].nextNode != null)
                {
                    Vector2 start = node.options[i].connectionPoint;
                    Vector2 end = node.options[i].nextNode.position * zoom + offset + new Vector2(0, NODE_HEADER_HEIGHT * zoom / 2);

                    Handles.color = Color.white;
                    Handles.DrawBezier(
                        start,
                        end,
                        start + Vector2.right * 50,
                        end + Vector2.left * 50,
                        Color.white,
                        null,
                        2f
                    );

                    Vector2 arrowPos = end;
                    Vector2 direction = (end - start).normalized;
                    DrawArrow(arrowPos, direction);
                }
            }
        }

        Handles.EndGUI();
    }

    void DrawArrow(Vector2 position, Vector2 direction)
    {
        if (position == Vector2.zero)
            return;
        if (direction == Vector2.zero)
            return;

        Vector2 perpendicular = new Vector2(-direction.y, direction.x);
        Vector2 arrowHead1 = position - direction * 10 + perpendicular * 5;
        Vector2 arrowHead2 = position - direction * 10 - perpendicular * 5;

        Handles.DrawLine(position, arrowHead1);
        Handles.DrawLine(position, arrowHead2);
    }

    void DrawConnectionLine()
    {
        if (connectingNode != null && connectingOptionIndex >= 0)
        {
            Handles.BeginGUI();
            Handles.color = Color.yellow;

            Vector2 start = connectingNode.options[connectingOptionIndex].connectionPoint;
            Vector2 end = Event.current.mousePosition;

            Handles.DrawBezier(
                start,
                end,
                start + Vector2.right * 50,
                end + Vector2.left * 50,
                Color.yellow,
                null,
                3f
            );

            Handles.EndGUI();
            Repaint();
        }
    }

    void ProcessEvents(Event e)
    {
        drag = Vector2.zero;

        switch (e.type)
        {
            case EventType.MouseDown:
                if (e.button == 0)
                {
                    if (connectingNode != null)
                    {
                        DialogNode targetNode = GetNodeAtPosition(e.mousePosition);
                        if (targetNode != null && targetNode != connectingNode)
                        {
                            Undo.RecordObject(currentData, "Connect Dialog Nodes");
                            connectingNode.options[connectingOptionIndex].nextNode = targetNode;
                            EditorUtility.SetDirty(currentData);
                        }
                        connectingNode = null;
                        connectingOptionIndex = -1;
                        e.Use();
                    }
                }
                else if (e.button == 1)
                {
                    if (connectingNode != null)
                    {
                        connectingNode = null;
                        connectingOptionIndex = -1;
                        e.Use();
                    }
                    else
                    {
                        ShowContextMenu(e.mousePosition);
                    }
                }
                else if (e.button == 2)
                {
                    // Middle mouse button - start panning
                }
                break;

            case EventType.MouseDrag:
                if (e.button == 0 && selectedNode != null)
                {
                    Undo.RecordObject(currentData, "Move Dialog Node");
                    selectedNode.position += e.delta / zoom;
                    EditorUtility.SetDirty(currentData);
                    e.Use();
                }
                else if (e.button == 2)
                {
                    offset += e.delta;
                    e.Use();
                }
                break;

            case EventType.ScrollWheel:
                float zoomDelta = -e.delta.y * 0.05f;
                zoom = Mathf.Clamp(zoom + zoomDelta, 0.5f, 2f);
                e.Use();
                break;
        }

        if (currentData != null)
        {
            currentData.editorOffset = offset;
            currentData.editorZoom = zoom;
        }
    }

    DialogNode GetNodeAtPosition(Vector2 mousePos)
    {
        if (currentData == null) return null;

        foreach (var node in currentData.dialogNodes)
        {
            Vector2 pos = node.position * zoom + offset;
            float nodeHeight = NODE_HEADER_HEIGHT + (node.options.Count * OPTION_HEIGHT) + 60;
            Rect nodeRect = new Rect(pos.x, pos.y, NODE_WIDTH * zoom, nodeHeight * zoom);

            if (nodeRect.Contains(mousePos))
                return node;
        }

        return null;
    }

    void ShowContextMenu(Vector2 mousePos)
    {
        GenericMenu menu = new GenericMenu();

        if (currentData != null)
        {
            menu.AddItem(new GUIContent("Add Node"), false, () => AddNode(mousePos - offset));

            DialogNode clickedNode = GetNodeAtPosition(mousePos);
            if (clickedNode != null)
            {
                menu.AddSeparator("");
                menu.AddItem(new GUIContent("Set as Entry Node"), false, () =>
                {
                    Undo.RecordObject(currentData, "Set Entry Node");
                    currentData.entryNode = clickedNode;
                    EditorUtility.SetDirty(currentData);
                });
                menu.AddItem(new GUIContent("Delete Node"), false, () => DeleteNode(clickedNode));
            }
        }
        else
        {
            menu.AddDisabledItem(new GUIContent("No Dialog Data Selected"));
        }

        menu.ShowAsContext();
    }

    void AddNode(Vector2 position)
    {
        if (currentData == null) return;

        Undo.RecordObject(currentData, "Add Dialog Node");

        DialogNode newNode = new DialogNode
        {
            nodeID = System.Guid.NewGuid().ToString(),
            npcName = "NPC",
            dialogText = "Enter dialog text here...",
            position = position / zoom,
            options = new List<DialogOption>
            {
                new DialogOption { optionText = "Option 1", nextNode = null }
            }
        };

        currentData.dialogNodes.Add(newNode);

        if (currentData.entryNode == null)
            currentData.entryNode = newNode;

        selectedNode = newNode;
        EditorUtility.SetDirty(currentData);
    }

    void DeleteNode(DialogNode node)
    {
        if (currentData == null) return;

        Undo.RecordObject(currentData, "Delete Dialog Node");

        foreach (var n in currentData.dialogNodes)
        {
            foreach (var option in n.options)
            {
                if (option.nextNode == node)
                    option.nextNode = null;
            }
        }

        if (currentData.entryNode == node)
            currentData.entryNode = null;

        if (inspectorSelectedNode == node)
            inspectorSelectedNode = null;

        currentData.dialogNodes.Remove(node);

        if (selectedNode == node)
            selectedNode = null;

        EditorUtility.SetDirty(currentData);
    }
}
#endif