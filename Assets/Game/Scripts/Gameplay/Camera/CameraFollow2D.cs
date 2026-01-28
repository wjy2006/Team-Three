using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraFollow2D : MonoBehaviour
{
    public enum FollowMode
    {
        Fixed,          // 固定到 fixedPosition2D
        HorizontalOnly, // 只跟随X，Y锁到 lockedAxisValue
        VerticalOnly,   // 只跟随Y，X锁到 lockedAxisValue
        Both            // X + Y
    }

    [Header("Target")]
    public Transform target;

    [Header("Follow")]
    public FollowMode followMode = FollowMode.Both;
    public float smoothTime = 0.12f;
    public Vector3 offset = new(0, 0, -10);

    [Header("Mode Anchors")]
    [Tooltip("Fixed 模式下相机要去的世界坐标（2D部分）。你要回到(0,0)就保持默认。")]
    public Vector2 fixedPosition2D = Vector2.zero;

    [Tooltip("HorizontalOnly/VerticalOnly 时，被锁住的轴要固定到的值（默认0）。")]
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
                y = lockedAxisValue;   // ✅ 不保留旧Y，锁到固定值（默认0）
                break;

            case FollowMode.VerticalOnly:
                x = lockedAxisValue;   // ✅ 不保留旧X，锁到固定值（默认0）
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

    // 运行时切换模式（给剧情/触发器用）
    public void SetFollowMode(FollowMode mode, bool snapImmediately = false)
    {
        followMode = mode;

        // ✅ 切模式清掉 SmoothDamp 速度，避免“漂移”
        velocity = Vector3.zero;

        if (snapImmediately)
            SnapToModeTarget();
    }

    public void SetBounds(BoxCollider2D newBounds, bool snapImmediately = false)
    {
        bounds = newBounds;
        velocity = Vector3.zero;

        if (snapImmediately)
            SnapToModeTarget();
    }

    // ✅ 直接把相机瞬移到“当前模式应该在的位置”
    public void SnapToModeTarget()
    {
        Vector3 p = GetDesiredPosition();
        if (bounds != null && cam != null && cam.orthographic)
            p = ClampToBounds(p);

        transform.position = p;
    }

    // 保留你原来的函数名（如果外部已经在用）
    public void SnapToTarget()
    {
        // 在新逻辑下，SnapToModeTarget 更准确
        SnapToModeTarget();
    }
}
