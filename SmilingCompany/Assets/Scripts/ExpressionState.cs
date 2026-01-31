using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class ExpressionState : MonoBehaviour
{
    public bool IsSmiling { get; private set; }

    public event Action<bool> OnSmileChanged;

    void Start()
    {
        SetSmiling(false);
    }

    void Update()
    {
        // 临时：按 R 切换笑/不笑
        if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
        {
            SetSmiling(!IsSmiling);
        }
    }

    public void SetSmiling(bool smiling)
    {
        if (IsSmiling == smiling) return;
        IsSmiling = smiling;
        OnSmileChanged?.Invoke(IsSmiling);
    }
}
