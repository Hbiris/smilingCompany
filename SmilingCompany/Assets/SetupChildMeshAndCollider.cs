using UnityEngine;

public class SetupChildMeshAndCollider : MonoBehaviour
{
    [Header("Options")]
    public bool addMeshCollider = true;
    public bool convexCollider = false;
    public bool isTrigger = false;

    [ContextMenu("Setup Mesh & Collider For Children")]
    public void Setup()
    {
        MeshFilter[] meshFilters = GetComponentsInChildren<MeshFilter>(true);

        foreach (MeshFilter mf in meshFilters)
        {
            if (mf.sharedMesh == null) continue;

            GameObject go = mf.gameObject;
            if (go.GetComponent<MeshRenderer>() == null)
            {
                go.AddComponent<MeshRenderer>();
            }

            // Mesh Collider
            if (addMeshCollider)
            {
                MeshCollider mc = go.GetComponent<MeshCollider>();
                if (mc == null)
                    mc = go.AddComponent<MeshCollider>();

                mc.sharedMesh = mf.sharedMesh;
                mc.convex = convexCollider;
                mc.isTrigger = isTrigger;
            }
        }

        Debug.Log($"[Setup] Processed {meshFilters.Length} mesh objects");
    }
}
