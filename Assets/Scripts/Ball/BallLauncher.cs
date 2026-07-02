using UnityEngine;
using System;

/// <summary>
/// Topa başlangıç hızı (velocity) ve falso spini (angular velocity) atar.
///
/// Sorumluluk (SRP): YALNIZCA şut anında balistik çözüm + hız ataması.
///   - Nişangâh noktasına ulaşacak yay (parabol) hesaplanır:
///       yatay hız  = Power ile doğrusal (min→max)
///       dikey hız  = hedefe tam oturan balistik çözüm (vy = dy/t + g·t/2)
///   - Falso: çıkış yönü falsonun tersine bükülür, topa Y-ekseni spini verilir.
///     Magnus etkisi (BallPhysicsController) topu uçuşta hedefe geri kıvırır.
///   - Fizik simülasyonu yapmaz → BallPhysicsController
///   - Durma tespiti yapmaz    → BallStateTracker
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class BallLauncher : MonoBehaviour
{
    // ─── Inspector ───────────────────────────────────────────

    [Header("Shot Speed")]
    [Tooltip("Power = 0 iken yatay çıkış hızı (m/s).")]
    [SerializeField, Range(5f, 25f)] private float minShotSpeed = 13f;

    [Tooltip("Power = 1 iken yatay çıkış hızı (m/s). 32 m/s ≈ 115 km/h.")]
    [SerializeField, Range(20f, 50f)] private float maxShotSpeed = 32f;

    [Header("Curve (Falso)")]
    [Tooltip("Tam falsoda çıkış yönünün büküleceği açı (derece). Magnus geri kıvırır.")]
    [SerializeField, Range(0f, 15f)] private float bendAngleDeg = 6.5f;

    [Tooltip("Tam falsoda topa verilecek Y-ekseni spini (rad/s).")]
    [SerializeField, Range(0f, 30f)] private float maxSpin = 9f;

    [Header("Fallback (hedefsiz şut)")]
    [Tooltip("TargetPoint olmayan eski tip şutlarda kullanılan dikey açı.")]
    [SerializeField, Range(0f, 45f)] private float launchAngle = 12f;

    // ─── Events ──────────────────────────────────────────────

    /// <summary>
    /// Top başarıyla fırlatıldığında tetiklenir.
    /// BallPhysicsController ve BallStateTracker bu eventi dinler.
    /// </summary>
    public event Action OnBallFired;

    // ─── Private State ───────────────────────────────────────

    private Rigidbody rb;
    private bool      isInFlight;

    // ─── Unity Lifecycle ─────────────────────────────────────

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    // ─── Public API ──────────────────────────────────────────

    /// <summary>
    /// ShotData'ya göre topa başlangıç hızı ve spin atar.
    /// Top zaten uçuştaysa çağrı güvenli biçimde görmezden gelinir.
    /// </summary>
    public void Fire(ShotData data)
    {
        if (isInFlight)
        {
            Debug.LogWarning("[BallLauncher] Top zaten uçuşta, Fire() yoksayıldı.");
            return;
        }

        isInFlight = true;
        ClearPhysicsState();

        Vector3 velocity;
        Vector3 spin = Vector3.zero;

        if (data.HasTarget)
        {
            velocity = ComputeLaunchVelocity(transform.position, data.TargetPoint,
                                             data.Power, data.Curve, out spin);
        }
        else
        {
            // Eski davranış: sabit dikey açı + yön.
            float speed = Mathf.Lerp(minShotSpeed, maxShotSpeed, data.Power);
            Vector3 flatDirection = new Vector3(data.Direction.x, 0f, data.Direction.z).normalized;
            float   angleRad      = launchAngle * Mathf.Deg2Rad;
            velocity = (flatDirection * Mathf.Cos(angleRad) + Vector3.up * Mathf.Sin(angleRad)).normalized * speed;
        }

        rb.linearVelocity  = velocity;
        rb.angularVelocity = spin;

        OnBallFired?.Invoke();
    }

    /// <summary>
    /// Hedef noktaya oturan balistik çıkış hızını hesaplar.
    /// AimVisualController de yay önizlemesi için aynı hesabı kullanır (tutarlılık).
    /// </summary>
    public Vector3 ComputeLaunchVelocity(Vector3 from, Vector3 target,
                                         float power, float curve, out Vector3 spin)
    {
        Vector3 to       = target - from;
        Vector3 flat     = new Vector3(to.x, 0f, to.z);
        float   flatDist = Mathf.Max(flat.magnitude, 0.5f);

        float horizontalSpeed = Mathf.Lerp(minShotSpeed, maxShotSpeed, Mathf.Clamp01(power));
        float flightTime      = flatDist / horizontalSpeed;

        // Balistik dikey hız: hedefin yüksekliğine flightTime anında ulaş.
        float g  = Mathf.Abs(Physics.gravity.y);
        float vy = (to.y / flightTime) + 0.5f * g * flightTime;

        // Falso: çıkış yönünü falsonun TERSİNE bük; Magnus uçuşta geri kıvırır.
        Vector3 flatDir = Quaternion.AngleAxis(-curve * bendAngleDeg, Vector3.up) * flat.normalized;

        spin = Vector3.up * (curve * maxSpin);

        return flatDir * horizontalSpeed + Vector3.up * vy;
    }

    /// <summary>
    /// BallResetter topu sıfırladıktan sonra bu metodu çağırarak
    /// bir sonraki şuta hazır duruma getirir.
    /// </summary>
    public void ReadyForNextShot()
    {
        isInFlight = false;
        ClearPhysicsState();
    }

    // ─── Private Helpers ─────────────────────────────────────

    private void ClearPhysicsState()
    {
        rb.linearVelocity  = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
    }
}
