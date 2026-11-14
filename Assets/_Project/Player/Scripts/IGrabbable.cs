// Oyuncunun E ile tutabileceði her nesne bunu uygular (ör. Ýdil'in tepsi ucu).
// Ortak projede Shared/ klasörüne taþýnacak; deðiþtirmeden önce ekiple konuþulur.
public interface IGrabbable
{
    bool CanGrab(PlayerHands hands);   // þu an tutulabilir mi?
    void OnGrab(PlayerHands hands);    // tutuldu
    void OnRelease(PlayerHands hands); // býrakýldý
}