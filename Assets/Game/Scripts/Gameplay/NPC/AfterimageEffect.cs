using UnityEngine;

public class AfterimageEffect : MonoBehaviour
{
    public float fadeSpeed = 5f; // 消失速度
    private SpriteRenderer sr;
    private Color color;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        color = sr.color;
    }

    private void Update()
    {
        color.a -= fadeSpeed * Time.deltaTime;
        sr.color = color;
        if (color.a <= 0) Destroy(gameObject);
    }
}