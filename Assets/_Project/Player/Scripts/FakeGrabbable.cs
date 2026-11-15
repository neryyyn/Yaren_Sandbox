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

    // Tepsiyi hareket yönünde "kaydýrarak" dene; çarpacaksa duvar boyunca kaydýr
    private Vector3 BlockIfTrayHits(Vector3 move)
    {
        float distance = move.magnitude;
        if (distance < 0.0001f) return move;

        Vector3 center = transform.TransformPoint(box.center);
        Vector3 halfExtents = Vector3.Scale(box.size * 0.5f, transform.lossyScale);

        if (Physics.BoxCast(center, halfExtents, move / distance, out RaycastHit hit,
                            transform.rotation, distance + 0.02f, blockMask, QueryTriggerInteraction.Ignore))
        {
            Vector3 slideAlong = Vector3.ProjectOnPlane(move, hit.normal);
            slideAlong.y = 0f;
            return slideAlong;
        }
        return move;
    }

    // Karakter bu açýya dönerse tepsi bir þeyin içine girecek mi? Girecekse dönme.
    private Quaternion BlockIfTurnHits(Quaternion nextRotation)
    {
        Transform player = holderMotor.transform;
        Quaternion currentRotation = player.rotation;

        // Tepsinin karaktere göre yerini ve açýsýný bul
        Quaternion inverse = Quaternion.Inverse(currentRotation);
        Vector3 localPosition = inverse * (transform.position - player.position);
        Quaternion localRotation = inverse * transform.rotation;

        Vector3 halfExtents = Vector3.Scale(box.size * 0.5f, transform.lossyScale);
        Vector3 boxOffset = Vector3.Scale(box.center, transform.lossyScale);

        // Dönüþ sonrasý tepsi nerede olacak?
        Quaternion nextTrayRotation = nextRotation * localRotation;
        Vector3 nextCenter = player.position + nextRotation * localPosition + nextTrayRotation * boxOffset;
        bool hitsAfterTurn = Physics.CheckBox(nextCenter, halfExtents, nextTrayRotation, blockMask, QueryTriggerInteraction.Ignore);

        // Tepsi þu an zaten bir þeyin içindeyse dönmeye izin ver ki oradan çýkabilsin
        Vector3 currentCenter = transform.TransformPoint(box.center);
        bool hitsNow = Physics.CheckBox(currentCenter, halfExtents, transform.rotation, blockMask, QueryTriggerInteraction.Ignore);

        return (hitsAfterTurn && !hitsNow) ? currentRotation : nextRotation;
    }
}