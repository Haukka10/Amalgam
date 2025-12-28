#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(DialogData))]
public class DialogDataEditor : Editor
{
    SerializedProperty dialogNodesProp;
    SerializedProperty entryNodeIDProp;

    private DialogNode cachedSelectedNode;
    private int cachedNodeIndex = -1;
    private string[] cachedNodeNames;

    void OnEnable()
    {
        dialogNodesProp = serializedObject.FindProperty("dialogNodes");
        entryNodeIDProp = serializedObject.FindProperty("entryNodeID");
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

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Quick Info", EditorStyles.boldLabel);
        EditorGUILayout.LabelField($"Total Nodes: {data.dialogNodes.Count}");

        // Entry Node dropdown - cache node names
        if (cachedNodeNames == null || cachedNodeNames.Length != data.dialogNodes.Count + 1)
        {
            cachedNodeNames = GetNodeNamesArray(data);
        }

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Entry Node:", GUILayout.Width(80));
        int entryIndex = GetNodeIndexByID(data, data.entryNodeID);
        int newEntryIndex = EditorGUILayout.Popup(entryIndex + 1, cachedNodeNames) - 1;

        if (newEntryIndex != entryIndex)
        {
            Undo.RecordObject(data, "Set Entry Node");
            data.entryNodeID = (newEntryIndex < 0) ? "" : data.dialogNodes[newEntryIndex].nodeID;
            EditorUtility.SetDirty(data);
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();

        // Show selected node details - OPTIMIZED
        var selNode = DialogVisualEditor.inspectorSelectedNode;
        if (selNode == null)
        {
            cachedSelectedNode = null;
            cachedNodeIndex = -1;
            EditorGUILayout.LabelField("No node selected in Visual Editor.");
            serializedObject.ApplyModifiedProperties();
            return;
        }

        // Cache node index lookup
        if (selNode != cachedSelectedNode)
        {
            cachedSelectedNode = selNode;
            cachedNodeIndex = data.dialogNodes.IndexOf(selNode);
        }

        if (cachedNodeIndex < 0)
        {
            EditorGUILayout.HelpBox("Selected node is not part of this DialogData asset.", MessageType.Warning);
            serializedObject.ApplyModifiedProperties();
            return;
        }

        EditorGUILayout.LabelField("Selected Node", EditorStyles.boldLabel);
        SerializedProperty nodeProp = dialogNodesProp.GetArrayElementAtIndex(cachedNodeIndex);

        EditorGUI.indentLevel++;

        // Node ID (read-only)
        EditorGUILayout.LabelField("Node ID:", selNode.nodeID);

        // NPC Name & Dialog Text
        var npcNameProp = nodeProp.FindPropertyRelative("npcName");
        var dialogTextProp = nodeProp.FindPropertyRelative("dialogText");
        EditorGUILayout.PropertyField(npcNameProp, new GUIContent("NPC Name"));
        EditorGUILayout.PropertyField(dialogTextProp, new GUIContent("Dialog Text"));

        // Start Card Game Toggle
        EditorGUILayout.Space(5);
        bool currentStartCardGame = selNode.IsStartCardGame;
        bool newStartCardGame = EditorGUILayout.Toggle("Start Card Game", currentStartCardGame);
        if (newStartCardGame != currentStartCardGame)
        {
            Undo.RecordObject(data, "Toggle Start Card Game");
            selNode.IsStartCardGame = newStartCardGame;
            EditorUtility.SetDirty(data);
        }

        // Options
        var optionsProp = nodeProp.FindPropertyRelative("options");
        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField($"Options ({optionsProp.arraySize})", EditorStyles.miniBoldLabel);
        EditorGUI.indentLevel++;

        for (int i = 0; i < optionsProp.arraySize; i++)
        {
            var optProp = optionsProp.GetArrayElementAtIndex(i);
            var optionTextProp = optProp.FindPropertyRelative("optionText");

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(optionTextProp, new GUIContent($"Option {i + 1}"));

            // Next node dropdown
            DialogOption optRef = selNode.options[i];
            int currentIndex = GetNodeIndexByID(data, optRef.nextNodeID);
            int newIndex = EditorGUILayout.Popup(currentIndex + 1, cachedNodeNames, GUILayout.Width(150)) - 1;

            if (newIndex != currentIndex)
            {
                Undo.RecordObject(data, "Set Option Next Node");
                optRef.nextNodeID = (newIndex < 0) ? "" : data.dialogNodes[newIndex].nodeID;
                EditorUtility.SetDirty(data);
            }

            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.Space(4);
        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("+ Add Option"))
        {
            Undo.RecordObject(data, "Add Dialog Option");
            selNode.options.Add(new DialogOption { optionText = "New Option", nextNodeID = "" });
            EditorUtility.SetDirty(data);
            serializedObject.Update();
            cachedNodeNames = null; // Invalidate cache
        }

        if (optionsProp.arraySize > 0 && GUILayout.Button("- Remove Last"))
        {
            Undo.RecordObject(data, "Remove Dialog Option");
            selNode.options.RemoveAt(selNode.options.Count - 1);
            EditorUtility.SetDirty(data);
            serializedObject.Update();
        }

        EditorGUILayout.EndHorizontal();
        EditorGUI.indentLevel -= 2;

        serializedObject.ApplyModifiedProperties();
    }

    int GetNodeIndexByID(DialogData data, string nodeID)
    {
        if (string.IsNullOrEmpty(nodeID)) return -1;

        // Fast lookup without creating intermediate list
        for (int i = 0; i < data.dialogNodes.Count; i++)
        {
            if (data.dialogNodes[i].nodeID == nodeID)
                return i;
        }
        return -1;
    }

    string[] GetNodeNamesArray(DialogData data)
    {
        string[] names = new string[data.dialogNodes.Count + 1];
        names[0] = "<None>";
        for (int i = 0; i < data.dialogNodes.Count; i++)
        {
            names[i + 1] = !string.IsNullOrEmpty(data.dialogNodes[i].npcName)
                ? data.dialogNodes[i].npcName
                : $"Node {i}";
        }
        return names;
    }
}

public class DialogVisualEditor : EditorWindow
{
    public static DialogNode inspectorSelectedNode;

