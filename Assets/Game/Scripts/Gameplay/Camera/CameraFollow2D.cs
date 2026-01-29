using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraFollow2D : MonoBehaviour
{
    public enum FollowMode
    {
        Fixed,
        HorizontalOnly,
        VerticalOnly,
        Both
    }

    [Header("Target")]
    public Transform target;

    [Header("Follow")]
    public FollowMode followMode = FollowMode.Both;
    public float smoothTime = 0.12f;
    public Vector3 offset = new(0, 0, -10);

    [Header("Mode Anchors")]
    public Vector2 fixedPosition2D = Vector2.zero;
    public float lockedAxisValue = 0f;

    [Header("Bounds (Optional)")]
    public BoxCollider2D bounds;

    private Vector3 velocity;
    private Camera cam;

    void Awake()
    {
        cam = GetComponent<Camera>();
    }

    void LateUpdate()
    {
        if (target == null && followMode != FollowMode.Fixed) return;

        Vector3 desired = GetDesiredPosition();
        Vector3 smoothPos = Vector3.SmoothDamp(transform.position, desired, ref velocity, smoothTime);

        if (bounds != null && cam != null && cam.orthographic)
        {
            smoothPos = ClampToBounds(smoothPos);
        }

        transform.position = smoothPos;
    }

    private Vector3 GetDesiredPosition()
    {
        Vector3 targetPos = (target != null) ? target.position + offset : new Vector3(0, 0, offset.z);

        float x = transform.position.x;
        float y = transform.position.y;

        switch (followMode)
        {
            case FollowMode.Fixed:
                x = fixedPosition2D.x;
                y = fixedPosition2D.y;
                break;

            case FollowMode.HorizontalOnly:
                x = targetPos.x;
                y = lockedAxisValue;
                break;

            case FollowMode.VerticalOnly:
                x = lockedAxisValue;
                y = targetPos.y;
                break;

            case FollowMode.Both:
                x = targetPos.x;
                y = targetPos.y;
                break;
        }

        return new Vector3(x, y, offset.z);
    }

    Vector3 ClampToBounds(Vector3 pos)
    {
        Bounds b = bounds.bounds;

        float halfHeight = cam.orthographicSize;
        float halfWidth = halfHeight * cam.aspect;

        float minX = b.min.x + halfWidth;
        float maxX = b.max.x - halfWidth;
        float minY = b.min.y + halfHeight;
        float maxY = b.max.y - halfHeight;

        pos.x = Mathf.Clamp(pos.x, minX, maxX);
        pos.y = Mathf.Clamp(pos.y, minY, maxY);
        pos.z = offset.z;

        return pos;
    }


    public void SetFollowMode(FollowMode mode, bool snapImmediately = false)
    {
        followMode = mode;
        velocity = Vector3.zero;

        if (snapImmediately)
            SnapToTarget();
    }

    public void SetBounds(BoxCollider2D newBounds, bool snapImmediately = false)
    {
        bounds = newBounds;
        velocity = Vector3.zero;

        if (snapImmediately)
            SnapToTarget();
    }

    public void SnapToTarget()
    {
         Vector3 p = GetDesiredPosition();
        if (bounds != null && cam != null && cam.orthographic)
            p = ClampToBounds(p);

        transform.position = p;
    }
}
