using UnityEngine;

[System.Serializable]
public class DialogOption
{
    public string optionText;
    public DialogNode nextNode; // Direct reference instead of ID

#if UNITY_EDITOR
    [HideInInspector] public Vector2 connectionPoint;
#endif
}
