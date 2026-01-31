using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

/// <summary>
/// UI Panel for face calibration and expression challenge.
/// Flow:
/// 1. Calibrate Neutral -> press F -> 2 seconds recording
/// 2. Calibrate Smile -> press F -> 2 seconds recording
/// 3. Calibrate Sad -> press F -> 2 seconds recording
/// 4. Challenge: random expressions, switch when matched
/// Press Q to quit anytime.
/// </summary>
public class FaceCalibrationPanel : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject panel;
    [SerializeField] private TextMeshProUGUI instructionText;
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private Image progressBar;
    [SerializeField] private RawImage webcamPreview;
    [SerializeField] private TextMeshProUGUI debugText;

    [Header("Challenge Settings")]
    [SerializeField] private int challengeCount = 5; // How many expressions to complete
    [SerializeField] private float matchThreshold = 0.4f;
    [SerializeField] private float matchHoldTime = 0.5f; // Must hold expression for this long

    public bool IsOpen { get; private set; }

    private enum State { Idle, CalibratingNeutral, CalibratingSmile, CalibratingSad, Challenge, Complete }
    private State currentState = State.Idle;

    private ExpressionCalibrator calibrator;
    private FaceDetector detector;

    private ExpressionCalibrator.Expression currentChallenge;
    private int challengesCompleted = 0;
    private float matchStartTime = -1f;
    private float openTime = 0f; // Prevent input on same frame as open
    private bool waitingForCalibrationStart = true; // True = waiting for user to press space
    private bool isAdvancing = false; // Prevent multiple coroutine calls

    private FirstPersonController_NewInput playerController;

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
    }

    private void Update()
    {
        if (!IsOpen) return;

        // Q to quit
        if (Keyboard.current != null && Keyboard.current.qKey.wasPressedThisFrame)
        {
            Close();
            return;
        }

        // Update webcam preview
        if (webcamPreview != null && detector != null && detector.WebcamTexture != null)
        {
            if (webcamPreview.texture != detector.WebcamTexture)
            {
                webcamPreview.texture = detector.WebcamTexture;

                // Fix aspect ratio
                float aspect = (float)detector.WebcamTexture.width / detector.WebcamTexture.height;
                var rect = webcamPreview.rectTransform;
                rect.sizeDelta = new Vector2(rect.sizeDelta.y * aspect, rect.sizeDelta.y);
            }
        }

        // Update debug info
        UpdateDebug();

        // Update progress bar during calibration
        if (calibrator != null && calibrator.IsCalibrating)
        {
            SetProgress(calibrator.CalibrationProgress);
        }

        // State machine
        switch (currentState)
        {
            case State.CalibratingNeutral:
            case State.CalibratingSmile:
            case State.CalibratingSad:
                HandleCalibrationState();
                break;

            case State.Challenge:
                HandleChallengeState();
                break;
        }
    }

    public void Open()
    {
        IsOpen = true;
        if (panel != null) panel.SetActive(true);

        // Lock player movement
        if (playerController != null)
        {
            playerController.enabled = false;
        }
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Reset and start calibration
        if (calibrator != null)
        {
            calibrator.ResetCalibration();
        }
        challengesCompleted = 0;

        // Start with neutral calibration
        currentState = State.CalibratingNeutral;
        SetInstruction("Show your NEUTRAL face\nPress SPACE when ready");
        SetStatus("Waiting...");
        SetProgress(0);
        openTime = Time.time;
        waitingForCalibrationStart = true;
        isAdvancing = false;
    }

    public void Close()
    {
        IsOpen = false;
        if (panel != null) panel.SetActive(false);

        currentState = State.Idle;

        // Unlock player movement
        if (playerController != null)
        {
            playerController.enabled = true;
        }
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void HandleCalibrationState()
    {
        // Check if calibration is in progress
        if (calibrator != null && calibrator.IsCalibrating)
        {
            SetStatus($"Recording... {calibrator.CalibrationProgress:P0}");
            return;
        }

        // Waiting for user to press SPACE to start
        if (waitingForCalibrationStart)
        {
            // Check if SPACE pressed (with delay to prevent accidental trigger)
            if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame && Time.time - openTime > 0.5f)
            {
                if (detector == null || !detector.IsFaceDetected)
                {
                    SetStatus("<color=red>No face detected! Look at camera.</color>");
                    return;
                }

                ExpressionCalibrator.Expression expr = GetCurrentCalibrationExpression();
                if (calibrator.StartCalibration(expr))
                {
                    SetStatus("Recording...");
                    waitingForCalibrationStart = false; // Now recording
                }
            }
            return;
        }

        // Calibration just finished, advance to next state (only once)
        if (!isAdvancing)
        {
            isAdvancing = true;
            StartCoroutine(AdvanceCalibrationState());
        }
    }

    private IEnumerator AdvanceCalibrationState()
    {
        SetStatus("Done!");
        yield return new WaitForSeconds(0.5f);

        switch (currentState)
        {
            case State.CalibratingNeutral:
                currentState = State.CalibratingSmile;
                SetInstruction("Show your SMILE face\nPress SPACE when ready");
                SetStatus("Waiting...");
                SetProgress(0);
                waitingForCalibrationStart = true;
                break;

            case State.CalibratingSmile:
                currentState = State.CalibratingSad;
                SetInstruction("Show your SAD face\nPress SPACE when ready");
                SetStatus("Waiting...");
                SetProgress(0);
                waitingForCalibrationStart = true;
                break;

            case State.CalibratingSad:
                // All calibration done, start challenge
                currentState = State.Challenge;
                StartChallenge();
                break;
        }

        isAdvancing = false;
    }

    private ExpressionCalibrator.Expression GetCurrentCalibrationExpression()
    {
        switch (currentState)
        {
            case State.CalibratingNeutral: return ExpressionCalibrator.Expression.Neutral;
            case State.CalibratingSmile: return ExpressionCalibrator.Expression.Smile;
            case State.CalibratingSad: return ExpressionCalibrator.Expression.Sad;
            default: return ExpressionCalibrator.Expression.Neutral;
        }
    }

    private void StartChallenge()
    {
        challengesCompleted = 0;
        SetProgress(0);
        NextChallenge();
    }

    private void NextChallenge()
    {
        // Pick random expression
        int rand = Random.Range(0, 3);
        currentChallenge = (ExpressionCalibrator.Expression)rand;
        matchStartTime = -1f;

        string exprName = currentChallenge.ToString().ToUpper();
        SetInstruction($"Show: {exprName}");
        SetStatus($"Challenge {challengesCompleted + 1}/{challengeCount}");
    }

    private void HandleChallengeState()
    {
        if (calibrator == null) return;

        var (detected, confidence) = calibrator.DetectExpression();

        // Check if player is showing the correct expression
        if (detected == currentChallenge && confidence >= matchThreshold)
        {
            if (matchStartTime < 0)
            {
                matchStartTime = Time.time;
            }

            float held = Time.time - matchStartTime;
            SetProgress(held / matchHoldTime);

            if (held >= matchHoldTime)
            {
                // Challenge completed!
                challengesCompleted++;

                if (challengesCompleted >= challengeCount)
                {
                    // All done!
                    currentState = State.Complete;
                    SetInstruction("<color=green>COMPLETE!</color>\nPress Q to exit");
                    SetStatus("All expressions matched!");
                    SetProgress(1);
                }
                else
                {
                    NextChallenge();
                }
            }
        }
        else
        {
            matchStartTime = -1f;
            SetProgress(0);
        }

        // Show what's being detected
        SetStatus($"Detected: {detected} ({confidence:P0})");
    }

    private void SetInstruction(string text)
    {
        if (instructionText != null) instructionText.text = text;
    }

    private void SetStatus(string text)
    {
        if (statusText != null) statusText.text = text;
    }

    private void SetProgress(float value)
    {
        if (progressBar != null)
        {
            progressBar.fillAmount = Mathf.Clamp01(value);
        }
    }

    private void UpdateDebug()
    {
        if (debugText == null) return;

        string debug = "";

        // Detector status
        if (detector == null)
        {
            debug = "<color=red>FaceDetector: NULL</color>";
        }
        else
        {
            debug = $"Initialized: {detector.IsInitialized}\n";
            debug += $"Face: {(detector.IsFaceDetected ? "<color=green>YES</color>" : "<color=red>NO</color>")}\n";
            debug += $"Blendshapes: {detector.Blendshapes?.Count ?? 0}\n";

            if (detector.IsFaceDetected && detector.Blendshapes != null)
            {
                float smileL = 0, smileR = 0, frownL = 0, frownR = 0;
                detector.Blendshapes.TryGetValue("mouthSmileLeft", out smileL);
                detector.Blendshapes.TryGetValue("mouthSmileRight", out smileR);
                detector.Blendshapes.TryGetValue("mouthFrownLeft", out frownL);
                detector.Blendshapes.TryGetValue("mouthFrownRight", out frownR);

                debug += $"Smile: {(smileL + smileR) / 2f:F2}\n";
                debug += $"Frown: {(frownL + frownR) / 2f:F2}\n";
            }
        }

        // Calibrator status
        if (calibrator != null && calibrator.IsCalibrated)
        {
            var (expr, conf) = calibrator.DetectExpression();
            debug += $"\nDetected: {expr} ({conf:P0})";
        }

        debugText.text = debug;
    }
}
