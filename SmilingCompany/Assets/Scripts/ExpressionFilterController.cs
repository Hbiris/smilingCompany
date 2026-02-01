using UnityEngine;
using UnityEngine.UI;

public class ExpressionFilterController : MonoBehaviour
{
    public ExpressionState expressionState;

    [Header("Expression Images (assign in Inspector)")]
    public Image expressionImage;
    public Sprite neutralSprite;
    public Sprite smileSprite;
    public Sprite sadSprite;

    [Header("Settings")]
    public float fadeSpeed = 6f;

    private Sprite currentSprite;
    private Sprite targetSprite;
    private float currentAlpha = 0f;
    private bool isFading = false;
    private bool isActive = false;

    void Start()
    {
        targetSprite = neutralSprite;
        currentSprite = neutralSprite;
        if (expressionImage != null)
        {
            expressionImage.sprite = neutralSprite;
            expressionImage.enabled = false; // Completely hide until activated
        }
    }

    /// <summary>
    /// Call this to show the expression filter (after calibration is complete)
    /// </summary>
    public void Activate()
    {
        isActive = true;
        currentAlpha = 0f;
        if (expressionImage != null)
        {
            expressionImage.enabled = true;
            SetAlpha(0f);
        }
    }

    void OnEnable()
    {
        if (expressionState != null)
            expressionState.OnExpressionChanged += HandleChange;
    }

    void OnDisable()
    {
        if (expressionState != null)
            expressionState.OnExpressionChanged -= HandleChange;
    }

    void Update()
    {
        if (expressionImage == null || !isActive) return;

        if (isFading)
        {
            // Fade out current
            currentAlpha = Mathf.Lerp(currentAlpha, 0f, 1f - Mathf.Exp(-fadeSpeed * Time.deltaTime));

            if (currentAlpha < 0.05f)
            {
                // Switch sprite and fade in
                currentSprite = targetSprite;
                expressionImage.sprite = currentSprite;
                isFading = false;
            }

            SetAlpha(currentAlpha);
        }
        else if (currentAlpha < 1f)
        {
            // Fade in new sprite
            currentAlpha = Mathf.Lerp(currentAlpha, 1f, 1f - Mathf.Exp(-fadeSpeed * Time.deltaTime));
            SetAlpha(currentAlpha);
        }
    }

    void HandleChange(ExpressionState.ExpressionType type)
    {
        Sprite newSprite = null;

        switch (type)
        {
            case ExpressionState.ExpressionType.Neutral:
                newSprite = neutralSprite;
                break;
            case ExpressionState.ExpressionType.Smile:
                newSprite = smileSprite;
                break;
            case ExpressionState.ExpressionType.Sad:
                newSprite = sadSprite;
                break;
        }

        if (newSprite != null && newSprite != currentSprite)
        {
            targetSprite = newSprite;
            isFading = true;
        }
    }

    void SetAlpha(float alpha)
    {
        if (expressionImage != null)
        {
            Color c = expressionImage.color;
            c.a = alpha;
            expressionImage.color = c;
        }
    }
}
