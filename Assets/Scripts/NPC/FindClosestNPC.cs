using UnityEngine;
using UnityEngine.InputSystem;

public class FindClosestNPC : MonoBehaviour
{
    [SerializeField] private float detectionRadius = 5f;
    [SerializeField] private LayerMask targetLayer;

    [SerializeField] private InputActionReference interactAction;

    private GameObject _FindedNPC = null;

    void OnEnable()
    {
        if (interactAction != null)
        {
            interactAction.action.Enable();
            interactAction.action.performed += OnInteract;
        }
    }

    void OnDisable()
    {
        if (interactAction != null)
        {
            interactAction.action.performed -= OnInteract;
            interactAction.action.Disable();
        }
    }

    void OnInteract(InputAction.CallbackContext context)
    {

        if (_FindedNPC != null)
        {
            Debug.Log($"Interacting with: {_FindedNPC.name}");
            var c = _FindedNPC.GetComponent<NPCController>();
            c.StartNPCTalk();
        }
        else
        {
            Debug.Log("No object nearby to interact with!");
        }
    }

    // Update is called once per frame
    void LateUpdate()
    {
        _FindedNPC = FindNPC();
    }

    private GameObject FindNPC()
    {
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, detectionRadius, targetLayer);

        if (colliders.Length <= 0)
            return null;

        GameObject closest = null;
        float minDistSqr = Mathf.Infinity;
        Vector2 currentPos = transform.position;

        foreach (Collider2D col in colliders)
        {
            float distSqr = (col.transform.position - (Vector3)currentPos).sqrMagnitude;

            if (distSqr < minDistSqr)
            {
                closest = col.gameObject;
                minDistSqr = distSqr;
            }
        }

        return closest;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}
