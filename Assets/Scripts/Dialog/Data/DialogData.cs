using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewDialog", menuName = "Dialog/Dialog Data")]
public class DialogData : ScriptableObject
{
    // CHANGED: Use string ID for entry node
    public string entryNodeID;
    public List<DialogNode> dialogNodes = new List<DialogNode>();

#if UNITY_EDITOR
    [HideInInspector] public Vector2 editorOffset;
    [HideInInspector] public float editorZoom = 1f;
#endif

    // Helper property to get entry node
    public DialogNode entryNode => GetNodeByID(entryNodeID);

    // Helper method to find node by ID
    public DialogNode GetNodeByID(string nodeID)
    {
        if (string.IsNullOrEmpty(nodeID))
            return null;

        return dialogNodes.Find(node => node.nodeID == nodeID);
    }

    // Helper to check if any node triggers card game
    public bool HasCardGameNode()
    {
        return dialogNodes.Exists(node => node.IsStartCardGame);
    }
}