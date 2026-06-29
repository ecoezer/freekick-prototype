using UnityEngine;

/// <summary>
/// Serbest vuruş kamerası — iki modda çalışır:
///
///   Idle Mode   : Şut atılmadı. Topun arkasında ve üstünde sabit durur,
///                 kale yönüne bakar.
///
///   Follow Mode : Şut atıldı. Topu Lerp ile yumuşak biçimde takip eder.
///
/// Mod geçişleri BallLauncher.OnBallFired ve BallResetter.OnBallReset
/// event'leriyle tetiklenir. Kamera herhangi bir hesaplama yapmaz;
/// yalnızca Transform günceller.
///
/// Sorumluluk: YALNIZCA kamera konumlandırma.
///   Bilmediği scriptler: ShotInput · ShotController
/// </summary>
public class FreekickCameraController : MonoBehaviour
{
    // ─── Inspector ───────────────────────────────────────────

    [Header("References")]
    [Tooltip("Takip edilecek top Transform'u.")]
    [SerializeField] private Transform    ballTransform;
    [SerializeField] private BallLauncher ballLauncher;
    [SerializeField] private BallResetter ballResetter;

    [Header("Idle Pose")]
    [Tooltip("Top henüz atılmadığında kameranın top pozisyonuna göre ofseti.")]
    [SerializeField] private Vector3 idleOffset     = new Vector3(0f, 3f, -6f);

    [Tooltip("Idle modunda kameranın baktığı nokta — top pozisyonuna göre ofset.")]
    [SerializeField] private Vector3 idleLookOffset = new Vector3(0f, 0.5f, 10f);

    [Header("Follow Mode")]
    [Tooltip("Topu takip ederken kameranın topa göre ofseti.")]
    [SerializeField] private Vector3 followOffset = new Vector3(0f, 2f, -4f);

    [Tooltip("Takip yumuşaklığı — yüksek değer daha sert (anlık) takip sağlar.")]
    [SerializeField, Range(1f, 15f)] private float followSmoothing = 6f;

    // ─── Private State ───────────────────────────────────────

    private bool isFollowing;

    // ─── Unity Lifecycle ─────────────────────────────────────

    private void OnEnable()
    {
        ballLauncher.OnBallFired += StartFollowing;
        ballResetter.OnBallReset += ReturnToIdle;
    }

    private void OnDisable()
    {
        ballLauncher.OnBallFired -= StartFollowing;
        ballResetter.OnBallReset -= ReturnToIdle;
    }

    private void LateUpdate()
    {
        // LateUpdate kullanımı: kamerayı, fizik ve diğer scriptler
        // aynı frame'de hareket ettikten SONRA günceller. Titreme önlenir.
        if (isFollowing)
            FollowBall();
        else
            HoldIdlePose();
    }

    // ─── Event Handlers ──────────────────────────────────────

    private void StartFollowing() => isFollowing = true;
    private void ReturnToIdle()   => isFollowing = false;

    // ─── Camera Modes ────────────────────────────────────────

    private void HoldIdlePose()
    {
        // Topun pozisyonuna sabit ofset ekle.
        transform.position = ballTransform.position + idleOffset;

        // Kale yönünde sabit bakış hedefini hesapla.
        Vector3 lookTarget = ballTransform.position + idleLookOffset;
        transform.LookAt(lookTarget);
    }

    private void FollowBall()
    {
        // Hedef pozisyonu: topun anlık pozisyonu + ofset.
        Vector3 targetPosition = ballTransform.position + followOffset;

        // Yumuşak geçiş için Lerp kullan.
        transform.position = Vector3.Lerp(
            transform.position,
            targetPosition,
            followSmoothing * Time.deltaTime
        );

        // Her frame topu izle.
        transform.LookAt(ballTransform.position);
    }
}
