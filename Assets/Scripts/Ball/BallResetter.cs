using UnityEngine;
using System;
using System.Collections;

/// <summary>
/// Top durduğunda belirli bir süre bekler, ardından topu başlangıç
/// pozisyonuna döndürerek sistemi yeni şuta hazırlar.
///
/// Sorumluluk: YALNIZCA reset zamanlama ve pozisyon geri alma.
///   - Fizik uygulamaz  → BallLauncher
///   - Durma tespiti yapmaz → BallStateTracker
/// </summary>
public class BallResetter : MonoBehaviour
{
    // ─── Inspector ───────────────────────────────────────────

    [Header("References")]
    [SerializeField] private BallStateTracker       ballStateTracker;
    [SerializeField] private BallLauncher            ballLauncher;
    [SerializeField] private BallPhysicsController   ballPhysicsController;

    [Header("Reset Settings")]
    [Tooltip("Top durduğunda, sahneyi sıfırlamadan önce kaç saniye beklenir.")]
    [SerializeField, Range(0f, 5f)] private float resetDelay = 2f;

    // ─── Events ──────────────────────────────────────────────

    /// <summary>
    /// Top başarıyla sıfırlandığında ve yeni şut atılabilir olduğunda tetiklenir.
    /// ShotController ve FreekickCameraController bu eventi dinler.
    /// </summary>
    public event Action OnBallReset;

    // ─── Private State ───────────────────────────────────────

    private Vector3    startPosition;
    private Quaternion startRotation;

    // ─── Unity Lifecycle ─────────────────────────────────────

    private void Awake()
    {
        // Sahne açıldığındaki pozisyonu serbest vuruş başlangıcı olarak sakla.
        startPosition = transform.position;
        startRotation = transform.rotation;
    }

    private void OnEnable()
    {
        ballStateTracker.OnBallStopped += HandleBallStopped;
    }

    private void OnDisable()
    {
        ballStateTracker.OnBallStopped -= HandleBallStopped;
    }

    // ─── Private Methods ─────────────────────────────────────

    private void HandleBallStopped()
    {
        StartCoroutine(ResetAfterDelay());
    }

    private IEnumerator ResetAfterDelay()
    {
        yield return new WaitForSeconds(resetDelay);

        ApplyReset();
    }

    private void ApplyReset()
    {
        // 1. BallLauncher'ın iç durumunu temizle (isInFlight = false, hız = 0).
        ballLauncher.ReadyForNextShot();

        // 2. BallPhysicsController'ın state'ini Idle'a döndür.
        if (ballPhysicsController != null)
            ballPhysicsController.ResetState();

        // 3. Fiziksel pozisyonu serbest vuruş başlangıcına geri taşı.
        transform.SetPositionAndRotation(startPosition, startRotation);

        // 4. Koordinatörleri bilgilendir (input aç, kamera geri dön).
        OnBallReset?.Invoke();
    }
}
