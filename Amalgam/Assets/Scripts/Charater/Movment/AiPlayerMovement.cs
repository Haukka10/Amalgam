using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class AiPlayerMovement : MonoBehaviour
{
    [Tooltip("Assign a scene Transform (not a prefab asset). This script will ignore non-scene Transforms.")]
    public Transform Target;

    public float FollowSpeed = 3.5f;
    public float StoppingDistance = 0.2f;

    Rigidbody2D rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void FixedUpdate()
    {
        // defensive: only follow valid scene Transforms (prefab assets are not valid)
        if (Target == null || !IsSceneTransform(Target))
        {
            // nothing to follow
            return;
        }

        Vector2 current = rb.position;
        Vector2 targetPos = Target.position;
        Vector2 toTarget = targetPos - current;
        float dist = toTarget.magnitude;

        if (dist > StoppingDistance)
        {
            Vector2 direction = toTarget.normalized;
            Vector2 movement = direction * FollowSpeed;
            Vector2 newPos = current + movement * Time.fixedDeltaTime;
            rb.MovePosition(newPos);

            // If you have a renderer class, tell it the movement so it can animate
            var cr = GetComponent<CharacterRenderer>();
            if (cr != null) 
                cr.SetDirection(movement);
        }
        else
        {
            var cr = GetComponent<CharacterRenderer>();
            if (cr != null) 
                cr.SetDirection(Vector2.zero);
        }
    }

    // helper: returns true if transform belongs to a scene instance (not an asset prefab)
    private bool IsSceneTransform(Transform t)
    {
        return t != null && t.gameObject != null && t.gameObject.scene.IsValid();
    }
}
