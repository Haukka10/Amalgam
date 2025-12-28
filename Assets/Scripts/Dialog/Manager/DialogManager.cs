using System.Collections.Generic;
using CardGame.Manager.Main;
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
        currentDialog = dialog;
        dialogPanel.SetActive(true);

        // Start with entry node or first node
        DialogNode startNode = dialog.entryNode;
        if (startNode == null && dialog.dialogNodes.Count > 0)
        {
            startNode = dialog.dialogNodes[0];
        }

        if (startNode != null)
        {
            Debug.Log($"Starting dialog: {startNode.npcName} - {startNode.dialogText}");
            ShowDialog(startNode);
        }
        else
        {
            Debug.LogError("No valid entry node found!");
            EndDialog();
        }
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
        if (currentNode == null || optionIndex >= currentNode.options.Count)
            return;

        // Get next node by ID instead of direct reference
        string nextNodeID = currentNode.options[optionIndex].nextNodeID;

        if (string.IsNullOrEmpty(nextNodeID))
        {
            // Empty nextNodeID means end of dialog
            EndDialog();
            return;
        }

        DialogNode nextNode = currentDialog.GetNodeByID(nextNodeID);

        if (nextNode == null)
        {
            Debug.LogError($"Could not find node with ID: {nextNodeID}");
            EndDialog();
            return;
        }

        DEBUG_PrintOptions(nextNode);

        if (nextNode.options.Count == 0 || string.IsNullOrEmpty(nextNode.dialogText))
        {
            EndDialog();
        }
        else
        {
            ShowDialog(nextNode);
        }
    }

    void EndDialog()
    {
        if (currentDialog == null)
        {
            Debug.LogError("currentDialog is not set.");
            return;
        }

        dialogPanel.SetActive(false);

        // FIXED: Check if the CURRENT NODE triggers card game
        if (currentNode != null && currentNode.IsStartCardGame)
        {
            var c = FindAnyObjectByType<RitualGameManager>();
            if (c != null)
            {
                c.SetType(0);
                // Delay to setup board
                Invoke(nameof(StartCardGame), 0.75f);
            }
        }

        currentDialog = null;
        currentNode = null;
    }

    void StartCardGame()
    {
        var c = FindAnyObjectByType<RitualGameManager>();
        if (c != null)
        {
            c.StartRitualGame();
        }
    }

    void DEBUG_PrintOptions(DialogNode dialog)
    {
        Debug.Log($"Node '{dialog.nodeID}' has {dialog.options.Count} options:");
        foreach (var option in dialog.options)
        {
            Debug.Log($"  - {option.optionText} -> {option.nextNodeID}");
        }
    }
}