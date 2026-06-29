using UnityEngine;
using System;

/// <summary>
/// Topa fiziksel kuvvet (impulse) uygular.
///
/// Sorumluluk: YALNIZCA Fire(ShotData) — Rigidbody'e anlık kuvvet ekler.
///   - Durma tespiti yapmaz  → BallStateTracker
///   - Reset mantığı yoktur  → BallResetter
///
/// BallLauncher doğrudan Rigidbody ile konuşur; başka hiçbir
/// oyun scriptini tanımaz.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class BallLauncher : MonoBehaviour
{
    // ─── Inspector ───────────────────────────────────────────

    [Header("Shot Force")]
    [Tooltip("Güç 1.0 olduğunda uygulanacak maksimum impulse kuvveti (Newton·saniye).")]
    [SerializeField, Range(5f, 60f)] private float maxShotForce = 30f;

    // ─── Events ──────────────────────────────────────────────

    /// <summary>
    /// Top başarıyla fırlatıldığında tetiklenir.
    /// FreekickCameraController ve BallStateTracker bu eventi dinler.
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
    /// ShotData'ya göre topa impulse kuvveti uygular.
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

        // Önceki hareketi temizle, ardından kuvvet uygula.
        ClearPhysicsState();
        rb.AddForce(data.Direction * (data.Power * maxShotForce), ForceMode.Impulse);

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
    /// Fire() öncesi ve Reset sonrasında çağrılır.
    /// </summary>
    private void ClearPhysicsState()
    {
        rb.linearVelocity  = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
    }
}
