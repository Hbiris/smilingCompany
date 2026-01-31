using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Handles player interaction with the Gate.
/// Press F near gate to open calibration panel.
/// </summary>
public class GateInteraction : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float interactionDistance = 3f;
    [SerializeField] private GameObject interactionPrompt; // "Press F to interact" UI

    [Header("References")]
    [SerializeField] private FaceCalibrationPanel calibrationPanel;

    private Transform player;
    private bool isPlayerNear = false;

    private void Start()
    {
        // Find player
        var playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
        }
        else
        {
            // Try to find by FirstPersonController_NewInput
            var fpc = FindFirstObjectByType<FirstPersonController_NewInput>();
            if (fpc != null) player = fpc.transform;
        }

        if (interactionPrompt != null)
        {
            interactionPrompt.SetActive(false);
        }
    }

    private void Update()
    {
        if (player == null) return;

        // Check distance
        float distance = Vector3.Distance(transform.position, player.position);
        isPlayerNear = distance <= interactionDistance;

        // Show/hide prompt
        if (interactionPrompt != null)
        {
            bool showPrompt = isPlayerNear && (calibrationPanel == null || !calibrationPanel.IsOpen);
            interactionPrompt.SetActive(showPrompt);
        }

        // Handle F key press
        if (isPlayerNear && Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame)
        {
            if (calibrationPanel != null && !calibrationPanel.IsOpen)
            {
                calibrationPanel.Open();
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactionDistance);
    }
}
