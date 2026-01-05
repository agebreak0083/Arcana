using UnityEngine;
using TMPro;
using System.Collections;

/// <summary>
/// 데미지 텍스트 애니메이션 처리
/// </summary>
public class DamageText : MonoBehaviour
{
    [Header("Animation Settings")]
    public float moveSpeed = 2f;
    public float fadeSpeed = 1f;
    public float lifetime = 1.5f;
    public float moveUpDistance = 1f;

    public float normalFontSize = 36f;
    public float criticalFontSize = 48f;
    public Color normalColor = new Color(1f, 1f, 1f, 1f);
    public Color criticalColor = new Color(1f, 0.2f, 0.2f, 1f);

    private TextMeshProUGUI textMesh;
    private Color originalColor;
    private Vector3 startPosition;
    private float timer = 0f;

    private void Awake()
    {
        textMesh = GetComponent<TextMeshProUGUI>();
        if (textMesh != null)
        {
            originalColor = textMesh.color;
        }
    }

    private void Start()
    {
        startPosition = transform.position;
        Destroy(gameObject, lifetime);
    }

    private void Update()
    {
        timer += Time.deltaTime;
        float progress = timer / lifetime;

        // 위로 이동
        transform.position = startPosition + Vector3.up * (moveUpDistance * progress);

        // 페이드 아웃
        if (textMesh != null)
        {
            Color color = originalColor;
            color.a = Mathf.Lerp(1f, 0f, progress);
            textMesh.color = color;
        }
    }

    /// <summary>
    /// 데미지 텍스트 설정
    /// </summary>
    public void Setup(int damage, bool isCritical)
    {
        if (textMesh == null)
        {
            textMesh = GetComponent<TextMeshProUGUI>();
        }

        textMesh.text = damage.ToString();

        if (isCritical)
        {
            // 크리티컬: 빨간색, 더 큰 폰트
            textMesh.color = criticalColor;
            textMesh.fontSize = criticalFontSize;
            textMesh.fontStyle = FontStyles.Bold;
        }
        else
        {
            // 일반: 흰색
            textMesh.color = normalColor;
            textMesh.fontSize = normalFontSize;
            textMesh.fontStyle = FontStyles.Normal;
        }

        originalColor = textMesh.color;
    }
}
