using System;
using System.Collections;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public Transform player;
    public Transform spawnPoint;

    [Header("Respawn")]
    public float respawnDelay = 0f;
    public float invulnSeconds = 0.5f;

    public bool IsInvulnerable { get; private set; }
    public bool IsRespawning { get; private set; }

    public event Action OnPlayerDied;
    public event Action OnPlayerRespawned;

    CharacterController cc;
    Rigidbody rb;

    void Awake()
    {
        if (player != null)
        {
            cc = player.GetComponent<CharacterController>();
            rb = player.GetComponent<Rigidbody>();
        }
    }

    public void Die(string reason)
    {
        if (IsRespawning) return; // ✅ 防重复
        IsRespawning = true;

        Debug.Log($"PLAYER DIED: {reason}");
        OnPlayerDied?.Invoke();

        StartCoroutine(RespawnRoutine());
    }

    IEnumerator RespawnRoutine()
    {
        if (respawnDelay > 0f)
            yield return new WaitForSeconds(respawnDelay);

        // ✅ 处理 CharacterController / Rigidbody 的瞬移坑
        if (cc != null) cc.enabled = false;

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.position = spawnPoint.position;
        }
        else if (player != null)
        {
            player.position = spawnPoint.position;
        }

        // ✅ 让物理/触发器立刻更新到新位置（关键）
        Physics.SyncTransforms();

        if (cc != null) cc.enabled = true;

        // ✅ 无敌帧：避免复活瞬间又被判死
        IsInvulnerable = true;
        OnPlayerRespawned?.Invoke();

        yield return new WaitForSeconds(invulnSeconds);
        IsInvulnerable = false;

        IsRespawning = false;
    }
}
