using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class FirstPersonController_NewInput : MonoBehaviour
{
    public Transform cameraPivot;

    public float moveSpeed = 4.5f;
    public float sprintSpeed = 7f;
    public float gravity = -20f;
    public float jumpHeight = 1.2f;

    public float mouseSensitivity = 0.12f; // 新系统下建议小一点
    public float pitchMin = -80f, pitchMax = 80f;

    CharacterController cc;
    float verticalVelocity;
    float pitch;

    void Awake()
    {
        cc = GetComponent<CharacterController>();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        Look();
        Move();
    }

    void Look()
    {
        Vector2 delta = Mouse.current.delta.ReadValue();
        float mx = delta.x * mouseSensitivity;
        float my = delta.y * mouseSensitivity;

        transform.Rotate(Vector3.up * mx);

        pitch -= my;
        pitch = Mathf.Clamp(pitch, pitchMin, pitchMax);
        if (cameraPivot) cameraPivot.localRotation = Quaternion.Euler(pitch, 0, 0);
    }

    void Move()
    {
        // WASD
        Vector2 wasd = Vector2.zero;
        if (Keyboard.current.wKey.isPressed) wasd.y += 1;
        if (Keyboard.current.sKey.isPressed) wasd.y -= 1;
        if (Keyboard.current.dKey.isPressed) wasd.x += 1;
        if (Keyboard.current.aKey.isPressed) wasd.x -= 1;

        Vector3 move = (transform.right * wasd.x + transform.forward * wasd.y);
        if (move.sqrMagnitude > 1e-4f) move.Normalize();

        bool sprint = Keyboard.current.leftShiftKey.isPressed;
        float speed = sprint ? sprintSpeed : moveSpeed;

        if (cc.isGrounded && verticalVelocity < 0f) verticalVelocity = -2f;

        if (cc.isGrounded && Keyboard.current.spaceKey.wasPressedThisFrame)
            verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);

        verticalVelocity += gravity * Time.deltaTime;

        Vector3 vel = move * speed;
        vel.y = verticalVelocity;

        cc.Move(vel * Time.deltaTime);
    }
}
