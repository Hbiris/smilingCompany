using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Calibrates and detects expressions: Neutral, Smile, Sad
/// </summary>
public class ExpressionCalibrator : MonoBehaviour
{
    public enum Expression { Neutral, Smile, Sad }

    [Header("Calibration Settings")]
    [SerializeField] private float calibrationDuration = 2f;
    [SerializeField] private float sampleInterval = 0.1f;

    public static ExpressionCalibrator Instance { get; private set; }

    public bool IsCalibrated => calibratedExpressions.Count == 3;
    public bool IsCalibrating { get; private set; }
    public Expression? CurrentCalibrating { get; private set; }
    public float CalibrationProgress { get; private set; }

    public event Action<Expression> OnCalibrationStarted;
    public event Action<Expression> OnCalibrationComplete;
    public event Action OnAllCalibrationComplete;

    // Key blendshapes for comparison
    private static readonly string[] KeyBlendshapes = new string[]
    {
        "mouthSmileLeft", "mouthSmileRight",
        "mouthFrownLeft", "mouthFrownRight",
        "browDownLeft", "browDownRight",
        "browInnerUp", "browOuterUpLeft", "browOuterUpRight",
        "mouthPucker", "jawOpen", "eyeSquintLeft", "eyeSquintRight"
    };

    private Dictionary<Expression, Dictionary<string, float>> calibratedExpressions = new Dictionary<Expression, Dictionary<string, float>>();
    private List<Dictionary<string, float>> currentSamples = new List<Dictionary<string, float>>();
    private float calibrationStartTime;
    private float lastSampleTime;
    private FaceDetector detector;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        detector = FaceDetector.Instance;
        if (detector == null)
        {
            detector = FindFirstObjectByType<FaceDetector>();
        }
    }

    private void Update()
    {
        if (IsCalibrating)
        {
            CollectSamples();
        }
    }

    public bool StartCalibration(Expression expression)
    {
        if (detector == null)
        {
            detector = FaceDetector.Instance;
        }

        if (detector == null || !detector.IsFaceDetected)
        {
            Debug.LogWarning("[ExpressionCalibrator] No face detected!");
            return false;
        }

        IsCalibrating = true;
        CurrentCalibrating = expression;
        CalibrationProgress = 0f;
        currentSamples.Clear();
        calibrationStartTime = Time.time;
        lastSampleTime = 0f;

        Debug.Log($"[ExpressionCalibrator] Calibrating {expression}...");
        OnCalibrationStarted?.Invoke(expression);
        return true;
    }

    private void CollectSamples()
    {
        if (!CurrentCalibrating.HasValue) return;

        float elapsed = Time.time - calibrationStartTime;
        CalibrationProgress = Mathf.Clamp01(elapsed / calibrationDuration);

        // Sample at interval
        if (Time.time - lastSampleTime >= sampleInterval)
        {
            lastSampleTime = Time.time;

            if (detector != null && detector.IsFaceDetected && detector.Blendshapes != null)
            {
                var sample = new Dictionary<string, float>();
                foreach (var key in KeyBlendshapes)
                {
                    float val = 0f;
                    detector.Blendshapes.TryGetValue(key, out val);
                    sample[key] = val;
                }
                currentSamples.Add(sample);
            }
        }

        // Finish calibration
        if (elapsed >= calibrationDuration)
        {
            FinishCalibration();
        }
    }

    private void FinishCalibration()
    {
        if (!CurrentCalibrating.HasValue || currentSamples.Count == 0)
        {
            IsCalibrating = false;
            CurrentCalibrating = null;
            return;
        }

        // Average all samples
        var averaged = new Dictionary<string, float>();
        foreach (var key in KeyBlendshapes)
        {
            float sum = 0f;
            foreach (var sample in currentSamples)
            {
                float val;
                if (sample.TryGetValue(key, out val)) sum += val;
            }
            averaged[key] = sum / currentSamples.Count;
        }

        Expression expr = CurrentCalibrating.Value;
        calibratedExpressions[expr] = averaged;

        Debug.Log($"[ExpressionCalibrator] {expr} calibrated with {currentSamples.Count} samples");

        IsCalibrating = false;
        OnCalibrationComplete?.Invoke(expr);

        CurrentCalibrating = null;

        if (IsCalibrated)
        {
            OnAllCalibrationComplete?.Invoke();
        }
    }

    /// <summary>
    /// Detect which calibrated expression matches current face.
    /// Returns the expression and confidence (0-1).
    /// </summary>
    public (Expression expression, float confidence) DetectExpression()
    {
        if (!IsCalibrated || detector == null || !detector.IsFaceDetected)
        {
            return (Expression.Neutral, 0f);
        }

        var current = detector.Blendshapes;
        if (current == null || current.Count == 0)
        {
            return (Expression.Neutral, 0f);
        }

        // Calculate distance to each calibrated expression
        float minDistance = float.MaxValue;
        Expression closest = Expression.Neutral;
        float totalDistance = 0f;

        foreach (var kvp in calibratedExpressions)
        {
            float distance = CalculateDistance(current, kvp.Value);
            totalDistance += distance;

            if (distance < minDistance)
            {
                minDistance = distance;
                closest = kvp.Key;
            }
        }

        // Convert to confidence (inverse of relative distance)
        float confidence = 1f - (minDistance / (totalDistance + 0.001f));
        confidence = Mathf.Clamp01(confidence * 2f); // Scale up

        return (closest, confidence);
    }

    /// <summary>
    /// Check if current face matches a specific expression.
    /// </summary>
    public bool IsExpression(Expression target, float threshold = 0.5f)
    {
        var (detected, confidence) = DetectExpression();
        return detected == target && confidence >= threshold;
    }

    private float CalculateDistance(Dictionary<string, float> current, Dictionary<string, float> calibrated)
    {
        float sumSquared = 0f;

        foreach (var key in KeyBlendshapes)
        {
            float currentVal = 0f, calibratedVal = 0f;
            current.TryGetValue(key, out currentVal);
            calibrated.TryGetValue(key, out calibratedVal);
            float diff = currentVal - calibratedVal;
            sumSquared += diff * diff;
        }

        return Mathf.Sqrt(sumSquared);
    }

    public void ResetCalibration()
    {
        calibratedExpressions.Clear();
        IsCalibrating = false;
        CurrentCalibrating = null;
    }
}
