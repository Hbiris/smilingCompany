using UnityEngine;
using UnityEngine.UI;

public class SmileFilterOverlay : MonoBehaviour
{
    [Header("References")]
    public ExpressionState expressionState;
    public CanvasGroup canvasGroup;   // 用来淡入淡出
    public Image softTint;            // 柔和滤镜层
    public Image border;              // 边框层（先简单用 Image 占位）

    [Header("Tuning")]
    public float fadeSpeed = 8f;      // 越大越快
    public float smilingAlpha = 1f;   // 笑时整体强度（CanvasGroup alpha）

    float targetAlpha = 0f;

    void Reset()
    {
        canvasGroup = GetComponent<CanvasGroup>();
    }

    void Awake()
    {
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();

        // 不挡点击
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;
    }

    void OnEnable()
    {
        if (expressionState != null)
            expressionState.OnSmileChanged += HandleSmileChanged;
    }

    void OnDisable()
    {
        if (expressionState != null)
            expressionState.OnSmileChanged -= HandleSmileChanged;
    }

    void Start()
    {
        // 初始隐藏
        canvasGroup.alpha = 0f;
        targetAlpha = 0f;
    }

    void Update()
    {
        canvasGroup.alpha = Mathf.Lerp(canvasGroup.alpha, targetAlpha, 1f - Mathf.Exp(-fadeSpeed * Time.deltaTime));
    }

    void HandleSmileChanged(bool isSmiling)
    {
        targetAlpha = isSmiling ? smilingAlpha : 0f;
    }
}
