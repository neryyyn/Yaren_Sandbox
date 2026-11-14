using UnityEngine;

// SADECE TEST: Ýdil'in tepsisi gelene kadar yer tutucu.
[RequireComponent(typeof(Rigidbody))]
public class FakeGrabbable : MonoBehaviour, IGrabbable
{
    [SerializeField] private float gap = 0.05f; // elle tepsinin yakýn kenarý arasýndaki boþluk

    private PlayerHands holder;
    private Rigidbody body;
    private Collider col;

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        body.interpolation = RigidbodyInterpolation.Interpolate;
    }

    public bool CanGrab(PlayerHands hands) => holder == null;

    public void OnGrab(PlayerHands hands)
    {
        holder = hands;
        body.isKinematic = true;
        body.interpolation = RigidbodyInterpolation.None; // eldeyken karakteri birebir takip etsin
        col.enabled = false;

        transform.SetParent(hands.HandPoint, false);
        transform.localRotation = Quaternion.identity;

        // Tepsinin gerçek derinliðinin yarýsý kadar ileri: yakýn kenarý ele gelir
        float halfDepth = GetComponent<MeshFilter>().sharedMesh.bounds.extents.z * transform.localScale.z;
        transform.localPosition = new Vector3(0f, 0f, halfDepth + gap);
    }

    public void OnRelease(PlayerHands hands)
    {
        transform.SetParent(null, true);
        col.enabled = true;
        body.isKinematic = false;
        body.interpolation = RigidbodyInterpolation.Interpolate; // yere düþerken yumuþak olsun
        body.linearVelocity = Vector3.zero;
        body.angularVelocity = Vector3.zero;
        holder = null;
    }
}