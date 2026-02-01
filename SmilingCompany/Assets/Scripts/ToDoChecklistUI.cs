using System.Collections.Generic;
using UnityEngine;

public class ToDoChecklistUI : MonoBehaviour
{
    [System.Serializable]
    public class TaskCheck
    {
        public string taskId;
        public GameObject checkObject;
    }

    public TaskManager taskManager;

    // 这个指向你的 ToDoPanel（整个面板）
    public GameObject todoPanel;

    public List<TaskCheck> checks = new List<TaskCheck>();

    Dictionary<string, GameObject> map;

    void Awake()
    {
        map = new Dictionary<string, GameObject>();
        foreach (var c in checks)
        {
            if (c != null && !string.IsNullOrEmpty(c.taskId) && c.checkObject != null)
            {
                map[c.taskId] = c.checkObject;
                c.checkObject.SetActive(false); // 初始都隐藏
            }
        }
    }

    void OnEnable()
    {
        if (taskManager != null)
            taskManager.OnTaskCompleted += HandleTaskCompleted;
    }

    void OnDisable()
    {
        if (taskManager != null)
            taskManager.OnTaskCompleted -= HandleTaskCompleted;
    }

    void HandleTaskCompleted(string taskId)
    {
        if (todoPanel != null && todoPanel.activeInHierarchy)
            Refresh();
    }


    public void Refresh()
    {
        foreach (var kv in map)
        {
            bool done = taskManager.IsCompleted(kv.Key);
            kv.Value.SetActive(done);
        }
    }
    public void HideAllChecks()
    {
        if (map == null) return;
        foreach (var kv in map)
        {
            if (kv.Value != null) kv.Value.SetActive(false);
        }
    }

}