    private DialogData currentData;
    private Vector2 offset;
    private float zoom = 1f;

    private DialogNode selectedNode;
    private DialogNode connectingNode;
    private int connectingOptionIndex = -1;

    private bool isDraggingNode = false;

    private const float NODE_WIDTH = 200f;
    private const float NODE_HEADER_HEIGHT = 30f;
    private const float OPTION_HEIGHT = 25f;
    private const float GRID_SIZE = 20f;

    private GUIStyle nodeStyle;
    private GUIStyle selectedNodeStyle;
    private GUIStyle entryNodeStyle;
    private GUIStyle endDialogStyle;
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

        endDialogStyle = new GUIStyle(nodeStyle);
        endDialogStyle.normal.background = MakeTexture(2, 2, new Color(1f, 0f, 0.2f));

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

        GUIStyle style;
        bool isEntryNode = node.nodeID == currentData.entryNodeID;

        if (node == selectedNode)
            style = selectedNodeStyle;
        else if (isEntryNode)
            style = entryNodeStyle;
        else if (node.IsStartCardGame)
            style = endDialogStyle;
        else
            style = nodeStyle;

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

            string optionLabel = string.IsNullOrEmpty(node.options[i].optionText)
                ? $"Option {i + 1}"
                : node.options[i].optionText;

            if (GUILayout.Button(optionLabel, EditorStyles.label))
            {
                SelectNodeInInspector(node);
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
            node.options.Add(new DialogOption { optionText = "New Option", nextNodeID = "" });
            EditorUtility.SetDirty(currentData);
        }

        GUILayout.EndArea();

        if (nodeRect.Contains(Event.current.mousePosition))
        {
            if (Event.current.type == EventType.MouseDown && Event.current.button == 0)
            {
                SelectNodeInInspector(node);
                GUI.changed = true;
            }
        }
    }

