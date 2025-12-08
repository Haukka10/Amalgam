using UnityEngine;
using UnityEngine.InputSystem;

public class MouseMovment : MonoBehaviour
{
    [Header("References")]
    public Camera mainCamera;
    public GameObject MarkerPrefab;
    public ControlSwitcher controlSwitcher;

    [Header("Settings")]
    public LayerMask npcLayerMask = -1; // Which layers to check for NPCs
    public bool IsFollowMouse = false;


    [Header("Runtime Info")]
    public GameObject CurrentMarker;

    // Cached components
    private Vector3 _lastPosMouse;
    private PlayerInput _playerInput;

    private void Awake()
    {
        // Auto-assign camera if not set
        if (mainCamera == null)
            mainCamera = Camera.main;

        // Auto-find ControlSwitcher if not set
        if (controlSwitcher == null)
            controlSwitcher = FindAnyObjectByType<ControlSwitcher>();
    }

    private void Update()
    {
        if(IsFollowMouse)
        {
            HandleMouseMovment();
        }

        // Check for mouse click
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            HandleMouseClick();
        }
    }

    private void HandleMouseMovment()
    {
        _lastPosMouse = GetMousePos();

        SetMarker();
    }

    private void HandleMouseClick()
    {
        _lastPosMouse = GetMousePos();

        // First check if we clicked on an NPC
        if (CheckIfClickedOnNPC(_lastPosMouse))
        {
            // We clicked an NPC, don't place marker
            return;
        }

        // No NPC clicked, place movement marker
        SetMarker();
    }

    private Vector3 GetMousePos()
    {
        Vector2 mouseScreenPos = Mouse.current.position.ReadValue();

        return GetMousePos2D(mouseScreenPos);
    }

    private Vector3 GetMousePos2D(Vector2 screenPos)
    {
        // Convert screen position to world position for 2D
        Vector3 worldPos = mainCamera.ScreenToWorldPoint(screenPos);
        worldPos.z = 0; // Set Z to 0 for 2D
        return worldPos;
    }

    private void SetMarker()
    {
        if (MarkerPrefab == null)
        {
            Debug.LogWarning("No marker prefab assigned!");
            return;
        }

        // Create or move existing marker
        if (CurrentMarker == null)
        {
            CurrentMarker = Instantiate(MarkerPrefab, _lastPosMouse, Quaternion.identity);
            if(!IsFollowMouse)
            {
                Debug.Log($"Marker placed at {_lastPosMouse}");
            }
        }
        else
        {
            CurrentMarker.transform.position = _lastPosMouse;
            Debug.Log($"Marker moved to {_lastPosMouse}");
        }
    }

    private bool CheckIfClickedOnNPC(Vector3 worldPos)
    {
        return CheckIfClickedOnNPC2D(worldPos);
    }

    private bool CheckIfClickedOnNPC2D(Vector3 worldPos)
    {
        // Raycast from the click position in 2D
        RaycastHit2D hit = Physics2D.Raycast(worldPos, Vector2.zero, 0f, npcLayerMask);

        if (hit.collider != null)
        {
            return TrySelectCharacter(hit.collider.gameObject);
        }

        return false;
    }

    private bool TrySelectCharacter(GameObject clickedObject)
    {
        // Check if clicked object has PlayerMovement component
        PlayerMovement playerMovement = clickedObject.GetComponent<PlayerMovement>();

        if (playerMovement != null && controlSwitcher != null)
        {
            Debug.Log($"Switching control to: {clickedObject.name}");
            controlSwitcher.SetControlled(playerMovement);
            return true;
        }

        // Also check parent in case collider is on child object
        playerMovement = clickedObject.GetComponentInParent<PlayerMovement>();
        if (playerMovement != null && controlSwitcher != null)
        {
            Debug.Log($"Switching control to: {playerMovement.gameObject.name}");
            controlSwitcher.SetControlled(playerMovement);
            return true;
        }

        return false;
    }
}