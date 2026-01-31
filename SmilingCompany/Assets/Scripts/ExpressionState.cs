using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class ExpressionState : MonoBehaviour
{
    public enum ExpressionType
    {
        Neutral,
        Smile,
        Sad
    }

    public ExpressionType Current { get; private set; } = ExpressionType.Neutral;

    public event Action<ExpressionType> OnExpressionChanged;

    void Update()
    {
        // 临时调试按键
        if (Keyboard.current.digit1Key.wasPressedThisFrame)
            SetExpression(ExpressionType.Neutral);

        if (Keyboard.current.digit2Key.wasPressedThisFrame)
            SetExpression(ExpressionType.Smile);

        if (Keyboard.current.digit3Key.wasPressedThisFrame)
            SetExpression(ExpressionType.Sad);
    }

    public void SetExpression(ExpressionType type)
    {
        if (Current == type) return;

        Current = type;
        OnExpressionChanged?.Invoke(Current);
    }
}
