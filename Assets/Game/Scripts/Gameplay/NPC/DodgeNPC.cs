using UnityEngine;
using Game.Gameplay.Combat;
using System.Collections;

public class OmniDodge : MonoBehaviour
{
    [Header("Identity")]
    public string npcId = "npc_ghost";

    [Header("Teleport Points")]
    public Transform[] teleportSpots;
    public float dodgeDelay = 0.02f;

    [Header("VFX & Audio")]
    public GameObject afterimagePrefab;
    public AudioClip dodgeSfx; 
    private Health2D health;
    private int lastSpotIndex = -1;
    private bool isDodging = false;

    private void Awake()
    {
        health = GetComponent<Health2D>();
    }

    private void OnEnable()
    {
        if (health != null) health.OnDamaged += OnValueDamaged;
    }

    private void OnDisable()
    {
        if (health != null) health.OnDamaged -= OnValueDamaged;
    }

    // 路径 A：针对子弹（穿透层）
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Bullet") && !isDodging)
        {
            StartCoroutine(DodgeRoutine());
        }
    }

    // 路径 B：针对爆炸等数值伤害
    private void OnValueDamaged(DamageInfo info)
    {
        if (!isDodging)
        {
            StartCoroutine(DodgeRoutine());
        }
    }

    private IEnumerator DodgeRoutine()
    {
        isDodging = true;

        // 1. 播放音频
        PlayDodgeSfx();

        SpawnAfterimage();

        if (dodgeDelay > 0)
            yield return new WaitForSeconds(dodgeDelay);

        PerformTeleport();

        yield return new WaitForSeconds(0.05f); // 防抖
        isDodging = false;
        
        if (GameRoot.I != null && GameRoot.I.Triggers != null)
        {
            GameRoot.I.Triggers.Raise(new NpcDodgeEvent(npcId, gameObject));
        }
    }

    private void PlayDodgeSfx()
    {
        if (dodgeSfx == null) return;
        
        // 使用 GameRoot 提供的全局音效源
        if (GameRoot.I != null && GameRoot.I.globalSfxSource != null)
        {
            GameRoot.I.globalSfxSource.PlayOneShot(dodgeSfx);
        }
    }

    private void PerformTeleport()
    {
        if (teleportSpots == null || teleportSpots.Length == 0) return;
        int idx = Random.Range(0, teleportSpots.Length);
        if (idx == lastSpotIndex) idx = (idx + 1) % teleportSpots.Length;
        transform.position = teleportSpots[idx].position;
        lastSpotIndex = idx;
    }

    private void SpawnAfterimage()
    {
        if (afterimagePrefab == null) return;
        var ghost = Instantiate(afterimagePrefab, transform.position, transform.rotation);
        var ghostSr = ghost.GetComponent<SpriteRenderer>();
        var mySr = GetComponentInChildren<SpriteRenderer>();
        if (ghostSr && mySr)
        {
            ghostSr.sprite = mySr.sprite;
            ghostSr.flipX = mySr.flipX;
        }
    }
}