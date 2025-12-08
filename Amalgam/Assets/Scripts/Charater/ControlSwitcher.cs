using UnityEngine;
using UnityEngine.InputSystem;

public class ControlSwitcher : MonoBehaviour
{
    [Header("Tags")]
    public string playerATag = "PlayerA";
    public string playerBTag = "PlayerB";

    [Header("Input")]
    public InputActionReference switchAction; // e.g. "SwitchControl" (Button)

    [Header("Optional")]
    public AiPlayerMovement follower;
    public MouseMovment movmentMouse;
    public CameraFollowSystem CameraFollowSystem;

    private PlayerMovement _playerA;
    private PlayerMovement _playerB;
    private PlayerMovement _controlled;

    private void Start()
    {
        FindPlayersByTag();

        movmentMouse = GameObject.FindAnyObjectByType<MouseMovment>();
        CameraFollowSystem = GameObject.FindAnyObjectByType<CameraFollowSystem>();

        // default: control playerA if present, otherwise playerB
        if (_playerA != null) SetControlled(_playerA);
        else if (_playerB != null) SetControlled(_playerB);
    }

    private void OnEnable()
    {
        if (switchAction != null && switchAction.action != null)
        {
            switchAction.action.performed += OnSwitchPerformed;
            switchAction.action.Enable();
        }
    }

    private void OnDisable()
    {
        if (switchAction != null && switchAction.action != null)
        {
            switchAction.action.performed -= OnSwitchPerformed;
            switchAction.action.Disable();
        }
    }

    private void OnSwitchPerformed(InputAction.CallbackContext ctx)
    {
        // try to re-find players in case they were created at runtime or destroyed
        if (_playerA == null || _playerB == null) FindPlayersByTag();

        if (_controlled == _playerA && _playerB != null) SetControlled(_playerB);
        else if (_controlled == _playerB && _playerA != null) SetControlled(_playerA);
    }

    private void FindPlayersByTag()
    {
        // find Player A
        var goA = GameObject.FindWithTag(playerATag);
        if (goA != null && goA.scene.IsValid())
            _playerA = goA.GetComponent<PlayerMovement>();
        else
            _playerA = null;

        // find Player B
        var goB = GameObject.FindWithTag(playerBTag);
        if (goB != null && goB.scene.IsValid())
            _playerB = goB.GetComponent<PlayerMovement>();
        else
            _playerB = null;

        if (_playerA == null) 
            Debug.LogWarning($"ControlSwitcherByTag: couldn't find a scene object with tag '{playerATag}' (or it has no PlayerMovement).");

        if (_playerB == null) 
            Debug.LogWarning($"ControlSwitcherByTag: couldn't find a scene object with tag '{playerBTag}' (or it has no PlayerMovement).");
    }

    public void SetControlled(PlayerMovement which)
    {
        if (which == null)
        {
            Debug.LogError("ControlSwitcherByTag.SetControlled called with null.");
            return;
        }

        // ensure we have the latest references
        if (_playerA == null || _playerB == null)
            FindPlayersByTag();

        if (_playerA != null) _playerA.IsControlled = (which == _playerA);
        if (_playerB != null) _playerB.IsControlled = (which == _playerB);

        //if (_playerA != null) _playerA.IsControllMouse = (which == _playerA);
        //if (_playerB != null) _playerB.IsControllMouse = (which == _playerB);

        _controlled = which;

        // set follower target to the currently controlled character's transform (only if valid scene instance)
        if (follower != null)
        {
            if (_controlled != null && _controlled.gameObject != null && _controlled.gameObject.scene.IsValid())
            {
                follower.Target = _controlled.transform;
                CameraFollowSystem.SetFollowTarget(_controlled.gameObject);
            }
            else
            {

                follower.Target = null;
            }
        }

        // get the companion (the non-controlled player)
        PlayerMovement companion = (_controlled == _playerA) ? _playerB : _playerA;

        // DISABLE AI on the controlled character (we want player control)
        if (_controlled != null)
        {
            var aiControlled = _controlled.GetComponent<AiPlayerMovement>();
            if (aiControlled != null && _controlled.IsControllMouse == false)
            {
                aiControlled.enabled = false;
                aiControlled.Target = null;
            }
        }

        // ENABLE AI on the companion to follow the controlled character
        if (companion != null && companion != _controlled)
        {
            var aiCompanion = companion.GetComponent<AiPlayerMovement>();
            if (aiCompanion != null)
            {
                // Set the companion to follow the controlled character
                aiCompanion.Target = _controlled.transform;
                aiCompanion.enabled = true;
            }
            else
            {
                Debug.LogWarning($"ControlSwitcherByTag: {companion.name} has no AiPlayerMovement component, cannot follow!");
            }
        }

        Debug.Log($"ControlSwitcherByTag: now controlling {_controlled.name}, companion {(companion != null ? companion.name : "none")} is following.");
    }
    public void MoveToMarker()
    {
        // Check if mouse movement system and marker are valid
        if (movmentMouse == null || movmentMouse.CurrentMarker == null)
            return;

        // If the controlled character uses mouse control
        if (_controlled != null && _controlled.IsControllMouse && _controlled.IsControlled)
        {
            // Get the controlled character's AI component
            var aiControlled = _controlled.GetComponent<AiPlayerMovement>();
            
            if (aiControlled != null)
            {
                // Enable AI and set target to marker
                aiControlled.enabled = true;
               // CameraFollowSystem.Speed -= 0.25F; // DEBUG
                aiControlled.Target = movmentMouse.CurrentMarker.transform;

                Debug.Log($"Moving {_controlled.name} to marker at {movmentMouse.CurrentMarker.transform.position}");
            }
            else
            {
                Debug.LogWarning($"ControlSwitcher: {_controlled.name} has no AiPlayerMovement component for mouse movement!");
            }
        }
    }

    private void FixedUpdate()
    {
        MoveToMarker();
    }
}