using UnityEngine;

public class NPCController : MonoBehaviour
{

    public DialogData npcDialog;
    public string npcName;
    public string npcType;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void StartNPCTalk()
    {
        DialogManager.Instance?.StartDialog(npcDialog);
    }
}
