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

    [Header("Face Detection")]
    [SerializeField] private bool useFaceDetection = true;
    [SerializeField] private float detectionInterval = 0.1f; // How often to check face
    [SerializeField] private float confidenceThreshold = 0.4f;

    public ExpressionType Current { get; private set; } = ExpressionType.Neutral;
    public bool IsCalibrated => calibrator != null && calibrator.IsCalibrated;

    public event Action<ExpressionType> OnExpressionChanged;

    private ExpressionCalibrator calibrator;
    private float lastDetectionTime;

    void Start()
    {
        calibrator = ExpressionCalibrator.Instance;
        if (calibrator == null)
        {
            calibrator = FindFirstObjectByType<ExpressionCalibrator>();
        }
    }

    void Update()
    {
        // Face detection (after calibration)
        if (useFaceDetection && calibrator != null && calibrator.IsCalibrated)
        {
            if (Time.time - lastDetectionTime >= detectionInterval)
            {
                lastDetectionTime = Time.time;
                DetectAndSetExpression();
            }
        }

        // Debug keyboard controls (always available)
        if (Keyboard.current != null)
        {
            if (Keyboard.current.digit1Key.wasPressedThisFrame)
                SetExpression(ExpressionType.Neutral);

            if (Keyboard.current.digit2Key.wasPressedThisFrame)
                SetExpression(ExpressionType.Smile);

            if (Keyboard.current.digit3Key.wasPressedThisFrame)
                SetExpression(ExpressionType.Sad);
        }
    }

    private void DetectAndSetExpression()
    {
        var (detected, confidence) = calibrator.DetectExpression();

        if (confidence >= confidenceThreshold)
        {
            // Convert calibrator expression to our expression type
            ExpressionType newType = detected switch
            {
                ExpressionCalibrator.Expression.Neutral => ExpressionType.Neutral,
                ExpressionCalibrator.Expression.Smile => ExpressionType.Smile,
                ExpressionCalibrator.Expression.Sad => ExpressionType.Sad,
                _ => ExpressionType.Neutral
            };

            SetExpression(newType);
        }
    }

    public void SetExpression(ExpressionType type)
    {
        if (Current == type) return;

        Current = type;
        OnExpressionChanged?.Invoke(Current);
    }
}
