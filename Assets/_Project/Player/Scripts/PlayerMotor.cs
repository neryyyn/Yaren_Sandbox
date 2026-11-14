using UnityEngine;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(PlayerInputReader))]
public class PlayerMotor : MonoBehaviour
{
    public enum RotationMode { FaceCamera, FaceMovement }

    [Header("Dönüþ")]
    [SerializeField] private RotationMode rotationMode = RotationMode.FaceCamera;
    [SerializeField] private float turnSpeed = 720f;        // derece/sn
    [SerializeField] private bool turnWhileHolding = false; // tek kiþilik test için: tepsiyle yavaþça dön
    [SerializeField] private float holdingTurnSpeed = 90f;  // derece/sn, tepsiyle dönüþ hýzý

    [Header("Hýzlar (birim/sn)")]
    [SerializeField] private float walkSpeed = 20f;
    [SerializeField] private float runSpeed = 30f;
    [SerializeField] private float slowSpeed = 8f;   // GDD'de yok, ekiple netleþecek
    [SerializeField, Range(0.1f, 1f)] private float carrySpeedMultiplier = 0.5f; // tepsiyle hýz çarpaný

    [Header("Zýplama")]
    [SerializeField] private float jumpHeight = 1.5f;
    [SerializeField] private float gravity = -25f;
    [SerializeField] private float coyoteTime = 0.1f; // kenardan düþerken kýsa süre zýplama izni

    // Tepsiyi tutunca Hands script'i bunu true yapar
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
    }

    private void Update()
    {
        PlayerInputData data = input.Current;

        // 1) Kameraya göre yön (kameranýn açýsý girdiyle geliyor, sahnedeki kameraya bakmýyoruz)
        Quaternion cameraRotation = Quaternion.Euler(0f, data.CameraYaw, 0f);
        Vector3 forward = cameraRotation * Vector3.forward;
        Vector3 right = cameraRotation * Vector3.right;

        Vector3 direction = forward * data.Move.y + right * data.Move.x;
        if (direction.sqrMagnitude > 1f) direction.Normalize();

        // 2) Hýz
        float speed = PickSpeed(data);
        CurrentSpeed = direction.magnitude * speed;

        // 3) Dönüþ
        bool isMoving = direction.sqrMagnitude > 0.001f;
        if (!LockRotation && isMoving)
        {
            if (!IsHolding)
            {
                // Elin boþken: seçilen moda göre normal hýzda dön
                Vector3 lookDirection = rotationMode == RotationMode.FaceCamera ? forward : direction;
                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation, Quaternion.LookRotation(lookDirection), turnSpeed * Time.deltaTime);
            }
            else if (turnWhileHolding)
            {
                // Tepsiyle: kamera yönüne yavaþça dön (sadece test için)
                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation, Quaternion.LookRotation(forward), holdingTurnSpeed * Time.deltaTime);
            }
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

    // Zýplamayý dener; yerdeyse (veya coyote süresindeyse) zýplatýr
    public bool TryJump()
    {
        if (Time.time - lastGroundedTime > coyoteTime) return false;
        verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
        lastGroundedTime = -999f; // havada ikinci kez zýplamasýn
        return true;
    }
}