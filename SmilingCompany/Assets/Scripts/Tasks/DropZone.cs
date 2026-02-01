using UnityEngine;

public class DropZone : MonoBehaviour
{
    public string acceptsTaskId = "deliver_document";

    void OnTriggerEnter(Collider other)
    {
        var c = other.GetComponentInParent<Carryable>();
        if (c != null) c.SetZone(this);
    }

    void OnTriggerExit(Collider other)
    {
        var c = other.GetComponentInParent<Carryable>();
        if (c != null && c.CurrentZone == this) c.SetZone(null);
    }

    public bool TryAccept(Carryable c)
    {
        if (c == null) return false;

        // 只接受指定任务物品
        if (c.deliverTaskId != acceptsTaskId) return false;

        // 这里可以做“吸附到桌面指定点”
        // c.transform.position = snapPoint.position;

        return true;
    }
}
