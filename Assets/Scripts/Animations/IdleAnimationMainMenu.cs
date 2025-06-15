using UnityEngine;

public class IdleAnimationMainMenu : MonoBehaviour
{
    public bool isIdleActive = true;

    public float scaleSpeed = 1.5f;     // Speed of breathing
    public float scaleAmount = 0.02f;   // Amount of vertical scale for breathing

    private RectTransform rectTransform;
    private Vector2 originalAnchoredPos;
    private Vector3 originalScale;

    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        originalAnchoredPos = rectTransform.anchoredPosition;
        originalScale = rectTransform.localScale;
    }

    void Update()
    {
        if (!isIdleActive) return;

        // Breathing effect (scales up/down vertically only)
        float scaleY = originalScale.y + Mathf.Sin(Time.time * scaleSpeed) * scaleAmount;
        rectTransform.localScale = new Vector3(originalScale.x, scaleY, originalScale.z);

        // Keep anchored position unchanged
        rectTransform.anchoredPosition = originalAnchoredPos;
    }
}
