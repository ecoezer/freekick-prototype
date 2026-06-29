using UnityEngine;

/// <summary>
/// ShotInput ile BallLauncher arasındaki koordinatör.
///
/// Sorumluluk: YALNIZCA koordinasyon — doğru event'i dinle, doğru metodu çağır.
///   - Kendi başına hiçbir hesaplama yapmaz.
///   - Fizik veya durum yönetimi içermez.
///   - Bildiği scriptler: ShotInput · BallLauncher · BallResetter
///   - Bilmediği scriptler: CameraController · UI · BallStateTracker
/// </summary>
public class ShotController : MonoBehaviour
{
    // ─── Inspector ───────────────────────────────────────────

    [Header("References")]
    [SerializeField] private ShotInput    shotInput;
    [SerializeField] private BallLauncher ballLauncher;
    [SerializeField] private BallResetter ballResetter;

    // ─── Unity Lifecycle ─────────────────────────────────────

    private void OnEnable()
    {
        shotInput.OnShotReady    += HandleShotReady;
        ballResetter.OnBallReset += HandleBallReset;
    }

    private void OnDisable()
    {
        shotInput.OnShotReady    -= HandleShotReady;
        ballResetter.OnBallReset -= HandleBallReset;
    }

    // ─── Event Handlers ──────────────────────────────────────

    private void HandleShotReady(ShotData data)
    {
        // Şut atılırken yeni input engellenir.
        shotInput.SetEnabled(false);

        // Topu fırlat.
        ballLauncher.Fire(data);
    }

    private void HandleBallReset()
    {
        // Top sıfırlandıktan sonra oyuncunun yeni şut atmasına izin ver.
        shotInput.SetEnabled(true);
    }
}
