using UnityEngine;

[RequireComponent(typeof(PlayerInputReader))]
[RequireComponent(typeof(PlayerMotor))]
public class PlayerHands : MonoBehaviour
{
    [SerializeField] private Transform handPoint;           // karakterin önündeki el noktasý
    [SerializeField] private float grabRadius = 1.2f;        // ne kadar yakýndakini tutabilir
    [SerializeField] private LayerMask grabMask = ~0;        // þimdilik tüm katmanlar
    [SerializeField] private float grabReachDown = 1.3f;     // elden aþaðý ne kadar uzanabilir

    // --- Ýdil'in kullanacaðý kýsým ---
    public Transform HandPoint => handPoint;
    public bool IsHolding => Held != null;
    public IGrabbable Held { get; private set; }
    public float LiftInput => IsHolding ? input.Current.Lift : 0f; // +1 yükselt, -1 alçalt

    public event System.Action<IGrabbable> Grabbed;
    public event System.Action<IGrabbable> Released;
    public event System.Action JumpedWhileHolding;   // tepsi tutarken zýplandý (Ýdil dinleyecek)

    private PlayerInputReader input;
    private PlayerMotor motor;
    private readonly Collider[] hits = new Collider[16];

    private void Awake()
    {
        input = GetComponent<PlayerInputReader>();
        motor = GetComponent<PlayerMotor>();
        motor.Jumped += () => { if (IsHolding) JumpedWhileHolding?.Invoke(); };
    }

    private void Update()
    {
        if (!input.Current.GrabPressed) return;

        if (IsHolding) Release();
        else TryGrab();
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
    }
}
