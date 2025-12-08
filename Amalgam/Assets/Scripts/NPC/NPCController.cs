using UnityEngine;

public class NPCController : MonoBehaviour
{

    public DialogData npcDialog;
    public string npcName;
    public string npcType;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        DialogManager.Instance?.StartDialog(npcDialog);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
