using UnityEngine;

public class LevelCameraSettings : MonoBehaviour
{
    public CameraFollow2D.FollowMode defaultMode = CameraFollow2D.FollowMode.Both;

    [Tooltip("可选：这个关卡的相机边界（BoxCollider2D）。不填就不限制。")]
    public BoxCollider2D bounds;

    [Tooltip("可选：进入关卡时相机先瞬移到目标（避免从上个场景飞过来）")]
    public bool snapOnEnter = true;
}
