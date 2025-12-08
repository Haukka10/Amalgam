using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class DialogNode
{
    public string nodeID; // Auto-generated
    public string npcName;
    [TextArea(3, 6)]
    public string dialogText;
    public List<DialogOption> options = new List<DialogOption>();

#if UNITY_EDITOR
    [HideInInspector] public Vector2 position;
    [HideInInspector] public bool isEntryNode;
#endif
}
