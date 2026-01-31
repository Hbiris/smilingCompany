using UnityEngine;

public class ExpressionFilterController : MonoBehaviour
{
    public ExpressionState expressionState;

    [Header("Canvas Groups")]
    public CanvasGroup neutral;
    public CanvasGroup smile;
    public CanvasGroup sad;

    public float fadeSpeed = 6f;

    CanvasGroup current;

    void Start()
    {
        SetActive(neutral);
    }

    void OnEnable()
    {
        expressionState.OnExpressionChanged += HandleChange;
    }

    void OnDisable()
    {
        expressionState.OnExpressionChanged -= HandleChange;
    }

    void Update()
    {
        // 平滑淡入淡出
        Fade(neutral);
        Fade(smile);
        Fade(sad);
    }

    void HandleChange(ExpressionState.ExpressionType type)
    {
        switch (type)
        {
            case ExpressionState.ExpressionType.Neutral:
                SetActive(neutral);
                break;

            case ExpressionState.ExpressionType.Smile:
                SetActive(smile);
                break;

            case ExpressionState.ExpressionType.Sad:
                SetActive(sad);
                break;
        }
    }

    void SetActive(CanvasGroup target)
    {
        current = target;
    }

    void Fade(CanvasGroup cg)
    {
        float target = (cg == current) ? 1f : 0f;
        cg.alpha = Mathf.Lerp(cg.alpha, target, 1f - Mathf.Exp(-fadeSpeed * Time.deltaTime));
    } 
}
