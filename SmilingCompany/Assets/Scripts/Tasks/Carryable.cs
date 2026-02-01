using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Carryable : MonoBehaviour, IInteractable
{
    public string pickupTaskId = "pickup_document";
    public string deliverTaskId = "deliver_document";

    [HideInInspector] public Rigidbody rb;
    public DropZone CurrentZone { get; private set; }

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Start()
    {
        // Ignore collision with player to prevent pushing
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            Collider playerCollider = player.GetComponent<Collider>();
            Collider myCollider = GetComponent<Collider>();
            if (playerCollider != null && myCollider != null)
            {
                Physics.IgnoreCollision(myCollider, playerCollider, true);
            }
        }
    }

    public void Interact(Interactor interactor)
    {
        var carry = interactor.GetComponentInParent<CarrySystem>();
        if (carry == null) return;

        if (!carry.IsCarrying)
            carry.Pickup(this);
        else carry.Drop();
    }

    public void SetZone(DropZone zone) => CurrentZone = zone;

    public void OnPickedUp()
    {
        AudioManager.Instance?.PlayPickupSound();
    }
    public void OnDropped() { /* 可加音效 */ }
}
