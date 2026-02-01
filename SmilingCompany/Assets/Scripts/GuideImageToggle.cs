using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class GuideImageToggle : MonoBehaviour
{
    [Header("Guide Images")]
    [SerializeField] private GameObject rGuideImage;
    [SerializeField] private GameObject hGuideImage;
    [SerializeField] private ToDoChecklistUI todoChecklistUI; 

    void Start()
    {
        // Start hidden
        if (rGuideImage != null) rGuideImage.SetActive(false);
        if (hGuideImage != null) hGuideImage.SetActive(false);
    }

    void Update()
    {
        if (Keyboard.current == null) return;

        // R key toggles R guide
        if (Keyboard.current.rKey.wasPressedThisFrame)
        {
            if (rGuideImage != null)
            {
                bool newState = !rGuideImage.activeSelf;
                rGuideImage.SetActive(newState);

                if (newState && todoChecklistUI != null)
                {
                    todoChecklistUI.Refresh();
                }
            }
        }

        // H key toggles H guide
        if (Keyboard.current.hKey.wasPressedThisFrame)
        {
            if (hGuideImage != null)
                hGuideImage.SetActive(!hGuideImage.activeSelf);
        }

        // Q key closes both
        if (Keyboard.current.qKey.wasPressedThisFrame)
        {
            if (rGuideImage != null) rGuideImage.SetActive(false);
            if (hGuideImage != null) hGuideImage.SetActive(false);
        }
    }
}
