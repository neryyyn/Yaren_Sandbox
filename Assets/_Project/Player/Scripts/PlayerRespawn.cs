using UnityEngine;

[RequireComponent(typeof(PlayerMotor))]
public class PlayerRespawn : MonoBehaviour, IRespawnable
{
    [SerializeField] private Transform spawnPoint; // boþsa oyunun baþladýðý yer kullanýlýr

    private PlayerMotor motor;
    private PlayerHands hands;
    private Vector3 startPosition;
    private Quaternion startRotation;

    // Aðda Sude oyuncuyu doðurunca hangi noktaya ait olduðunu buradan verir
    public void SetSpawnPoint(Transform point) => spawnPoint = point;

    private void Awake()
    {
        motor = GetComponent<PlayerMotor>();
        hands = GetComponent<PlayerHands>();
        startPosition = transform.position;
        startRotation = transform.rotation;
    }

    public void Respawn()
    {
        if (hands) hands.Release(); // elindekini býrak (gerçek oyunda pasta hasarýný Melike yönetir)

        if (spawnPoint) motor.Teleport(spawnPoint.position, spawnPoint.rotation);
        else motor.Teleport(startPosition, startRotation);
    }
}
