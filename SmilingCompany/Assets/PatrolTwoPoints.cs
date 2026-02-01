using UnityEngine;

public class PatrolTwoPoints : MonoBehaviour
{
    [Header("Path")]
    public Transform pointA;
    public Transform pointB;

    [Header("Move")]
    public float speed = 1.6f;
    public float arriveDistance = 0.2f;
    public float turnSpeed = 12f;

    [Header("Y Lock (recommended)")]
    public bool lockY = true;        // 防止越走越飞
    public bool useInitialY = true;
    public float fixedY = 0f;

    [Header("Animator (optional)")]
    public Animator animator;
    public string speedParam = "";   // 例如 "Speed" (不填就不管)
    public string attackTrigger = "Attack";
    public bool pauseMoveWhileAttacking = true;

    private Transform _target;
    private bool _isAttacking;

    void Start()
    {
        if (useInitialY) fixedY = transform.position.y;

        if (pointA != null && pointB != null)
            _target = pointB;
    }

    void Update()
    {
        if (pointA == null || pointB == null) return;
        if (pauseMoveWhileAttacking && _isAttacking)
        {
            SetAnimSpeed(0f);
            return;
        }

        Vector3 pos = transform.position;
        if (lockY) pos.y = fixedY;
        transform.position = pos;

        Vector3 to = _target.position - transform.position;
        to.y = 0f;
        float dist = to.magnitude;

        if (dist <= arriveDistance)
        {
            _target = (_target == pointA) ? pointB : pointA;
            SetAnimSpeed(0f);
            return;
        }

        Vector3 dir = to.normalized;
        transform.position += dir * speed * Time.deltaTime;

        // rotate to face moving direction
        if (dir.sqrMagnitude > 0.0001f)
        {
            Quaternion look = Quaternion.LookRotation(dir, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, look, turnSpeed * Time.deltaTime);
        }

        SetAnimSpeed(1f); // 或者 SetAnimSpeed(speed); 看你Animator参数怎么定义
    }

    void SetAnimSpeed(float v)
    {
        if (animator == null) return;
        if (!string.IsNullOrEmpty(speedParam))
            animator.SetFloat(speedParam, v);
    }

    public void TriggerAttack(float resumeAfterSeconds = 0f)
    {
        if (_isAttacking) return;
        _isAttacking = true;

        if (animator != null && !string.IsNullOrEmpty(attackTrigger))
            animator.SetTrigger(attackTrigger);

        CancelInvoke(nameof(ResumeMove));
        if (resumeAfterSeconds > 0f)
            Invoke(nameof(ResumeMove), resumeAfterSeconds);
    }

    public void ResumeMove()
    {
        _isAttacking = false;
    }
}
