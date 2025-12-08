using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DialogManager : MonoBehaviour
{
    public static DialogManager Instance;

    [Header("UI References")]
    public GameObject dialogPanel;
    public TextMeshProUGUI npcNameText;
    public TextMeshProUGUI dialogText;
    public Button optionAButton;
    public Button optionBButton;
    public Button optionCButton;
    public Button optionDButton;

    private TextMeshProUGUI optionAText;
    private TextMeshProUGUI optionBText;
    private TextMeshProUGUI optionCText;
    private TextMeshProUGUI optionDText;

    private DialogData currentDialog;
    private DialogNode currentNode;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);

        optionAText = optionAButton.GetComponentInChildren<TextMeshProUGUI>();
        optionBText = optionBButton.GetComponentInChildren<TextMeshProUGUI>();
        optionCText = optionCButton.GetComponentInChildren<TextMeshProUGUI>();
        optionDText = optionDButton.GetComponentInChildren<TextMeshProUGUI>();

        optionAButton.onClick.AddListener(() => SelectOption(0));
        optionBButton.onClick.AddListener(() => SelectOption(1));
        optionCButton.onClick.AddListener(() => SelectOption(2));
        optionDButton.onClick.AddListener(() => SelectOption(3));

        dialogPanel.SetActive(false);
    }

    public void StartDialog(DialogData dialog)
    {
        Debug.Log(dialog.entryNode.dialogText);
        Debug.Log(dialog.entryNode.npcName);
        //Debug
        currentNode = dialog.dialogNodes[0];
        SelectOption(0);
        //EndDebug
        currentDialog = dialog;
        dialogPanel.SetActive(true);

        if (dialog.entryNode != null)
            ShowDialog(dialog.entryNode);
        else if (dialog.dialogNodes.Count > 0)
            ShowDialog(dialog.dialogNodes[0]);
        else
            EndDialog();
    }

    void ShowDialog(DialogNode node)
    {
        if (node == null)
        {
            EndDialog();
            return;
        }

        currentNode = node;
        npcNameText.text = node.npcName;
        dialogText.text = node.dialogText;

        SetupOption(optionAButton, optionAText, node.options, 0);
        SetupOption(optionBButton, optionBText, node.options, 1);
        SetupOption(optionCButton, optionCText, node.options, 2);
        SetupOption(optionDButton, optionDText, node.options, 3);
    }

    void SetupOption(Button button, TextMeshProUGUI text, List<DialogOption> options, int index)
    {
        if (index < options.Count && !string.IsNullOrEmpty(options[index].optionText))
        {
            button.gameObject.SetActive(true);
            text.text = options[index].optionText;
        }
        else
        {
            button.gameObject.SetActive(false);
        }
    }

    void SelectOption(int optionIndex)
    {
        if (optionIndex >= currentNode.options.Count)
            return;

        DialogNode nextNode = currentNode.options[optionIndex].nextNode;
        DEBUG_PrintOptions(nextNode);
        if (nextNode == null || nextNode.options.Count == 0 || nextNode.dialogText == string.Empty)
            EndDialog();
        else
            ShowDialog(nextNode);
    }

    void EndDialog()
    {
        dialogPanel.SetActive(false);
        currentDialog = null;
        currentNode = null;
    }

    void DEBUG_PrintOptions(DialogNode dialog)
    {
        foreach(var c in dialog.options)
        {
            Debug.Log(c.optionText);
        }
    }
}
