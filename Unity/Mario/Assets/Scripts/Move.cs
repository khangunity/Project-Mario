
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class Move : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;
    public float rotationSpeed = 10f;

    [Header("Camera")]
    public Transform cameraTransform;

    private Rigidbody rb;
    private Vector3 movement;

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        // Tự động lấy Main Camera nếu chưa gán
        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }
    }

    void Update()
    {
        Vector2 input = Vector2.zero;

        // Đọc bàn phím bằng Input System mới
        if (Keyboard.current != null)
        {
            if (Keyboard.current.wKey.isPressed ||
                Keyboard.current.upArrowKey.isPressed)
                input.y += 1;

            if (Keyboard.current.sKey.isPressed ||
                Keyboard.current.downArrowKey.isPressed)
                input.y -= 1;

            if (Keyboard.current.aKey.isPressed ||
                Keyboard.current.leftArrowKey.isPressed)
                input.x -= 1;

            if (Keyboard.current.dKey.isPressed ||
                Keyboard.current.rightArrowKey.isPressed)
                input.x += 1;
        }

        input = Vector2.ClampMagnitude(input, 1f);

        // Di chuyển theo hướng camera
        if (cameraTransform != null)
        {
            Vector3 forward = Vector3.ProjectOnPlane(
                cameraTransform.forward, Vector3.up
            ).normalized;

            Vector3 right = Vector3.ProjectOnPlane(
                cameraTransform.right, Vector3.up
            ).normalized;

            movement = (forward * input.y + right * input.x).normalized;
        }
        else
        {
            movement = new Vector3(input.x, 0, input.y).normalized;
        }
    }

    void FixedUpdate()
    {
        // Di chuyển bằng Rigidbody
        rb.MovePosition(
            rb.position + movement * moveSpeed * Time.fixedDeltaTime
        );

        // Xoay nhân vật theo hướng di chuyển
        if (movement.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(movement);

            rb.MoveRotation(
                Quaternion.Slerp(
                    rb.rotation,
                    targetRotation,
                    rotationSpeed * Time.fixedDeltaTime
                )
            );
        }
    }
}
