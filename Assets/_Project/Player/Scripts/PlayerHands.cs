using UnityEngine;

[RequireComponent(typeof(PlayerInputReader))]
[RequireComponent(typeof(PlayerMotor))]
public class PlayerHands : MonoBehaviour
{
    [SerializeField] private Transform handPoint;           // iki elin ortasý (tepsi buraya baðlanýr)
    [SerializeField] private float grabRadius = 1.2f;        // ne kadar yakýndakini tutabilir
    [SerializeField] private LayerMask grabMask = ~0;        // þimdilik tüm katmanlar
    [SerializeField] private float grabReachDown = 1.3f;     // elden aþaðý ne kadar uzanabilir

    [Header("Ýki el")]
    [SerializeField] private Transform leftHand;             // HandPoint'in içinde, solda
    [SerializeField] private Transform rightHand;            // HandPoint'in içinde, saðda
    [SerializeField] private float handSpacing = 0.5f;       // iki el arasý mesafe

    [Header("El yüksekliði (sol týk yükselt, sað týk alçalt)")]
    [SerializeField] private float liftSpeed = 0.8f;         // metre/sn
    [SerializeField] private float minHandOffset = -0.4f;    // en fazla ne kadar alçalýr
    [SerializeField] private float maxHandOffset = 0.4f;     // en fazla ne kadar yükselir

    [Header("Kollar (sadece görsel)")]
    [SerializeField] private Transform leftShoulder;
    [SerializeField] private Transform rightShoulder;
    [SerializeField] private Transform leftArm;              // collider'ý olmayan küp
    [SerializeField] private Transform rightArm;
    [SerializeField] private float armThickness = 0.12f;
    [SerializeField] private float restArmLength = 0.6f;     // elin boþken kol boyu

    // --- Ýdil'in kullanacaðý kýsým ---
    public Transform HandPoint => handPoint;
    public Transform LeftHand => leftHand;
    public Transform RightHand => rightHand;
    public bool IsHolding => Held != null;
    public IGrabbable Held { get; private set; }
    public float LiftInput => IsHolding ? input.Current.Lift : 0f; // +1 yükselt, -1 alçalt
    public float HandHeightOffset => handOffset;                   // ellerin normal yüksekliðe göre farký (metre)

    // Tutulan eþya ellerin yükselip alçalmasýný kýsýtlamak isterse ayarlar
    // (ör. alçaltýnca tepsi alttaki küpe girecekse). Ellerin bu karedeki yükseklik deðiþimini alýr,
    // izin verilen deðiþimi döndürür.
    public System.Func<float, float> HandHeightFilter { get; set; }

    public event System.Action<IGrabbable> Grabbed;
    public event System.Action<IGrabbable> Released;
    public event System.Action JumpedWhileHolding;   // tepsi tutarken zýplandý (Ýdil dinleyecek)

    private PlayerInputReader input;
    private PlayerMotor motor;
    private readonly Collider[] hits = new Collider[16];
    private float handOffset;
    private Vector3 handPointRestLocal;

    private void Awake()
    {
        input = GetComponent<PlayerInputReader>();
        motor = GetComponent<PlayerMotor>();
        motor.Jumped += () => { if (IsHolding) JumpedWhileHolding?.Invoke(); };
        handPointRestLocal = handPoint.localPosition;
        PlaceHands();
    }

    // Inspector'da handSpacing deðiþince eller hemen yerine otursun
    private void OnValidate() => PlaceHands();

    private void PlaceHands()
    {
        if (leftHand) leftHand.localPosition = new Vector3(-handSpacing * 0.5f, 0f, 0f);
        if (rightHand) rightHand.localPosition = new Vector3(handSpacing * 0.5f, 0f, 0f);
    }

    private void Update()
    {
        UpdateHandHeight();

        if (input.Current.GrabPressed)
        {
            if (IsHolding) Release();
            else TryGrab();
        }
    }

