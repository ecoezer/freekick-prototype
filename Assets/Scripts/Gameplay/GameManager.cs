using UnityEngine;
using System;

/// <summary>
/// Oyun döngüsü ve skor yönetimi.
///
///   - Her şutun sonucunu MatchReferee'den alır, istatistikleri günceller.
///   - Şuttan sonra YENİ bir frikik noktası seçer (mesafe 16-26 m, açı ±32°),
///     BallResetter'a bildirir; reset gerçekleşince barajı yeni noktaya kurar.
///   - Seri (streak) ve rekor seri PlayerPrefs ile kalıcıdır.
/// </summary>
public class GameManager : MonoBehaviour
{
    // ─── Inspector ───────────────────────────────────────────

    [Header("References")]
    [SerializeField] private MatchReferee  referee;
    [SerializeField] private BallResetter  ballResetter;
    [SerializeField] private DefensiveWall wall;
    [SerializeField] private Transform     goalCenter;

    [Header("Freekick Spot Randomization")]
    [SerializeField] private float minDistance = 16f;
    [SerializeField] private float maxDistance = 26f;
    [SerializeField] private float maxAngleDeg = 32f;
    [Tooltip("Topun yerden yüksekliği (yarıçap).")]
    [SerializeField] private float ballRadius  = 0.11f;

    // ─── Stats ───────────────────────────────────────────────

    public int Goals       { get; private set; }
    public int Shots       { get; private set; }
    public int Streak      { get; private set; }
    public int BestStreak  { get; private set; }

    /// <summary>İstatistikler değiştiğinde tetiklenir. HUD dinler.</summary>
    public event Action OnStatsChanged;

    private const string BestStreakKey = "FreekickGame.BestStreak";

    // ─── Unity Lifecycle ─────────────────────────────────────

    private void Awake()
    {
        BestStreak = PlayerPrefs.GetInt(BestStreakKey, 0);
    }

    /// <summary>Konsoldan hangi build'in çalıştığını doğrulamak için sürüm etiketi.</summary>
    public const string BuildVersion = "v5";

    /// <summary>
    /// Kaleden sahaya bakan yön. Kale transformunun rotasyonuna GÜVENİLMEZ;
    /// topun ilk dizilişinden türetilir — spawn asla kalenin arkasına düşemez.
    /// </summary>
    private Vector3 fieldDir;

    private Rigidbody ballRb;
    private float nextDiagTime;

    private void Start()
    {
        Debug.Log($"[FreekickGame] Build {BuildVersion}");

        Vector3 toField = ballResetter.SpawnPoint - goalCenter.position;
        toField.y = 0f;
        fieldDir = toField.sqrMagnitude > 0.01f ? toField.normalized : -goalCenter.forward;

        ballRb = ballResetter.GetComponent<Rigidbody>();
        // İlk kurulum: baraj mevcut top noktasına göre dizilir.
        if (wall != null)
            wall.Configure(ballResetter.SpawnPoint);

        OnStatsChanged?.Invoke();
    }

    private void OnEnable()
    {
        if (referee != null)      referee.OnOutcome        += HandleOutcome;
        if (ballResetter != null) ballResetter.OnBallReset += HandleBallReset;
    }

    private void OnDisable()
    {
        if (referee != null)      referee.OnOutcome        -= HandleOutcome;
        if (ballResetter != null) ballResetter.OnBallReset -= HandleBallReset;
    }

    // ─── Event Handlers ──────────────────────────────────────

    private void HandleOutcome(MatchReferee.ShotOutcome outcome)
    {
        Shots++;

        if (outcome == MatchReferee.ShotOutcome.Goal)
        {
            Goals++;
            Streak++;

            if (Streak > BestStreak)
            {
                BestStreak = Streak;
                PlayerPrefs.SetInt(BestStreakKey, BestStreak);
                PlayerPrefs.Save();
            }
        }
        else
        {
            Streak = 0;
        }

        // Bir sonraki frikik noktasını şimdiden belirle — reset bu noktaya taşır.
        ballResetter.SetSpawnPoint(PickRandomSpot());

        OnStatsChanged?.Invoke();
    }

    private void HandleBallReset()
    {
        // Top yeni noktasına taşındı; barajı ona göre yeniden kur.
        if (wall != null)
            wall.Configure(ballResetter.SpawnPoint);
    }

    // ─── Private Methods ─────────────────────────────────────

    private Vector3 PickRandomSpot()
    {
        float angle = UnityEngine.Random.Range(-maxAngleDeg, maxAngleDeg);
        float dist  = UnityEngine.Random.Range(minDistance, maxDistance);

        // fieldDir topun İLK dizilişinden türetildi — spot her zaman saha tarafında.
        Vector3 dir  = Quaternion.AngleAxis(angle, Vector3.up) * fieldDir;
        Vector3 spot = goalCenter.position + dir * dist;
        spot.y = ballRadius;

        Debug.Log($"[GameManager] Yeni frikik noktası: {spot:F1} (açı {angle:F0}°, mesafe {dist:F1} m)");
        return spot;
    }

    private void Update()
    {
        // Teşhis: zaman ölçeği veya top hızı anormalse her 3 sn'de logla.
        if (Time.unscaledTime < nextDiagTime) return;
        nextDiagTime = Time.unscaledTime + 3f;

        if (ballRb != null && (Time.timeScale < 0.99f || ballRb.linearVelocity.magnitude > 0.2f))
        {
            Debug.Log($"[Diag] timeScale={Time.timeScale:F2} topHız={ballRb.linearVelocity.magnitude:F1} m/s topPoz={ballRb.position:F1}");
        }
    }
}
