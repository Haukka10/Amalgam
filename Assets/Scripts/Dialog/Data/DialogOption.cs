using UnityEngine;

[System.Serializable]
public class DialogOption
{
    [TextArea(2, 4)]
    public string optionText;

    // CHANGED: Use string ID instead of direct node reference
    public string nextNodeID;

#if UNITY_EDITOR
    [HideInInspector] public Vector2 connectionPoint; // For visual editor
#endif
}