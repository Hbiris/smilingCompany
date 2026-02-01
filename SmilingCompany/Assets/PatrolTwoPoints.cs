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

    [Header("Animator (optional)")]
    public Animator animator;
    public string attackTrigger = "Attack"; // 你之前用的Attack trigger
    public bool pauseMoveWhileAttacking = true;

    private Transform _target;
    private bool _isAttacking;

    void Start()
    {
        if (pointA == null || pointB == null) return;
        _target = pointB;
    }

    void Update()
    {
        if (pointA == null || pointB == null) return;
        if (pauseMoveWhileAttacking && _isAttacking) return;

        // move
        Vector3 to = _target.position - transform.position;
        to.y = 0f; // 保持地面高度
        float dist = to.magnitude;

        if (dist <= arriveDistance)
        {
            // switch target -> turn around effect
            _target = (_target == pointA) ? pointB : pointA;
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
    }

    // 你要在别的脚本里调用：patrol.TriggerAttack();
    public void TriggerAttack(float resumeAfterSeconds = 0f)
    {
        if (_isAttacking) return;
        _isAttacking = true;

        if (animator != null && !string.IsNullOrEmpty(attackTrigger))
            animator.SetTrigger(attackTrigger);

        if (resumeAfterSeconds > 0f)
            Invoke(nameof(ResumeMove), resumeAfterSeconds);
    }

    public void ResumeMove()
    {
        _isAttacking = false;
    }
}
