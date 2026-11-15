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

    [Header("Dik yüzey")]
    [SerializeField] private float slideSpeed = 6f;   // Slope Limit'ten dik yüzeyde aþaðý kayma hýzý

    // Tepsiyi tutunca Hands script'i bunu true yapar
    public bool IsHolding { get; set; }

    // Tepsi sistemi dönüþü kendisi yönetmek isterse true yapar
    public bool LockRotation { get; set; }

    // Tepsi sistemi hareketi kýsýtlamak isterse ayarlar (ör. tepsi duvara deðiyor, iki oyuncuyu tepsi boyunda tut)
    // Bu karede yapýlacak yatay hareketi alýr, izin verilen hareketi döndürür.
    public System.Func<Vector3, Vector3> MoveFilter { get; set; }

    // Tepsi sistemi dönüþü kýsýtlamak isterse ayarlar (ör. dönünce tepsi duvara girecekse)
    // Karakterin döneceði açýyý alýr, izin verilen açýyý döndürür.
    public System.Func<Quaternion, Quaternion> RotationFilter { get; set; }

    public bool IsGrounded { get; private set; }
    public float CurrentSpeed { get; private set; }

    // Karakter zýpladý (Ýdil'in tepsi sistemi senkron zýplamayý kontrol etmek için dinler)
    public event System.Action Jumped;

    private CharacterController controller;
    private PlayerInputReader input;
    private float verticalVelocity;
    private float lastGroundedTime = -999f;
    private bool onSteepSlope;
    private int groundMask;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        input = GetComponent<PlayerInputReader>();
        groundMask = ~LayerMask.GetMask("Player"); // kendi gövdemize çarpmasýn
    }

    private void Update()
    {
        PlayerInputData data = input.Current;

        // 0) Altýmýzdaki yüzey çok dik mi?
        onSteepSlope = CheckSteepSlope(out Vector3 slopeNormal);
        IsGrounded = controller.isGrounded && !onSteepSlope;

        // 1) Kameraya göre yön (kameranýn açýsý girdiyle geliyor, sahnedeki kameraya bakmýyoruz)
        Quaternion cameraRotation = Quaternion.Euler(0f, data.CameraYaw, 0f);
        Vector3 forward = cameraRotation * Vector3.forward;
        Vector3 right = cameraRotation * Vector3.right;

        Vector3 direction = forward * data.Move.y + right * data.Move.x;
        if (direction.sqrMagnitude > 1f) direction.Normalize();

        // Dik yüzeydeysek yokuþ yukarý giden kýsmý iptal et ve aþaðý kaydýr
        Vector3 slide = Vector3.zero;
        if (onSteepSlope)
        {
            Vector3 downhill = Vector3.ProjectOnPlane(Vector3.down, slopeNormal).normalized;
            Vector3 uphillFlat = -new Vector3(downhill.x, 0f, downhill.z).normalized;
            float uphillAmount = Vector3.Dot(direction, uphillFlat);
            if (uphillAmount > 0f) direction -= uphillFlat * uphillAmount;
            slide = downhill * slideSpeed;
        }

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
                // Tepsiyle: kamera yönüne yavaþça dön (sadece test için), önce tepsi sistemine sor
                Quaternion next = Quaternion.RotateTowards(
                    transform.rotation, Quaternion.LookRotation(forward), holdingTurnSpeed * Time.deltaTime);
                if (RotationFilter != null) next = RotationFilter(next);
                transform.rotation = next;
            }
        }

        // 4) Yer çekimi ve zýplama
        if (IsGrounded)
        {
            lastGroundedTime = Time.time;
            if (verticalVelocity < 0f) verticalVelocity = -2f; // yere yapýþýk kalsýn
        }

        if (data.JumpPressed && TryJump())
            Jumped?.Invoke();

        verticalVelocity += gravity * Time.deltaTime;
        verticalVelocity = Mathf.Max(verticalVelocity, -50f); // sonsuz hýzlanmasýn

        // 5) Hareket et (önce yatay hareketi tepsi sistemine sor)
        Vector3 horizontalMove = direction * speed * Time.deltaTime;
        if (MoveFilter != null) horizontalMove = MoveFilter(horizontalMove);

        Vector3 move = horizontalMove + (slide + Vector3.up * verticalVelocity) * Time.deltaTime;
        controller.Move(move);
    }

    // Karakterin tam altýna ýþýn atar; yüzey Slope Limit'ten dikse true döner
    private bool CheckSteepSlope(out Vector3 normal)
    {
        normal = Vector3.up;
        Vector3 origin = transform.position + controller.center;
        float length = controller.height * 0.5f + 0.6f;

        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, length, groundMask, QueryTriggerInteraction.Ignore))
        {
            normal = hit.normal;
            return Vector3.Angle(hit.normal, Vector3.up) > controller.slopeLimit + 1f;
        }
        return false;
    }

    private float PickSpeed(PlayerInputData data)
    {
        float speed = walkSpeed;
        if (data.Run) speed = runSpeed;
        else if (data.Slow) speed = slowSpeed;

        if (IsHolding) speed *= carrySpeedMultiplier; // tepsiyle her hýz yavaþlar
        return speed;
    }

    // Zýplamayý dener; yerdeyse (veya coyote süresindeyse) zýplatýr. Dik yüzeyde zýplanmaz.
    public bool TryJump()
    {
        if (onSteepSlope) return false;
        if (Time.time - lastGroundedTime > coyoteTime) return false;
        verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
        lastGroundedTime = -999f; // havada ikinci kez zýplamasýn
        return true;
    }

    // Karakteri bir noktaya ýþýnlar (respawn, aðda host düzeltmesi vb.)
    public void Teleport(Vector3 position, Quaternion rotation)
    {
        controller.enabled = false;
        transform.SetPositionAndRotation(position, rotation);
        controller.enabled = true;
        verticalVelocity = 0f;
    }
}