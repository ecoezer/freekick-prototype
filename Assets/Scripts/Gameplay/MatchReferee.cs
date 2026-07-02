using UnityEngine;
using System;
using System.Collections;

/// <summary>
/// Şutun sonucunu belirleyen hakem.
///
/// Sonuç öncelik sırası:
///   GOL      : GoalDetector tetiklendi (kaleci değse bile gol sayılır).
///   KURTARIŞ : Gol olmadı + kaleci topa dokundu.
///   BARAJ    : Gol olmadı + baraj topa dokundu (kaleci dokunmadı).
///   DİREK    : Gol olmadı + direğe çarptı (kimse dokunmadı).
///   AUT      : Hiçbiri.
///
/// Ayrıca 9 saniyelik failsafe: top hiçbir zaman "durmadıysa" zorla reset.
/// </summary>
public class MatchReferee : MonoBehaviour
{
    // ─── Inspector ───────────────────────────────────────────

    [Header("References")]
    [SerializeField] private BallLauncher         ballLauncher;
    [SerializeField] private BallStateTracker     ballStateTracker;
    [SerializeField] private BallResetter         ballResetter;
    [SerializeField] private GoalDetector         goalDetector;
    [SerializeField] private GoalkeeperController goalkeeper;
    [SerializeField] private DefensiveWall        wall;

    [Header("Failsafe")]
    [Tooltip("Şuttan bu kadar saniye sonra sonuç yoksa zorla reset (kaçak top).")]
    [SerializeField, Range(4f, 20f)] private float attemptTimeout = 9f;

    // ─── Outcome ─────────────────────────────────────────────

    public enum ShotOutcome { Goal, Saved, Wall, Woodwork, Miss }

    /// <summary>Yeni şut başladığında tetiklenir (HUD mesajı temizler).</summary>
    public event Action OnAttemptStarted;

    /// <summary>Şutun sonucu belli olduğunda tetiklenir. GameManager ve HUD dinler.</summary>
    public event Action<ShotOutcome> OnOutcome;

    // ─── Private State ───────────────────────────────────────

    private bool attemptActive;
    private bool keeperTouched;
    private bool wallTouched;
    private bool woodworkTouched;
    private Coroutine timeoutRoutine;

    // ─── Unity Lifecycle ─────────────────────────────────────

    private void Awake()
    {
        // Direkler sahnede birden fazla — otomatik bul, tek tek wiring gerekmesin.
        foreach (WoodworkNotifier post in FindObjectsByType<WoodworkNotifier>(FindObjectsSortMode.None))
            post.OnBallHit += HandleWoodwork;
    }

    private void OnEnable()
    {
        if (ballLauncher != null)     ballLauncher.OnBallFired        += HandleBallFired;
        if (ballStateTracker != null) ballStateTracker.OnBallStopped  += HandleBallStopped;
        if (goalDetector != null)     goalDetector.OnGoalScored       += HandleGoal;
        if (goalkeeper != null)       goalkeeper.OnBallTouched        += HandleKeeperTouch;
        if (wall != null)             wall.OnBallTouched              += HandleWallTouch;
    }

    private void OnDisable()
    {
        if (ballLauncher != null)     ballLauncher.OnBallFired        -= HandleBallFired;
        if (ballStateTracker != null) ballStateTracker.OnBallStopped  -= HandleBallStopped;
        if (goalDetector != null)     goalDetector.OnGoalScored       -= HandleGoal;
        if (goalkeeper != null)       goalkeeper.OnBallTouched        -= HandleKeeperTouch;
        if (wall != null)             wall.OnBallTouched              -= HandleWallTouch;
    }

    // ─── Event Handlers ──────────────────────────────────────

    private void HandleBallFired()
    {
        attemptActive   = true;
        keeperTouched   = false;
        wallTouched     = false;
        woodworkTouched = false;

        if (timeoutRoutine != null) StopCoroutine(timeoutRoutine);
        timeoutRoutine = StartCoroutine(TimeoutRoutine());

        OnAttemptStarted?.Invoke();
    }

    private void HandleGoal()
    {
        if (!attemptActive) return;
        Conclude(ShotOutcome.Goal);
    }

    private void HandleBallStopped()
    {
        if (!attemptActive) return;

        ShotOutcome outcome =
            keeperTouched   ? ShotOutcome.Saved :
            wallTouched     ? ShotOutcome.Wall  :
            woodworkTouched ? ShotOutcome.Woodwork :
                              ShotOutcome.Miss;

        Conclude(outcome);
    }

    private void HandleKeeperTouch() { keeperTouched   = true; }
    private void HandleWallTouch()   { wallTouched     = true; }
    private void HandleWoodwork()    { woodworkTouched = true; }

    // ─── Private Methods ─────────────────────────────────────

    private void Conclude(ShotOutcome outcome)
    {
        attemptActive = false;

        if (timeoutRoutine != null)
        {
            StopCoroutine(timeoutRoutine);
            timeoutRoutine = null;
        }

        OnOutcome?.Invoke(outcome);
    }

    private IEnumerator TimeoutRoutine()
    {
        yield return new WaitForSeconds(attemptTimeout);

        if (attemptActive)
        {
            // Kaçak top: sonucu karara bağla ve zorla resetle.
            HandleBallStopped();
            ballResetter.ForceReset();
        }
    }
}
