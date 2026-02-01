using UnityEngine;
using UnityEngine.InputSystem;

public class CarrySystem : MonoBehaviour
{
    public Transform holdPoint;
    public float dropForwardOffset = 0.6f;
    public float dropRightOffset = 0.6f; // Adjust to shift drop position left/right

    public TaskManager taskManager;

    Rigidbody carriedRb;
    Carryable carried;

    void LateUpdate()
    {
        if (carried != null)
        {
            carried.transform.position = holdPoint.position;
            carried.transform.rotation = holdPoint.rotation;
        }
    }

    public bool IsCarrying => carried != null;

    public void Pickup(Carryable obj)
    {
        if (obj == null || IsCarrying) return;

        carried = obj;
        carriedRb = obj.rb;

        // 让物体“手持跟随”更稳定：临时变成 kinematic
        carriedRb.isKinematic = true;
        carriedRb.useGravity = false;

        obj.OnPickedUp();
        // pickup task complete
        if (taskManager != null && !string.IsNullOrEmpty(obj.pickupTaskId))
        {
            taskManager.MarkTaskComplete(obj.pickupTaskId);
        }
    }

    public void Drop()
    {
        if (carried == null) return;

        // 放到你面前一点点，避免卡进身体
        Vector3 dropPos = holdPoint.position + holdPoint.forward * dropForwardOffset + holdPoint.right * dropRightOffset;

        carriedRb.isKinematic = false;
        carriedRb.useGravity = true;

        carried.transform.position = dropPos;

        carried.OnDropped();

        // 判定是否在合法 DropZone 内
        if (carried.CurrentZone != null)
        {
            bool accepted = carried.CurrentZone.TryAccept(carried);
            if (accepted && taskManager != null)
            {
                taskManager.MarkTaskComplete(carried.deliverTaskId);

            }
        }

        carried = null;
        carriedRb = null;
    }
}
