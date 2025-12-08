using UnityEngine;

public class CameraFollowSystem : MonoBehaviour
{
    //Vector3(-4,2,-5) for 3d
    [Header("Viewport")]
    public Camera MainCamera;
    public GameObject FollowingCharacter;

    [Header("Config")]
    public float Speed = 5f;
    public bool IsSnap;

    [Header("Optional Settings")]
    public Vector3 Offset = new Vector3(0, 0, -10f);
    public bool FollowX = true;
    public bool FollowY = true;
    public bool FollowZ = false;

    // Cached components
    private Transform _cameraTransform;
    private Transform _targetTransform;

    // Pre-calculated values
    private float _lerpSpeed;
    private Vector3 _targetPosition;

    private void Awake()
    {
        // Cache camera reference and transform
        if (MainCamera == null)
            MainCamera = Camera.main;

        if (MainCamera != null)
            _cameraTransform = MainCamera.transform;
    }

    private void Start()
    {
        // Cache target transform if available
        if (FollowingCharacter != null)
            _targetTransform = FollowingCharacter.transform;
    }

    private void LateUpdate()
    {
        if (_targetTransform == null)
            return;

        // Pre-calculate lerp speed once per frame
        _lerpSpeed = Speed * Time.fixedDeltaTime;

        if (IsSnap)
            SnapToCharacter();
        else
            GoToCharacterSmoothly();
    }

    private void GoToCharacterSmoothly()
    {
        // Calculate target position once
        _targetPosition.x = _targetTransform.position.x + Offset.x;
        _targetPosition.y = _targetTransform.position.y + Offset.y;
        _targetPosition.z = _targetTransform.position.z + Offset.z;

        // Get current position once
        Vector3 currentPos = _cameraTransform.position;

        // Only lerp the axes we need
        if (FollowX)
            currentPos.x = Mathf.Lerp(currentPos.x, _targetPosition.x, _lerpSpeed);

        if (FollowY)
            currentPos.y = Mathf.Lerp(currentPos.y, _targetPosition.y, _lerpSpeed);

        if (FollowZ)
            currentPos.z = Mathf.Lerp(currentPos.z, _targetPosition.z, _lerpSpeed);
        else
            currentPos.z = _targetPosition.z;

        // Single assignment to transform
        _cameraTransform.position = currentPos;
    }

    private void SnapToCharacter()
    {
        Vector3 currentPos = _cameraTransform.position;

        if (FollowX) currentPos.x = _targetTransform.position.x + Offset.x;
        if (FollowY) currentPos.y = _targetTransform.position.y + Offset.y;

        currentPos.z = FollowZ
            ? _targetTransform.position.z + Offset.z
            : _targetTransform.position.z + Offset.z;

        _cameraTransform.position = currentPos;
    }

    public void SetFollowTarget(GameObject newTarget)
    {
        if (newTarget == null)
        {
            FollowingCharacter = null;
            _targetTransform = null;
            return;
        }

        FollowingCharacter = newTarget;
        _targetTransform = newTarget.transform;

        if (IsSnap)
            SnapToCharacter();
    }
}