using UnityEngine;

[ExecuteAlways]
[DisallowMultipleComponent]
public class GlitchSpriteController : MonoBehaviour
{
    private static readonly int IntensityId = Shader.PropertyToID("_Intensity");
    private static readonly int TimeScaleId = Shader.PropertyToID("_TimeScale");
    private static readonly int SeedId = Shader.PropertyToID("_Seed");
    private static readonly int UseWorldZoneId = Shader.PropertyToID("_UseWorldZone");
    private static readonly int ZoneCenterId = Shader.PropertyToID("_ZoneCenter");
    private static readonly int ZoneSizeId = Shader.PropertyToID("_ZoneSize");
    private static readonly int ZoneSoftnessId = Shader.PropertyToID("_ZoneSoftness");

    [Header("Targets")]
    [SerializeField] private SpriteRenderer[] targets;
    [SerializeField] private bool autoFindChildSprites = true;

    [Header("Glitch")]
    [SerializeField, Range(0f, 1f)] private float intensity = 0.75f;
    [SerializeField, Min(0f)] private float timeScale = 1f;
    [SerializeField] private float seed = 0f;
    [SerializeField] private bool randomizeSeedOnPlay = true;

    [Header("World Zone")]
    [SerializeField] private bool useWorldZone = false;
    [SerializeField] private bool followTransformAsZoneCenter = true;
    [SerializeField] private Vector2 zoneCenter;
    [SerializeField] private Vector2 zoneOffset;
    [SerializeField] private Vector2 zoneSize = Vector2.one;
    [SerializeField, Min(0f)] private float zoneSoftness = 0.15f;

    private readonly MaterialPropertyBlock propertyBlock = new MaterialPropertyBlock();

    public float Intensity
    {
        get => intensity;
        set => intensity = Mathf.Clamp01(value);
    }

    private void Reset()
    {
        CollectTargets();
        ApplyProperties();
    }

    private void Awake()
    {
        EnsureTargets();
    }

    private void OnEnable()
    {
        EnsureTargets();

        if (Application.isPlaying && randomizeSeedOnPlay && Mathf.Approximately(seed, 0f))
            seed = Random.Range(1f, 9999f);

        ApplyProperties();
    }

    private void LateUpdate()
    {
        ApplyProperties();
    }

    private void OnValidate()
    {
        zoneSize.x = Mathf.Max(0.0001f, zoneSize.x);
        zoneSize.y = Mathf.Max(0.0001f, zoneSize.y);
        zoneSoftness = Mathf.Max(0f, zoneSoftness);

        EnsureTargets();
        ApplyProperties();
    }

    private void OnDisable()
    {
        ClearProperties();
    }

    [ContextMenu("Collect Sprite Targets")]
    public void CollectTargets()
    {
        if (autoFindChildSprites)
        {
            targets = GetComponentsInChildren<SpriteRenderer>(true);
            return;
        }

        var self = GetComponent<SpriteRenderer>();
        targets = self != null ? new[] { self } : System.Array.Empty<SpriteRenderer>();
    }

    public void SetIntensity(float value)
    {
        intensity = Mathf.Clamp01(value);
        ApplyProperties();
    }

    public void SetWorldZone(bool enabled, Vector2 center, Vector2 size)
    {
        useWorldZone = enabled;
        zoneCenter = center;
        zoneSize = new Vector2(Mathf.Max(0.0001f, size.x), Mathf.Max(0.0001f, size.y));
        ApplyProperties();
    }

    private void EnsureTargets()
    {
        if (targets != null && targets.Length > 0)
            return;

        CollectTargets();
    }

    private void ApplyProperties()
    {
        EnsureTargets();
        if (targets == null || targets.Length == 0)
            return;

        Vector2 resolvedZoneCenter = followTransformAsZoneCenter
            ? (Vector2)transform.position + zoneOffset
            : zoneCenter + zoneOffset;

        Vector4 zoneCenterVector = new Vector4(resolvedZoneCenter.x, resolvedZoneCenter.y, 0f, 0f);
        Vector4 zoneSizeVector = new Vector4(
            Mathf.Max(0.0001f, zoneSize.x),
            Mathf.Max(0.0001f, zoneSize.y),
            0f,
            0f);

        for (int i = 0; i < targets.Length; i++)
        {
            var target = targets[i];
            if (target == null)
                continue;

            target.GetPropertyBlock(propertyBlock);
            propertyBlock.SetFloat(IntensityId, intensity);
            propertyBlock.SetFloat(TimeScaleId, timeScale);
            propertyBlock.SetFloat(SeedId, seed);
            propertyBlock.SetFloat(UseWorldZoneId, useWorldZone ? 1f : 0f);
            propertyBlock.SetVector(ZoneCenterId, zoneCenterVector);
            propertyBlock.SetVector(ZoneSizeId, zoneSizeVector);
            propertyBlock.SetFloat(ZoneSoftnessId, zoneSoftness);
            target.SetPropertyBlock(propertyBlock);
        }
    }

    private void ClearProperties()
    {
        EnsureTargets();
        if (targets == null || targets.Length == 0)
            return;

        propertyBlock.Clear();
        for (int i = 0; i < targets.Length; i++)
        {
            var target = targets[i];
            if (target == null)
                continue;

            target.SetPropertyBlock(propertyBlock);
        }
    }
}
