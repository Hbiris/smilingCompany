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
        var carry = GetComponentInParent<CarrySystem>();
        if (carry != null && carry.IsCarrying)
        {
            carry.Drop();
            return;
        }

        TryInteract();
    }
}


    // void TryInteract()
    // {
    //     if (cam == null) return;

    //     Ray ray = new Ray(cam.transform.position, cam.transform.forward);

    //     if (Physics.Raycast(ray, out RaycastHit hit, interactRange, interactMask))
    //     {
    //         var interactable = hit.collider.GetComponentInParent<IInteractable>();
    //         if (interactable != null)
    //         {
    //             interactable.Interact(this);
    //         }
    //     }
    // }

    void TryInteract()
    {
        Vector3 center = cam.transform.position + cam.transform.forward * 2f;
        float radius = 1.2f;

        Collider[] hits = Physics.OverlapSphere(center, radius, interactMask);

        foreach (var h in hits)
        {
            var interactable = h.GetComponentInParent<IInteractable>();
            if (interactable != null)
            {
                interactable.Interact(this);
                return;
            }
        }
    }

}
