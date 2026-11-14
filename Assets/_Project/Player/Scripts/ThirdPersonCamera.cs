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

    [Header("Çarpýþma")]
    [SerializeField] private LayerMask collisionMask = ~0;  // kameranýn çarpacaðý katmanlar
    [SerializeField] private float collisionRadius = 0.25f; // kameranýn "kalýnlýðý"
    [SerializeField] private float minDistance = 0.8f;      // oyuncuya en fazla bu kadar yaklaþsýn

    private PlayerInputReader input;
    private float yaw;
    private float pitch = 15f;

    // Multiplayer'da Sude yerel oyuncu doðunca bunu çaðýracak
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        input = target ? target.GetComponent<PlayerInputReader>() : null;
        if (input != null) input.SetCamera(transform);
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
        Vector3 backward = rotation * Vector3.back;

        // Oyuncu ile kamera arasýnda engel varsa kamerayý engelin önüne çek
        float currentDistance = distance;
        if (Physics.SphereCast(focus, collisionRadius, backward, out RaycastHit hit,
                               distance, collisionMask, QueryTriggerInteraction.Ignore))
        {
            currentDistance = Mathf.Max(hit.distance, minDistance);
        }

        transform.position = focus + backward * currentDistance;
        transform.rotation = rotation;
    }

    private static void LockCursor(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }
}