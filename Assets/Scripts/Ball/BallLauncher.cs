using UnityEngine;
using System;

/// <summary>
/// Topa başlangıç hızı (velocity) atar.
///
/// Sorumluluk (SRP): YALNIZCA şut anında rb.linearVelocity ataması.
///   - AddForce KULLANILMAZ. Doğrudan velocity ataması yapılır.
///   - Böylece şut hızı kütle ve frame-rate'den bağımsız, deterministik olur.
///   - Fizik simülasyonu yapmaz → BallPhysicsController
///   - Durma tespiti yapmaz    → BallStateTracker
///   - Reset mantığı yoktur    → BallResetter
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class BallLauncher : MonoBehaviour
{
    // ─── Inspector ───────────────────────────────────────────

    [Header("Shot Speed")]
    [Tooltip("Power = 1.0 olduğunda topun çıkış hızı (m/s). 36 m/s ≈ 130 km/h.")]
    [SerializeField, Range(10f, 50f)] private float maxShotSpeed = 36f;

    [Header("Launch Angle")]
    [Tooltip("Şutun dikey açısı (derece). 0 = düz, 15 = hafif loblu.")]
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
    /// ShotData'ya göre topa başlangıç hızı atar.
    /// AddForce yerine doğrudan velocity manipülasyonu kullanılır.
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

        // Önceki hareketi temizle.
        ClearPhysicsState();

        // Şut hızını hesapla: Power (0-1) * maxShotSpeed (m/s)
        float speed = data.Power * maxShotSpeed;

        // Yön vektörünü dikey açıyla birleştir.
        // data.Direction yatay düzlemde (XZ) hedefi gösterir.
        // launchAngle kadar yukarı açı eklenir.
        Vector3 flatDirection = new Vector3(data.Direction.x, 0f, data.Direction.z).normalized;
        float   angleRad     = launchAngle * Mathf.Deg2Rad;
        Vector3 launchDir    = (flatDirection * Mathf.Cos(angleRad) + Vector3.up * Mathf.Sin(angleRad)).normalized;

        // Doğrudan velocity ataması — deterministik, kütle-bağımsız.
        rb.linearVelocity = launchDir * speed;

        OnBallFired?.Invoke();
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

    /// <summary>
    /// Rigidbody'nin linear ve angular hızını sıfırlar.
    /// </summary>
    private void ClearPhysicsState()
    {
        rb.linearVelocity  = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
    }
}
