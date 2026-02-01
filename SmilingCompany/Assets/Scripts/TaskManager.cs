using UnityEngine;

public class TaskManager : MonoBehaviour
{
    public void MarkTaskComplete(string taskId)
    {
        Debug.Log($"✅ Task complete: {taskId}");
        // 以后：触发事件给 UI ToDoList 打勾
    }
}
