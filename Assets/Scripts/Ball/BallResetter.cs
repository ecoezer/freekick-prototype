using UnityEngine;
using System;
using System.Collections;

/// <summary>
/// Top durduğunda belirli bir süre bekler, ardından topu spawn noktasına
/// döndürerek sistemi yeni şuta hazırlar.
///
/// Sorumluluk: YALNIZCA reset zamanlama ve pozisyon geri alma.
///   - Spawn noktası GameManager tarafından her şutta değiştirilebilir (SetSpawnPoint).
///   - MatchReferee kaçak toplar için ForceReset çağırabilir (failsafe).
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
    [SerializeField, Range(0f, 5f)] private float resetDelay = 1.2f;

    // ─── Events ──────────────────────────────────────────────

    /// <summary>
    /// Top başarıyla sıfırlandığında ve yeni şut atılabilir olduğunda tetiklenir.
    /// ShotController, kamera, kaleci, baraj ve GameManager bu eventi dinler.
    /// </summary>
    public event Action OnBallReset;

    // ─── Private State ───────────────────────────────────────

    private Vector3    spawnPosition;
    private Quaternion spawnRotation;
    private TrailRenderer trail;

    // ─── Unity Lifecycle ─────────────────────────────────────

    private void Awake()
    {
        // Sahne açılışındaki pozisyon ilk spawn noktasıdır.
        spawnPosition = transform.position;
        spawnRotation = transform.rotation;
        trail         = GetComponent<TrailRenderer>();
    }

    private void OnEnable()
    {
        ballStateTracker.OnBallStopped += HandleBallStopped;
    }

    private void OnDisable()
    {
        ballStateTracker.OnBallStopped -= HandleBallStopped;
    }

    // ─── Public API ──────────────────────────────────────────

    /// <summary>
    /// Bir sonraki reset'te topun taşınacağı noktayı değiştirir.
    /// GameManager her şuttan sonra yeni frikik noktası atamak için kullanır.
    /// </summary>
    public void SetSpawnPoint(Vector3 position)
    {
        spawnPosition = position;
    }

    /// <summary>Mevcut spawn noktası (baraj/kamera yerleşimi için).</summary>
    public Vector3 SpawnPoint => spawnPosition;

    /// <summary>
    /// Bekleme yapmadan derhal reset uygular (failsafe / kaçak top durumu).
    /// </summary>
    public void ForceReset()
    {
        StopAllCoroutines();
        ApplyReset();
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

        // 3. Fiziksel pozisyonu spawn noktasına taşı.
        transform.SetPositionAndRotation(spawnPosition, spawnRotation);

        // 4. İz efektini temizle (ışınlanma çizgisi kalmasın).
        if (trail != null) trail.Clear();

        // 5. Koordinatörleri bilgilendir (input aç, kamera geri dön, saha kurulumu).
        OnBallReset?.Invoke();
    }
}
