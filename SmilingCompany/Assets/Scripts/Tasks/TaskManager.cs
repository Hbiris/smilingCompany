using System;
using System.Collections.Generic;
using UnityEngine;

public class TaskManager : MonoBehaviour
{
    public event Action<string> OnTaskCompleted;

    private HashSet<string> completed = new HashSet<string>();

    public bool IsCompleted(string taskId) => completed.Contains(taskId);

    public void MarkTaskComplete(string taskId)
    {
        if (string.IsNullOrEmpty(taskId)) return;
        if (!completed.Add(taskId)) return; // 已完成则不重复

        Debug.Log($"✅ Task complete: {taskId}");
        OnTaskCompleted?.Invoke(taskId);
    }
}
