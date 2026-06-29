using UnityEngine;
using System;

/// <summary>
/// Topun fiziksel durumunu (Idle → InFlight → Stopped) izler ve event üretir.
///
/// Sorumluluk: YALNIZCA durum tespiti ve event yayma.
///   - Herhangi bir eylem veya sahne değişikliği tetiklemez.
///   - BallLauncher.OnBallFired eventi aracılığıyla takibi başlatır.
///
/// Stop detection:
///   Velocity eşiğinin altına düşmek yeterli değildir; bounce sonrası
///   geçici yavaşlama yerine gerçek durma tespiti için birkaç frame
///   boyunca yavaş kalma şartı aranır (stoppedConfirmationFrames).
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class BallStateTracker : MonoBehaviour
{
    // ─── Inspector ───────────────────────────────────────────

    [Header("References")]
    [SerializeField] private BallLauncher ballLauncher;

    [Header("Stop Detection")]
    [Tooltip("Bu hız eşiğinin altına düşünce top 'yavaşladı' kabul edilir (m/s).")]
    [SerializeField, Range(0.01f, 1f)] private float stoppedVelocityThreshold = 0.08f;

    [Tooltip("Yavaş hızın kaç ardışık FixedUpdate frame boyunca devam etmesi gerekir.")]
    [SerializeField, Range(1, 90)] private int stoppedConfirmationFrames = 15;

    // ─── Events ──────────────────────────────────────────────

    /// <summary>
    /// Top gerçekten durduğunda tetiklenir.
    /// BallResetter bu eventi dinler.
    /// </summary>
    public event Action OnBallStopped;

    // ─── Private State ───────────────────────────────────────

    private Rigidbody rb;
    private bool      isTracking;
    private int       slowFrameCount;

    // ─── Unity Lifecycle ─────────────────────────────────────

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void OnEnable()
    {
        ballLauncher.OnBallFired += StartTracking;
    }

    private void OnDisable()
    {
        ballLauncher.OnBallFired -= StartTracking;
    }

    private void FixedUpdate()
    {
        if (!isTracking) return;

        CheckIfStopped();
    }

    // ─── Private Methods ─────────────────────────────────────

    private void StartTracking()
    {
        isTracking     = true;
        slowFrameCount = 0;
    }

    private void CheckIfStopped()
    {
        bool isSlow = rb.linearVelocity.magnitude < stoppedVelocityThreshold;

        if (isSlow)
        {
            slowFrameCount++;

            if (slowFrameCount >= stoppedConfirmationFrames)
                ConfirmStopped();
        }
        else
        {
            // Hız tekrar yükselirse sayacı sıfırla.
            // Örnek: top kaleye çarptı ve geri sekti.
            slowFrameCount = 0;
        }
    }

    private void ConfirmStopped()
    {
        isTracking     = false;
        slowFrameCount = 0;

        OnBallStopped?.Invoke();
    }
}
