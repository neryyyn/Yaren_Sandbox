using UnityEngine;
using UnityEngine.InputSystem;

public class ThirdPersonCamera : MonoBehaviour
{
    [SerializeField] private Transform target;          // takip edilecek oyuncu
    [SerializeField] private float distance = 6f;       // oyuncuya uzaklýk
    [SerializeField] private float height = 1.5f;       // bakýlan nokta, oyuncunun ne kadar üstü
    [SerializeField] private float sensitivity = 0.15f; // fare hassasiyeti
    [SerializeField] private float minPitch = -30f;
    [SerializeField] private float maxPitch = 70f;

    private PlayerInputReader input;
    private float yaw;
    private float pitch = 15f;

    // Multiplayer'da Sude yerel oyuncu doðunca bunu çaðýracak
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        input = target ? target.GetComponent<PlayerInputReader>() : null;
        if (target) yaw = target.eulerAngles.y;
    }

    private void Start()
    {
        SetTarget(target);
        LockCursor(true);
    }

    private void Update()
    {
        // Esc ile imleci serbest býrak, oyuna týklayýnca tekrar kilitle
        if (Keyboard.current.escapeKey.wasPressedThisFrame) LockCursor(false);
        else if (Mouse.current.leftButton.wasPressedThisFrame && Cursor.lockState != CursorLockMode.Locked)
            LockCursor(true);
    }

    private void LateUpdate()
    {
        if (target == null || input == null) return;

        if (Cursor.lockState == CursorLockMode.Locked)
        {
            Vector2 look = input.Current.Look;
            yaw += look.x * sensitivity;
            pitch -= look.y * sensitivity;
            pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
        }

        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 focus = target.position + Vector3.up * height;
        transform.position = focus - rotation * Vector3.forward * distance;
        transform.rotation = rotation;
    }

    private static void LockCursor(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }
}