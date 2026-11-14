using UnityEngine;
using UnityEngine.InputSystem;

// Bir karedeki tüm tuþ bilgisi. Motor sadece bunu okur.
[System.Serializable]
public struct PlayerInputData
{
    public Vector2 Move;      // WASD
    public Vector2 Look;      // fare hareketi
    public bool Run;          // Shift basýlý mý
    public bool Slow;         // Ctrl basýlý mý
    public bool JumpPressed;  // Space bu karede basýldý mý
    public bool GrabPressed;  // E bu karede basýldý mý
    public float Lift;        // +1 sol týk (yükselt), -1 sað týk (alçalt), 0 hiçbiri
}

[DefaultExecutionOrder(-100)] // diðer script'lerden önce çalýþsýn
public class PlayerInputReader : MonoBehaviour
{
    [SerializeField] private PlayerInputData current; // Inspector'da canlý izlemek için
    public PlayerInputData Current => current;

    private InputAction move, look, run, slow, jump, grab, raise, lower;

    private void Awake()
    {
        move = new InputAction("Move");
        move.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/w")
            .With("Down", "<Keyboard>/s")
            .With("Left", "<Keyboard>/a")
            .With("Right", "<Keyboard>/d");

        look = new InputAction("Look", binding: "<Mouse>/delta");
        run = new InputAction("Run", binding: "<Keyboard>/leftShift");
        slow = new InputAction("Slow", binding: "<Keyboard>/leftCtrl");
        jump = new InputAction("Jump", binding: "<Keyboard>/space");
        grab = new InputAction("Grab", binding: "<Keyboard>/e");
        raise = new InputAction("Raise", binding: "<Mouse>/leftButton");
        lower = new InputAction("Lower", binding: "<Mouse>/rightButton");
    }

    private InputAction[] All => new[] { move, look, run, slow, jump, grab, raise, lower };

    private void OnEnable() { foreach (var a in All) a.Enable(); }
    private void OnDisable() { foreach (var a in All) a.Disable(); }

    private void Update()
    {
        current.Move = move.ReadValue<Vector2>();
        current.Look = look.ReadValue<Vector2>();
        current.Run = run.IsPressed();
        current.Slow = slow.IsPressed();
        current.JumpPressed = jump.WasPressedThisFrame();
        current.GrabPressed = grab.WasPressedThisFrame();
        current.Lift = (raise.IsPressed() ? 1f : 0f) - (lower.IsPressed() ? 1f : 0f);
    }
}
