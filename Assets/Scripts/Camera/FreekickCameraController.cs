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
    [SerializeField] private ShotInput    shotInput;

    [Tooltip("Kale merkezi — idle poz her zaman top→kale hattının arkasında kurulur.")]
    [SerializeField] private Transform    goalTransform;

    private Camera cam;

    [Header("Idle Pose")]
    [Tooltip("Kameranın topun arkasındaki mesafesi (top→kale hattı üzerinde).")]
    [SerializeField] private float idleDistance = 6f;

    [Tooltip("Kameranın top üzerindeki yüksekliği.")]
    [SerializeField] private float idleHeight = 2.8f;

    [Header("Follow Mode")]
    [Tooltip("Topu takip ederken kameranın topa göre ofseti.")]
    [SerializeField] private Vector3 followOffset = new Vector3(0f, 2f, -4f);

    [Tooltip("Takip yumuşaklığı — yüksek değer daha sert (anlık) takip sağlar.")]
    [SerializeField, Range(1f, 15f)] private float followSmoothing = 6f;

    [Header("Zoom (Juice)")]
    [SerializeField] private float baseFOV = 60f;
    [SerializeField] private float zoomedFOV = 55f;
    [SerializeField] private float zoomSpeed = 5f;

    // ─── Private State ───────────────────────────────────────

    private bool isFollowing;
    private float targetFOV;
    private Vector3 velocity = Vector3.zero;

    private void Awake()
    {
        cam = GetComponent<Camera>();
        if (cam != null)
        {
            targetFOV = cam.fieldOfView;
            baseFOV = cam.fieldOfView;
        }
    }

    private void OnEnable()
    {
        if (ballLauncher != null) ballLauncher.OnBallFired += StartFollowing;
        if (ballResetter != null) ballResetter.OnBallReset += ReturnToIdle;
        if (shotInput != null) shotInput.OnPowerChanged += HandlePowerChanged;
    }

    private void OnDisable()
    {
        if (ballLauncher != null) ballLauncher.OnBallFired -= StartFollowing;
        if (ballResetter != null) ballResetter.OnBallReset -= ReturnToIdle;
        if (shotInput != null) shotInput.OnPowerChanged -= HandlePowerChanged;
    }

    private void LateUpdate()
    {
        // LateUpdate kullanımı: kamerayı, fizik ve diğer scriptler
        // aynı frame'de hareket ettikten SONRA günceller. Titreme önlenir.
        if (isFollowing)
            FollowBall();
        else
            HoldIdlePose();
            
        // FOV Update (Zoom Juice)
        if (cam != null && cam.fieldOfView != targetFOV)
        {
            cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, targetFOV, Time.deltaTime * zoomSpeed);
        }
    }

    // ─── Event Handlers ──────────────────────────────────────

    private void StartFollowing() 
    {
        isFollowing = true;
        targetFOV = baseFOV; // Snap or lerp back to base FOV when shot is fired
    }
    
    private void ReturnToIdle() 
    {
        isFollowing = false;
        targetFOV = baseFOV;
    }
    
    private void HandlePowerChanged(float power)
    {
        if (!isFollowing)
        {
            targetFOV = Mathf.Lerp(baseFOV, zoomedFOV, power);
        }
    }

    // ─── Camera Modes ────────────────────────────────────────

    private void HoldIdlePose()
    {
        // Top→kale hattının arkasına yerleş; frikik noktası nereye taşınırsa
        // taşınsın kamera her zaman kaleye bakar.
        Vector3 toGoal = goalTransform != null
            ? goalTransform.position - ballTransform.position
            : Vector3.forward;
        toGoal.y = 0f;
        Vector3 dir = toGoal.normalized;

        transform.position = ballTransform.position - dir * idleDistance + Vector3.up * idleHeight;

        Vector3 lookTarget = ballTransform.position + dir * 10f + Vector3.up * 1.2f;
        transform.LookAt(lookTarget);
    }

    private void FollowBall()
    {
        // Hedef pozisyonu: topun anlık pozisyonu + ofset.
        Vector3 targetPosition = ballTransform.position + followOffset;

        // SmoothDamp for AAA camera lag feel (0.15s delay roughly represented by 1f/followSmoothing)
        float smoothTime = 1f / Mathf.Clamp(followSmoothing, 1f, 15f);
        transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref velocity, smoothTime);

        // Her frame topu izle.
        transform.LookAt(ballTransform.position);
    }
}