    private void UpdateHandHeight()
    {
        float target;
        if (IsHolding)
            target = handOffset + input.Current.Lift * liftSpeed * Time.deltaTime;  // týkla yükselt/alçalt
        else
            target = Mathf.MoveTowards(handOffset, 0f, liftSpeed * Time.deltaTime); // býrakýnca normale dön

        target = Mathf.Clamp(target, minHandOffset, maxHandOffset);

        // Tutulan eþyaya sor: bu kadar inip kalkabilir miyim?
        float delta = target - handOffset;
        if (IsHolding && HandHeightFilter != null) delta = HandHeightFilter(delta);

        SetHandOffset(handOffset + delta);
    }

    // Tepsi yukarýdan bir þeyin içine girdiyse elleri kaldýrmak için (karakter yerine eller kalkar)
    public void PushHandsUp(float amount)
    {
        if (amount <= 0f) return;
        SetHandOffset(handOffset + amount);
    }

    private void SetHandOffset(float value)
    {
        handOffset = Mathf.Clamp(value, minHandOffset, maxHandOffset);
        handPoint.localPosition = handPointRestLocal + Vector3.up * handOffset;
    }

    private void LateUpdate()
    {
        // Tutarken kollar iki ele uzanýr, boþken aþaðý sarkar
        if (leftShoulder && leftArm)
            PlaceArm(leftArm, leftShoulder.position,
                IsHolding && leftHand ? leftHand.position : leftShoulder.position - transform.up * restArmLength);

        if (rightShoulder && rightArm)
            PlaceArm(rightArm, rightShoulder.position,
                IsHolding && rightHand ? rightHand.position : rightShoulder.position - transform.up * restArmLength);
    }

    private void PlaceArm(Transform arm, Vector3 from, Vector3 to)
    {
        Vector3 dir = to - from;
        float length = dir.magnitude;
        if (length < 0.001f) return;

        arm.position = from + dir * 0.5f;
        arm.rotation = Quaternion.LookRotation(dir, transform.up);
        arm.localScale = new Vector3(armThickness, armThickness, length);
    }

    private void TryGrab()
    {
        // Elden yere kadar uzanan bir kapsül: hem önündekini hem yerdekini yakalar
        Vector3 top = handPoint.position;
        Vector3 bottom = top + Vector3.down * grabReachDown;
        int count = Physics.OverlapCapsuleNonAlloc(top, bottom, grabRadius, hits, grabMask);

        IGrabbable best = null;
        float bestDistance = float.MaxValue;

        for (int i = 0; i < count; i++)
        {
            var grabbable = hits[i].GetComponentInParent<IGrabbable>();
            if (grabbable == null || !grabbable.CanGrab(this)) continue;

            // Nesnenin ele en yakýn noktasýna bak (ince ve uzun nesnelerde merkez yanýltýr)
            Vector3 closest = hits[i].ClosestPoint(handPoint.position);
            float d = (closest - handPoint.position).sqrMagnitude;
            if (d < bestDistance) { bestDistance = d; best = grabbable; }
        }

        if (best == null) return;

        Held = best;
        motor.IsHolding = true;
        best.OnGrab(this);
        Grabbed?.Invoke(best);
    }

    // Ýdil'in tepsisi düþerse vb. dýþarýdan da çaðrýlabilir
    public void Release()
    {
        if (!IsHolding) return;

        IGrabbable released = Held;
        Held = null;
        motor.IsHolding = false;
        released.OnRelease(this);
        Released?.Invoke(released);
    }

    private void OnDrawGizmosSelected()
    {
        if (handPoint == null) return;
        Gizmos.color = Color.yellow;
        Vector3 top = handPoint.position;
        Vector3 bottom = top + Vector3.down * grabReachDown;
        Gizmos.DrawWireSphere(top, grabRadius);
        Gizmos.DrawWireSphere(bottom, grabRadius);
        Gizmos.DrawLine(top + Vector3.forward * grabRadius, bottom + Vector3.forward * grabRadius);
        Gizmos.DrawLine(top - Vector3.forward * grabRadius, bottom - Vector3.forward * grabRadius);

        // Ýki elin yeri
        Gizmos.color = Color.cyan;
        if (leftHand) Gizmos.DrawWireSphere(leftHand.position, 0.08f);
        if (rightHand) Gizmos.DrawWireSphere(rightHand.position, 0.08f);
    }
}