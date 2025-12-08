using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewDialog", menuName = "Dialog System/Dialog Data")]
public class DialogData : ScriptableObject
{
    public List<DialogNode> dialogNodes = new List<DialogNode>();
    public DialogNode entryNode;

#if UNITY_EDITOR
    [HideInInspector] public Vector2 editorOffset;
    [HideInInspector] public float editorZoom = 1f;
#endif
}