    void SelectNodeInInspector(DialogNode node)
    {
        if (selectedNode != node || inspectorSelectedNode != node)
        {
            selectedNode = node;
            inspectorSelectedNode = node;

            // Only change selection if not already focused on this asset
            if (Selection.activeObject != currentData)
            {
                Selection.activeObject = currentData;
            }

            // Force inspector repaint
            EditorUtility.SetDirty(currentData);
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
                if (!string.IsNullOrEmpty(node.options[i].nextNodeID))
                {
                    DialogNode nextNode = currentData.GetNodeByID(node.options[i].nextNodeID);
                    if (nextNode != null)
                    {
                        Vector2 start = node.options[i].connectionPoint;
                        Vector2 end = nextNode.position * zoom + offset + new Vector2(0, NODE_HEADER_HEIGHT * zoom / 2);

                        Handles.color = Color.white;
                        Handles.DrawBezier(
                            start, end,
                            start + Vector2.right * 50,
                            end + Vector2.left * 50,
                            Color.white, null, 2f
                        );

                        DrawArrow(end, (end - start).normalized);
                    }
                }
            }
        }

        Handles.EndGUI();
    }

    void DrawArrow(Vector2 position, Vector2 direction)
    {
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
                start, end,
                start + Vector2.right * 50,
                end + Vector2.left * 50,
                Color.yellow, null, 3f
            );

            Handles.EndGUI();
            Repaint();
        }
    }

    void ProcessEvents(Event e)
    {
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
                            connectingNode.options[connectingOptionIndex].nextNodeID = targetNode.nodeID;
                            EditorUtility.SetDirty(currentData);
                        }
                        connectingNode = null;
                        connectingOptionIndex = -1;
                        e.Use();
                    }
                    else if (selectedNode != null)
                    {
                        Undo.RecordObject(currentData, "Move Dialog Node");
                        isDraggingNode = true;
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
                break;

            case EventType.MouseDrag:
                if (e.button == 0 && selectedNode != null && isDraggingNode)
                {
                    selectedNode.position += e.delta / zoom;
                    e.Use();
                    Repaint();
                }
                else if (e.button == 2)
                {
                    offset += e.delta;
                    e.Use();
                }
                break;

            case EventType.MouseUp:
                if (e.button == 0 && isDraggingNode)
                {
                    isDraggingNode = false;
                    // Only mark dirty after dragging is complete
                    EditorUtility.SetDirty(currentData);
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
                    currentData.entryNodeID = clickedNode.nodeID;
                    EditorUtility.SetDirty(currentData);
                });

                menu.AddItem(new GUIContent("Toggle Start Card Game"), false, () =>
                {
                    Undo.RecordObject(currentData, "Toggle Start Card Game");
                    clickedNode.IsStartCardGame = !clickedNode.IsStartCardGame;
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
                new DialogOption { optionText = "Option 1", nextNodeID = "" }
            }
        };

        currentData.dialogNodes.Add(newNode);

        if (string.IsNullOrEmpty(currentData.entryNodeID))
            currentData.entryNodeID = newNode.nodeID;

        selectedNode = newNode;
        EditorUtility.SetDirty(currentData);
    }

    void DeleteNode(DialogNode node)
    {
        if (currentData == null) return;

        Undo.RecordObject(currentData, "Delete Dialog Node");

        // Remove all references to this node
        foreach (var n in currentData.dialogNodes)
        {
            foreach (var option in n.options)
            {
                if (option.nextNodeID == node.nodeID)
                    option.nextNodeID = "";
            }
        }

        if (currentData.entryNodeID == node.nodeID)
            currentData.entryNodeID = "";

        if (inspectorSelectedNode == node)
            inspectorSelectedNode = null;

        currentData.dialogNodes.Remove(node);

        if (selectedNode == node)
            selectedNode = null;

        EditorUtility.SetDirty(currentData);
    }
}
#endif