using UnityEngine;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(PlayerInputReader))]
public class PlayerMotor : MonoBehaviour
{
    public enum RotationMode { FaceCamera, FaceMovement }

    [Header("Dönüþ")]
    [SerializeField] private RotationMode rotationMode = RotationMode.FaceCamera;
    [Header("Hýzlar (birim/sn)")]
    [SerializeField] private float walkSpeed = 20f;
    [SerializeField, Range(0.1f, 1f)] private float carrySpeedMultiplier = 0.5f; // tepsiyle hýz çarpaný
    [SerializeField] private float runSpeed = 30f;
    [SerializeField] private float slowSpeed = 8f;   // GDD'de yok, ekiple netleþecek
    [SerializeField] private float turnSpeed = 720f; // derece/sn

    [Header("Zýplama")]
    [SerializeField] private float jumpHeight = 1.5f;
    [SerializeField] private float gravity = -25f;
    [SerializeField] private float coyoteTime = 0.1f; // kenardan düþerken kýsa süre zýplama izni

    [Header("Referans")]
    [SerializeField] private Transform cameraTransform;

    // Tepsiyi tutunca Hands script'i bunu true yapacak
    public bool IsHolding { get; set; }

    // Tepsi sistemi dönüþü kendisi yönetmek isterse true yapar
    public bool LockRotation { get; set; }
    public bool IsGrounded => controller.isGrounded;
    public float CurrentSpeed { get; private set; }

    
    // Karakter zýpladý (Ýdil'in tepsi sistemi senkron zýplamayý kontrol etmek için dinler)
    public event System.Action Jumped;

    private CharacterController controller;
    private PlayerInputReader input;
    private float verticalVelocity;
    private float lastGroundedTime = -999f;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        input = GetComponent<PlayerInputReader>();
        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;
    }

    private void Update()
    {
        PlayerInputData data = input.Current;

        // 1) Kameraya göre yön
        Vector3 forward = cameraTransform ? cameraTransform.forward : Vector3.forward;
        Vector3 right = cameraTransform ? cameraTransform.right : Vector3.right;
        forward.y = 0f; right.y = 0f;
        forward.Normalize(); right.Normalize();

        Vector3 direction = forward * data.Move.y + right * data.Move.x;
        if (direction.sqrMagnitude > 1f) direction.Normalize();

        // 2) Hýz
        float speed = PickSpeed(data);
        CurrentSpeed = direction.magnitude * speed;

        // 3) Dönüþ: tepsi tutarken dönme, elin boþken seçilen moda göre dön
        bool isMoving = direction.sqrMagnitude > 0.001f;
        if (!IsHolding && !LockRotation && isMoving)
        {
            Vector3 lookDirection = rotationMode == RotationMode.FaceCamera ? forward : direction;
            Quaternion targetRot = Quaternion.LookRotation(lookDirection);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation, targetRot, turnSpeed * Time.deltaTime);
        }

        // 4) Yer çekimi ve zýplama
        if (controller.isGrounded)
        {
            lastGroundedTime = Time.time;
            if (verticalVelocity < 0f) verticalVelocity = -2f; // yere yapýþýk kalsýn
        }

        if (data.JumpPressed && TryJump())
            Jumped?.Invoke();

        verticalVelocity += gravity * Time.deltaTime;

        // 5) Hareket et
        Vector3 velocity = direction * speed + Vector3.up * verticalVelocity;
        controller.Move(velocity * Time.deltaTime);
    }

    private float PickSpeed(PlayerInputData data)
    {
        float speed = walkSpeed;
        if (data.Run) speed = runSpeed;
        else if (data.Slow) speed = slowSpeed;

        if (IsHolding) speed *= carrySpeedMultiplier; // tepsiyle her hýz yavaþlar
        return speed;
    }

    // Ýdil senkron zýplamada bunu çaðýrabilir
    public bool TryJump()
    {
        if (Time.time - lastGroundedTime > coyoteTime) return false;
        verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
        lastGroundedTime = -999f; // havada ikinci kez zýplamasýn
        return true;
    }
}