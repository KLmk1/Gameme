using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : NetworkBehaviour
{
    [Header("Настройки движения")]
    [SerializeField] private float moveSpeed = 6f;
    [SerializeField] private float jumpHeight = 1.5f;
    [SerializeField] private float gravity = -19.62f;

    [Header("Камера")]
    [SerializeField] private Camera playerCamera;

    private CharacterController controller;
    private Vector3 velocity;
    private bool isGrounded;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
    }

    public override void OnNetworkSpawn()
    {
        // Включаем камеру ТОЛЬКО для локального владельца этого персонажа
        if (playerCamera != null)
        {
            playerCamera.enabled = IsOwner;
        }
    }

    private void Update()
    {
        // Если это не наш персонаж — пропускаем обработку ввода[cite: 2]
        if (!IsOwner) return;

        HandleMovement();
    }

    private void HandleMovement()
    {
        // Проверка касания земли
        isGrounded = controller.isGrounded;
        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f; // Небольшое прижатие к земле
        }

        // Ввод с клавиатуры (WASD)
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        // Движение относительно направления взгляда
        Vector3 move = transform.right * x + transform.forward * z;
        controller.Move(move * moveSpeed * Time.deltaTime);

        // Прыжок
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        // Гравитация
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }
}