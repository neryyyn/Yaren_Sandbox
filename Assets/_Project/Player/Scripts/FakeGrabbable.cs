using UnityEngine;

// SADECE TEST: Ýdil'in tepsisi gelene kadar yer tutucu.
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(BoxCollider))]
public class FakeGrabbable : MonoBehaviour, IGrabbable, IRespawnable
{
    [SerializeField] private float gap = 0.05f; // elle tepsinin yakýn kenarý arasýndaki boþluk

    private PlayerHands holder;
    private PlayerMotor holderMotor;
    private Rigidbody body;
    private BoxCollider box;
    private Vector3 startPosition;
    private Quaternion startRotation;
    private int blockMask;
    private readonly Collider[] overlaps = new Collider[16];

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
        box = GetComponent<BoxCollider>();
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        body.interpolation = RigidbodyInterpolation.Interpolate;
        startPosition = transform.position;
        startRotation = transform.rotation;
        blockMask = ~LayerMask.GetMask("Player"); // tutan oyuncuya takýlmasýn
    }

    public bool CanGrab(PlayerHands hands) => holder == null;

    public void OnGrab(PlayerHands hands)
    {
        holder = hands;
        body.isKinematic = true;
        body.interpolation = RigidbodyInterpolation.None; // eldeyken karakteri birebir takip etsin
        box.enabled = false;                              // karakter kendi tepsisine takýlmasýn

        transform.SetParent(hands.HandPoint, false);
        transform.localRotation = Quaternion.identity;

        // Tepsinin gerçek derinliðinin yarýsý kadar ileri: yakýn kenarý ele gelir
        float halfDepth = GetComponent<MeshFilter>().sharedMesh.bounds.extents.z * transform.localScale.z;
        transform.localPosition = new Vector3(0f, 0f, halfDepth + gap);

        // Karakter hareket etmeden ve dönmeden önce tepsinin bir yere çarpýp çarpmayacaðýný sorsun
        holderMotor = hands.GetComponent<PlayerMotor>();
        if (holderMotor)
        {
            holderMotor.MoveFilter = BlockIfTrayHits;
            holderMotor.RotationFilter = BlockIfTurnHits;
        }
    }

    public void OnRelease(PlayerHands hands)
    {
        if (holderMotor)
        {
            holderMotor.MoveFilter = null;
            holderMotor.RotationFilter = null;
        }
        holderMotor = null;

        transform.SetParent(null, true);
        box.enabled = true;
        body.isKinematic = false;
        body.interpolation = RigidbodyInterpolation.Interpolate; // yere düþerken yumuþak olsun
        body.linearVelocity = Vector3.zero;
        body.angularVelocity = Vector3.zero;
        holder = null;
    }

    public void Respawn()
    {
        if (holder) holder.Release();
        body.linearVelocity = Vector3.zero;
        body.angularVelocity = Vector3.zero;
        transform.SetPositionAndRotation(startPosition, startRotation);
    }

    // --- Yardýmcýlar: tepsinin kutusu belli bir yerde/açýda bir þeye deðiyor mu? ---
    private Vector3 HalfExtents => Vector3.Scale(box.size * 0.5f, transform.lossyScale);

    private Vector3 BoxCenterAt(Vector3 trayPosition, Quaternion trayRotation)
        => trayPosition + trayRotation * Vector3.Scale(box.center, transform.lossyScale);

    private bool IsBlocked(Vector3 trayPosition, Quaternion trayRotation)
        => Physics.CheckBox(BoxCenterAt(trayPosition, trayRotation), HalfExtents, trayRotation,
                            blockMask, QueryTriggerInteraction.Ignore);

    // 1) Gitmeden önce bak: varýlacak yerde bir þey varsa gitme, X veya Z boyunca kaymayý dene
    private Vector3 BlockIfTrayHits(Vector3 move)
    {
        if (move.sqrMagnitude < 0.00000001f) return move;

        Vector3 position = transform.position;
        Quaternion rotation = transform.rotation;

        if (!IsBlocked(position + move, rotation)) return move;

        Vector3 onlyX = new Vector3(move.x, 0f, 0f);
        if (onlyX.sqrMagnitude > 0.00000001f && !IsBlocked(position + onlyX, rotation)) return onlyX;

        Vector3 onlyZ = new Vector3(0f, 0f, move.z);
        if (onlyZ.sqrMagnitude > 0.00000001f && !IsBlocked(position + onlyZ, rotation)) return onlyZ;

        return Vector3.zero;
    }

    // Dönüþ için de aynýsý: dönünce tepsi bir þeye girecekse dönme
    private Quaternion BlockIfTurnHits(Quaternion nextRotation)
    {
        Transform player = holderMotor.transform;
        Quaternion inverse = Quaternion.Inverse(player.rotation);
        Vector3 localPosition = inverse * (transform.position - player.position);
        Quaternion localRotation = inverse * transform.rotation;

        Quaternion nextTrayRotation = nextRotation * localRotation;
        Vector3 nextTrayPosition = player.position + nextRotation * localPosition;

        return IsBlocked(nextTrayPosition, nextTrayRotation) ? player.rotation : nextRotation;
    }

    // 2) Girdiyse dýþarý it: her karenin sonunda tepsi bir þeyin içindeyse karakteri geri it
    private void LateUpdate()
    {
        if (holderMotor == null) return;

        Vector3 center = BoxCenterAt(transform.position, transform.rotation);
        int count = Physics.OverlapBoxNonAlloc(center, HalfExtents, overlaps, transform.rotation,
                                               blockMask, QueryTriggerInteraction.Ignore);
        if (count == 0) return;

        box.enabled = true; // hesap için kýsa süreliðine aç
        Vector3 push = Vector3.zero;
        for (int i = 0; i < count; i++)
        {
            Collider other = overlaps[i];
            if (Physics.ComputePenetration(box, transform.position, transform.rotation,
                                           other, other.transform.position, other.transform.rotation,
                                           out Vector3 direction, out float distance))
            {
                push += direction * distance;
            }
        }
        box.enabled = false; // karakter kendi tepsisine takýlmasýn diye tekrar kapat

        push.y = 0f; // sadece yatayda it (karakteri havaya kaldýrmasýn)
        if (push.sqrMagnitude > 0.00000001f)
            holderMotor.Nudge(push * 1.05f); // biraz fazlasýyla it ki tam dýþarý çýksýn
    }
}