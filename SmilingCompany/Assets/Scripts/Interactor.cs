using UnityEngine;
using UnityEngine.InputSystem;

public class Interactor : MonoBehaviour
{
    public Camera cam;
    public float interactRange = 5f;
    public LayerMask interactMask = ~0; // 默认全层

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame)
        {
            TryInteract();
        }
    }

    void TryInteract()
    {
        if (cam == null) return;

        Ray ray = new Ray(cam.transform.position, cam.transform.forward);
        float radius = 2f; // 越大越容易拾取
        // if (Physics.SphereCast(ray, radius, out RaycastHit hit, interactRange, interactMask))
        // {
        //     var interactable = hit.collider.GetComponentInParent<IInteractable>();
        //     if (interactable != null)
        //     {
        //         interactable.Interact(this);
        //     }
        // }

        if (Physics.Raycast(ray, out RaycastHit hit, interactRange, interactMask))
        {
            var interactable = hit.collider.GetComponentInParent<IInteractable>();
            if (interactable != null)
            {
                interactable.Interact(this);
            }
        }
    }
}
