using UnityEngine;

public class GameManager : MonoBehaviour
{
    public Transform player;
    public Transform spawnPoint;

    public void Die(string reason)
    {
        Debug.Log($"PLAYER DIED: {reason}");
        if (player == null)
            Debug.LogError("[GameManager] player is NULL!");
        if (spawnPoint == null)
            Debug.LogError("[GameManager] spawnPoint is NULL!");

        if (player != null && spawnPoint != null)
            player.position = spawnPoint.position;
    }
}
