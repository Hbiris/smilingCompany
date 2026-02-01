using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

/// <summary>
/// UI Panel for face calibration and expression testing.
///
/// Calibration Phase (first 3 - saves expressions):
///   1. Neutral -> press F -> save
///   2. Happy -> press F -> save
///   3. Sad -> press F -> save
///
/// Testing Phase (6 tests - must show correct expression):
///   Sequence: Happy, Sad, Neutral, Sad, Neutral, Happy
///   Press F to confirm - wrong = shake + red tint
///
/// On completion: Fires OnCalibrationComplete event (gate disappears)
/// Press Q to quit anytime.
/// </summary>
public class FaceCalibrationPanel : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject panel;
    [SerializeField] private Image expressionImage; // Shows the expression prompt image
    [SerializeField] private RawImage webcamPreview;

    [Header("Expression Images (assign in Inspector)")]
    [SerializeField] private Sprite neutralImage;
    [SerializeField] private Sprite happyImage;
    [SerializeField] private Sprite sadImage;

    [Header("Feedback")]
    [SerializeField] private float shakeDuration = 0.3f;
    [SerializeField] private float shakeIntensity = 10f;
    [SerializeField] private Color errorTintColor = new Color(1f, 0.3f, 0.3f, 1f);
    [SerializeField] private Color successTintColor = new Color(0.3f, 1f, 0.3f, 1f);
    [SerializeField] private float successTintDuration = 0.3f;

    [Header("Detection Settings")]
    [SerializeField] private float matchThreshold = 0.4f;

    [Header("On Complete")]
    [SerializeField] private ExpressionFilterController expressionFilter;

    public bool IsOpen { get; private set; }

    // Event fired when calibration + testing is complete
    public event System.Action OnCalibrationComplete;

    private enum Phase { Idle, Calibrating, Testing, Complete }
    private Phase currentPhase = Phase.Idle;

    // Calibration sequence: Neutral, Happy, Sad
    private readonly ExpressionCalibrator.Expression[] calibrationSequence = new[]
    {
        ExpressionCalibrator.Expression.Neutral,
        ExpressionCalibrator.Expression.Smile, // "Smile" = Happy
        ExpressionCalibrator.Expression.Sad
    };

    // Testing sequence: Happy, Sad, Neutral, Sad, Neutral, Happy
    private readonly ExpressionCalibrator.Expression[] testingSequence = new[]
    {
        ExpressionCalibrator.Expression.Smile,   // Happy
        ExpressionCalibrator.Expression.Sad,
        ExpressionCalibrator.Expression.Neutral,
        ExpressionCalibrator.Expression.Sad,
        ExpressionCalibrator.Expression.Neutral,
        ExpressionCalibrator.Expression.Smile    // Happy
    };

    private int currentIndex = 0;
    private ExpressionCalibrator calibrator;
    private FaceDetector detector;
    private FirstPersonController_NewInput playerController;

    private float openTime = 0f;
    private bool isProcessing = false;
    private Vector3 originalImagePosition;
    private Color originalImageColor;
    private int consecutiveFailures = 0;
    private const int MAX_FAILURES = 3;

    private void Start()
    {
        if (panel != null) panel.SetActive(false);

        calibrator = ExpressionCalibrator.Instance;
        if (calibrator == null)
        {
            calibrator = FindFirstObjectByType<ExpressionCalibrator>();
        }

        detector = FaceDetector.Instance;
        if (detector == null)
        {
            detector = FindFirstObjectByType<FaceDetector>();
        }

        playerController = FindFirstObjectByType<FirstPersonController_NewInput>();

        if (expressionImage != null)
        {
            originalImagePosition = expressionImage.rectTransform.anchoredPosition;
            originalImageColor = expressionImage.color;
        }
    }

    private void Update()
    {
        if (!IsOpen) return;

        if (Keyboard.current != null && Keyboard.current.qKey.wasPressedThisFrame)
        {
            Close();
            return;
        }

        UpdateWebcamPreview();

        if (!isProcessing && Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame && Time.time - openTime > 0.3f)
        {
            HandleFKeyPress();
        }
    }

    private void UpdateWebcamPreview()
    {
        if (webcamPreview != null && detector != null && detector.WebcamTexture != null)
        {
            if (webcamPreview.texture != detector.WebcamTexture)
            {
                webcamPreview.texture = detector.WebcamTexture;

                float aspect = (float)detector.WebcamTexture.width / detector.WebcamTexture.height;
                var rect = webcamPreview.rectTransform;
                rect.sizeDelta = new Vector2(rect.sizeDelta.y * aspect, rect.sizeDelta.y);
            }
        }
    }

    public void Open()
    {
        IsOpen = true;
        if (panel != null) panel.SetActive(true);

        if (playerController != null)
        {
            playerController.enabled = false;
        }
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (calibrator != null)
        {
            calibrator.ResetCalibration();
        }

        currentPhase = Phase.Calibrating;
        currentIndex = 0;
        openTime = Time.time;
        isProcessing = false;
        consecutiveFailures = 0;

        ShowCurrentExpression();
    }

    public void Close()
    {
        IsOpen = false;
        if (panel != null) panel.SetActive(false);

        currentPhase = Phase.Idle;

        if (playerController != null)
        {
            playerController.enabled = true;
        }
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void HandleFKeyPress()
    {
        if (currentPhase == Phase.Complete) return;

        if (detector == null || !detector.IsFaceDetected)
        {
            return;
        }

        if (currentPhase == Phase.Calibrating)
        {
            HandleCalibrationInput();
        }
        else if (currentPhase == Phase.Testing)
        {
            isProcessing = true;
            StartCoroutine(HandleTestingInput());
        }
    }

    private void HandleCalibrationInput()
    {
        if (calibrator == null) return;

        ExpressionCalibrator.Expression currentExpression = calibrationSequence[currentIndex];

        if (calibrator.StartCalibration(currentExpression))
        {
            isProcessing = true;
            StartCoroutine(WaitForCalibration());
        }
    }

    private IEnumerator WaitForCalibration()
    {
        while (calibrator != null && calibrator.IsCalibrating)
        {
            yield return null;
        }

        // Show green tint for successful save
        yield return StartCoroutine(ShowSuccessFeedback());

        currentIndex++;

        if (currentIndex >= calibrationSequence.Length)
        {
            currentPhase = Phase.Testing;
            currentIndex = 0;
        }

        ShowCurrentExpression();
        isProcessing = false;
    }

    private IEnumerator HandleTestingInput()
    {
        if (calibrator == null) yield break;

        // Delay to match calibration timing
        yield return new WaitForSeconds(0.3f);

        ExpressionCalibrator.Expression requiredExpression = testingSequence[currentIndex];
        var (detected, confidence) = calibrator.DetectExpression();

        if (detected == requiredExpression && confidence >= matchThreshold)
        {
            yield return StartCoroutine(HandleCorrectAnswer());
        }
        else
        {
            yield return StartCoroutine(ShowErrorFeedback());
        }
    }

    private IEnumerator HandleCorrectAnswer()
    {
        isProcessing = true;
        consecutiveFailures = 0; // Reset on correct answer

        // Show green tint for correct answer
        yield return StartCoroutine(ShowSuccessFeedback());

        currentIndex++;

        if (currentIndex >= testingSequence.Length)
        {
            currentPhase = Phase.Complete;

            yield return new WaitForSeconds(1f);

            // Activate the expression filter
            if (expressionFilter != null)
            {
                expressionFilter.Activate();
            }

            OnCalibrationComplete?.Invoke();

            Close();
        }
        else
        {
            ShowCurrentExpression();
        }

        isProcessing = false;
    }

    private IEnumerator ShowErrorFeedback()
    {
        isProcessing = true;
        consecutiveFailures++;

        if (expressionImage != null)
        {
            expressionImage.color = errorTintColor;

            float elapsed = 0f;
            while (elapsed < shakeDuration)
            {
                float offsetX = Random.Range(-shakeIntensity, shakeIntensity);
                float offsetY = Random.Range(-shakeIntensity, shakeIntensity);
                expressionImage.rectTransform.anchoredPosition = originalImagePosition + new Vector3(offsetX, offsetY, 0);

                elapsed += Time.deltaTime;
                yield return null;
            }

            expressionImage.rectTransform.anchoredPosition = originalImagePosition;
            expressionImage.color = originalImageColor;
        }

        yield return new WaitForSeconds(0.2f);

        // Check if too many failures - restart from beginning
        if (consecutiveFailures >= MAX_FAILURES)
        {
            RestartFromBeginning();
        }

        isProcessing = false;
    }

    private void RestartFromBeginning()
    {
        consecutiveFailures = 0;

        if (calibrator != null)
        {
            calibrator.ResetCalibration();
        }

        currentPhase = Phase.Calibrating;
        currentIndex = 0;
        ShowCurrentExpression();
    }

    private IEnumerator ShowSuccessFeedback()
    {
        if (expressionImage != null)
        {
            expressionImage.color = successTintColor;
            yield return new WaitForSeconds(successTintDuration);
            expressionImage.color = originalImageColor;
        }
        else
        {
            yield return new WaitForSeconds(successTintDuration);
        }
    }

    private void ShowCurrentExpression()
    {
        if (expressionImage == null) return;

        ExpressionCalibrator.Expression expr;

        if (currentPhase == Phase.Calibrating)
        {
            expr = calibrationSequence[currentIndex];
        }
        else if (currentPhase == Phase.Testing)
        {
            expr = testingSequence[currentIndex];
        }
        else
        {
            return;
        }

        switch (expr)
        {
            case ExpressionCalibrator.Expression.Neutral:
                expressionImage.sprite = neutralImage;
                break;
            case ExpressionCalibrator.Expression.Smile:
                expressionImage.sprite = happyImage;
                break;
            case ExpressionCalibrator.Expression.Sad:
                expressionImage.sprite = sadImage;
                break;
        }
    }
}
