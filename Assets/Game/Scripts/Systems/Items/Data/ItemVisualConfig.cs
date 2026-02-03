using UnityEngine;

namespace Game.Systems.Items
{

    [System.Serializable]
    public struct ItemVisualConfig
    {
        public Sprite worldSprite;
        public ItemVisualRotationMode rotationMode;
        public float defaultAngleOffset; // 度
        public float holdDistance;       // 默认 1
        public float z;                  // 例如 -5
    }
}
