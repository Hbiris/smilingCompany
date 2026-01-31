using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Mediapipe;
using Mediapipe.Tasks.Core;
using Mediapipe.Tasks.Vision.Core;
using Mediapipe.Tasks.Vision.FaceLandmarker;
using Mediapipe.Tasks.Components.Containers;

/// <summary>
/// Core face detection using MediaPipe Face Landmarker.
/// Provides blendshape data for expression detection.
/// </summary>
public class FaceDetector : MonoBehaviour
{
    [Header("Webcam Settings")]
    [SerializeField] private int webcamWidth = 640;
    [SerializeField] private int webcamHeight = 480;
    [SerializeField] private int webcamFPS = 30;
    [SerializeField] private bool mirrorWebcam = true;
    [SerializeField] private bool flipVertical = true;

    [Header("Model Settings")]
    [SerializeField] private string modelPath = "face_landmarker.task";

    [Header("Detection Settings")]
    [SerializeField][Range(0f, 1f)] private float minDetectionConfidence = 0.3f;
    [SerializeField][Range(0f, 1f)] private float minTrackingConfidence = 0.3f;

    public static FaceDetector Instance { get; private set; }

    public bool IsFaceDetected { get; private set; }
    public bool IsInitialized { get; private set; }
    public WebCamTexture WebcamTexture => webcamTexture;
    public Dictionary<string, float> Blendshapes => currentBlendshapes;

    public event Action OnFaceDetected;
    public event Action OnFaceLost;

    private WebCamTexture webcamTexture;
    private Texture2D processingTexture;
    private Dictionary<string, float> currentBlendshapes = new Dictionary<string, float>();
    private FaceLandmarker faceLandmarker;
    private float lastFaceTime;
    private const float FACE_TIMEOUT = 1.5f;
    private long frameCount = 0;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private IEnumerator Start()
    {
        yield return StartCoroutine(InitializeWebcam());
        yield return StartCoroutine(InitializeMediaPipe());

        if (IsInitialized)
        {
            StartCoroutine(DetectionLoop());
        }
    }

    private void OnDestroy()
    {
        Cleanup();
        if (Instance == this) Instance = null;
    }

    private IEnumerator InitializeWebcam()
    {
        yield return Application.RequestUserAuthorization(UserAuthorization.WebCam);

        if (!Application.HasUserAuthorization(UserAuthorization.WebCam))
        {
            Debug.LogError("[FaceDetector] Webcam permission denied!");
            yield break;
        }

        WebCamDevice[] devices = WebCamTexture.devices;
        if (devices.Length == 0)
        {
            Debug.LogError("[FaceDetector] No webcam found!");
            yield break;
        }

        Debug.Log($"[FaceDetector] Using webcam: {devices[0].name}");

        webcamTexture = new WebCamTexture(devices[0].name, webcamWidth, webcamHeight, webcamFPS);
        webcamTexture.Play();

        int timeout = 100;
        while (!webcamTexture.didUpdateThisFrame && timeout > 0)
        {
            timeout--;
            yield return null;
        }

        if (webcamTexture.width <= 16)
        {
            Debug.LogError("[FaceDetector] Webcam failed to initialize!");
            yield break;
        }

        processingTexture = new Texture2D(webcamTexture.width, webcamTexture.height, TextureFormat.RGBA32, false);
        Debug.Log($"[FaceDetector] Webcam ready: {webcamTexture.width}x{webcamTexture.height}");
    }

    private IEnumerator InitializeMediaPipe()
    {
        string fullModelPath = System.IO.Path.Combine(Application.streamingAssetsPath, modelPath);

        if (!System.IO.File.Exists(fullModelPath))
        {
            Debug.LogError($"[FaceDetector] Model not found: {fullModelPath}");
            yield break;
        }

        try
        {
            var baseOptions = new BaseOptions(
                delegateCase: BaseOptions.Delegate.CPU,
                modelAssetPath: fullModelPath
            );

            var options = new FaceLandmarkerOptions(
                baseOptions: baseOptions,
                runningMode: RunningMode.VIDEO,
                numFaces: 1,
                minFaceDetectionConfidence: minDetectionConfidence,
                minFacePresenceConfidence: minDetectionConfidence,
                minTrackingConfidence: minTrackingConfidence,
                outputFaceBlendshapes: true,
                outputFaceTransformationMatrixes: false
            );

            faceLandmarker = FaceLandmarker.CreateFromOptions(options);
            IsInitialized = true;
            Debug.Log("[FaceDetector] MediaPipe initialized!");
        }
        catch (Exception e)
        {
            Debug.LogError($"[FaceDetector] MediaPipe init failed: {e.Message}");
        }

        yield return null;
    }

    private IEnumerator DetectionLoop()
    {
        while (true)
        {
            if (webcamTexture != null && webcamTexture.didUpdateThisFrame)
            {
                ProcessFrame();
            }

            if (IsFaceDetected && Time.time - lastFaceTime > FACE_TIMEOUT)
            {
                IsFaceDetected = false;
                OnFaceLost?.Invoke();
            }

            yield return null;
        }
    }

    private void ProcessFrame()
    {
        if (faceLandmarker == null || processingTexture == null) return;

        try
        {
            Color32[] pixels = webcamTexture.GetPixels32();
            int width = webcamTexture.width;
            int height = webcamTexture.height;

            if (mirrorWebcam) MirrorPixels(pixels, width, height);
            if (flipVertical) FlipVertical(pixels, width, height);

            processingTexture.SetPixels32(pixels);
            processingTexture.Apply();

            using var image = new Mediapipe.Image(processingTexture);
            frameCount++;
            long timestampMs = (long)(Time.time * 1000);

            var result = faceLandmarker.DetectForVideo(image, timestampMs);

            if (result.faceLandmarks != null && result.faceLandmarks.Count > 0)
            {
                bool wasDetected = IsFaceDetected;
                IsFaceDetected = true;
                lastFaceTime = Time.time;

                if (!wasDetected) OnFaceDetected?.Invoke();

                if (result.faceBlendshapes != null && result.faceBlendshapes.Count > 0)
                {
                    ProcessBlendshapes(result.faceBlendshapes[0]);
                }
            }
        }
        catch (Exception e)
        {
            if (frameCount % 60 == 0)
            {
                Debug.LogWarning($"[FaceDetector] Error: {e.Message}");
            }
        }
    }

    private void ProcessBlendshapes(Classifications blendshapes)
    {
        currentBlendshapes.Clear();
        if (blendshapes.categories == null) return;

        foreach (var category in blendshapes.categories)
        {
            if (!string.IsNullOrEmpty(category.categoryName))
            {
                currentBlendshapes[category.categoryName] = category.score;
            }
        }
    }

    private void MirrorPixels(Color32[] pixels, int width, int height)
    {
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width / 2; x++)
            {
                int left = y * width + x;
                int right = y * width + (width - 1 - x);
                (pixels[left], pixels[right]) = (pixels[right], pixels[left]);
            }
        }
    }

    private void FlipVertical(Color32[] pixels, int width, int height)
    {
        for (int y = 0; y < height / 2; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int top = y * width + x;
                int bottom = (height - 1 - y) * width + x;
                (pixels[top], pixels[bottom]) = (pixels[bottom], pixels[top]);
            }
        }
    }

    private void Cleanup()
    {
        if (webcamTexture != null)
        {
            webcamTexture.Stop();
            Destroy(webcamTexture);
        }
        if (processingTexture != null)
        {
            Destroy(processingTexture);
        }
        faceLandmarker?.Close();
    }
}
