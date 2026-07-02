using UnityEngine;
using System;
using System.Collections;

/// <summary>
/// Basit ama etkili kaleci yapay zekası.
///
/// Davranış:
///   Idle     : Kale merkezinde hafifçe sağa-sola salınır.
///   Reacting : Şuttan reactionDelay sonra topun kale çizgisini keseceği
///              noktayı (lineer + yerçekimi tahmini) izler, o yöne koşar.
///   Diving   : Top kaleye ~diveCommitTime kala tahmin noktasına dalar
///              (gövde yana yatar, collider dalış hattını kapatır).
///
/// Zorluk dengesi: reactionDelay + moveSpeed + predictionNoise üçlüsüyle
/// köşelere sert şutlar gol olur, orta/yavaş şutlar kurtarılır.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class GoalkeeperController : MonoBehaviour
{
    // ─── Inspector ───────────────────────────────────────────

    [Header("References")]
    [SerializeField] private BallLauncher ballLauncher;
    [SerializeField] private BallResetter ballResetter;
    [SerializeField] private Rigidbody    ballRigidbody;
    [SerializeField] private Transform    goalCenter;

    [Header("AI Tuning")]
    [Tooltip("Şuttan sonra kalecinin tepki vermeye başlaması için geçen süre (s).")]
    [SerializeField, Range(0f, 1f)] private float reactionDelay = 0.25f;

    [Tooltip("Yerde yana koşma hızı (m/s).")]
    [SerializeField, Range(2f, 12f)] private float moveSpeed = 5.5f;

    [Tooltip("Dalış hızı (m/s).")]
    [SerializeField, Range(5f, 20f)] private float diveSpeed = 10f;

    [Tooltip("Topun kaleye varmasına bu kadar süre kala dalışa geçilir (s).")]
    [SerializeField, Range(0.1f, 0.8f)] private float diveCommitTime = 0.3f;

    [Tooltip("Tahmine eklenen rastgele hata (m). Yüksek değer = beceriksiz kaleci.")]
    [SerializeField, Range(0f, 2f)] private float predictionNoise = 0.45f;

    [Tooltip("Kalecinin kale merkezinden uzaklaşabileceği maksimum yatay mesafe (m).")]
    [SerializeField, Range(2f, 5f)] private float lateralLimit = 3.3f;

    // ─── Events ──────────────────────────────────────────────

    /// <summary>Kaleci topa dokunduğunda tetiklenir (kurtarış tespiti).</summary>
    public event Action OnBallTouched;

    // ─── State ───────────────────────────────────────────────

    private enum KeeperState { Idle, Reacting, Diving }

    private KeeperState state = KeeperState.Idle;
    private Rigidbody   rb;
    private Vector3     homePosition;
    private Quaternion  homeRotation;
    private float       attemptNoise;     // Bu şuta özgü tahmin hatası.
    private Vector3     diveTarget;
    private float       idlePhase;

    // ─── Unity Lifecycle ─────────────────────────────────────

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;

        homePosition = transform.position;
        homeRotation = transform.rotation;
    }

    private void OnEnable()
    {
        if (ballLauncher != null) ballLauncher.OnBallFired += HandleBallFired;
        if (ballResetter != null) ballResetter.OnBallReset += ResetKeeper;
    }

    private void OnDisable()
    {
        if (ballLauncher != null) ballLauncher.OnBallFired -= HandleBallFired;
        if (ballResetter != null) ballResetter.OnBallReset -= ResetKeeper;
    }

    private void FixedUpdate()
    {
        switch (state)
        {
            case KeeperState.Idle:     IdleSway();  break;
            case KeeperState.Reacting: TrackBall(); break;
            case KeeperState.Diving:   DiveMove();  break;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.collider.GetComponentInParent<BallLauncher>() != null)
        {
            OnBallTouched?.Invoke();
        }
    }

    // ─── Event Handlers ──────────────────────────────────────

    private void HandleBallFired()
    {
        attemptNoise = UnityEngine.Random.Range(-predictionNoise, predictionNoise);
        StopAllCoroutines();
        StartCoroutine(ReactAfterDelay());
    }

    private IEnumerator ReactAfterDelay()
    {
        yield return new WaitForSeconds(reactionDelay);
        if (state == KeeperState.Idle) state = KeeperState.Reacting;
    }

    /// <summary>Yeni şut için kaleyi merkezden korumaya dön.</summary>
    public void ResetKeeper()
    {
        StopAllCoroutines();
        state = KeeperState.Idle;
        transform.SetPositionAndRotation(homePosition, homeRotation);
    }

    // ─── AI Behaviours ───────────────────────────────────────

    private void IdleSway()
    {
        idlePhase += Time.fixedDeltaTime;
        float sway = Mathf.Sin(idlePhase * 1.4f) * 0.35f;
        Vector3 target = homePosition + goalRight * sway;
        rb.MovePosition(Vector3.MoveTowards(rb.position, target, moveSpeed * 0.3f * Time.fixedDeltaTime));
    }

    private void TrackBall()
    {
        if (!TryPredictCrossing(out Vector3 predicted, out float timeToGoal))
            return;

        // Dalış zamanı geldi mi?
        if (timeToGoal <= diveCommitTime)
        {
            diveTarget = ClampToReach(predicted);
            state      = KeeperState.Diving;
            return;
        }

        // Yerde yana kayarak pozisyon al (yükseklik home'da kalır).
        Vector3 groundTarget = homePosition + goalRight * LateralOffset(predicted);
        rb.MovePosition(Vector3.MoveTowards(rb.position, groundTarget, moveSpeed * Time.fixedDeltaTime));
        }

    private void DiveMove()
    {
        rb.MovePosition(Vector3.MoveTowards(rb.position, diveTarget, diveSpeed * Time.fixedDeltaTime));

        // Gövdeyi dalış yönüne yatır (görsel + collider kapsaması).
        float side = Mathf.Sign(LateralOffset(diveTarget));
        float lean = Mathf.Clamp01(Vector3.Distance(rb.position, homePosition) / 2.2f) * 62f;
        Quaternion targetRot = homeRotation * Quaternion.AngleAxis(-side * lean, Vector3.forward);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, 240f * Time.fixedDeltaTime);
    }

    // ─── Prediction Helpers ──────────────────────────────────

    /// <summary>
    /// Topun kale düzlemini keseceği noktayı tahmin eder.
    /// Lineer XZ + yerçekimli Y tahmini; falsolu toplarda doğal olarak yanılır.
    /// </summary>
    private bool TryPredictCrossing(out Vector3 predicted, out float timeToGoal)
    {
        predicted  = homePosition;
        timeToGoal = float.MaxValue;

        Vector3 ballPos = ballRigidbody.position;
        Vector3 v       = ballRigidbody.linearVelocity;

        // Kale düzlemine doğru ilerleme bileşeni.
        Vector3 toGoal   = goalCenter.position - ballPos;
        Vector3 goalFwd  = new Vector3(toGoal.x, 0f, toGoal.z).normalized;
        float   approach = Vector3.Dot(v, goalFwd);

        if (approach < 0.5f) return false; // Top kaleye yaklaşmıyor.

        float dist = new Vector3(toGoal.x, 0f, toGoal.z).magnitude;
        float t    = dist / approach;

        float g  = Mathf.Abs(Physics.gravity.y);
        float py = ballPos.y + v.y * t - 0.5f * g * t * t;

        Vector3 flatCross = ballPos + new Vector3(v.x, 0f, v.z) * t;

        predicted = new Vector3(flatCross.x, Mathf.Clamp(py, 0.3f, 2.2f), flatCross.z)
                  + goalRight * attemptNoise;
        timeToGoal = t;
        return true;
    }

    private Vector3 goalRight => goalCenter != null ? goalCenter.right : Vector3.right;

    private float LateralOffset(Vector3 worldPoint)
    {
        return Vector3.Dot(worldPoint - homePosition, goalRight);
    }

    private Vector3 ClampToReach(Vector3 predicted)
    {
        float lateral = Mathf.Clamp(LateralOffset(predicted), -lateralLimit, lateralLimit);
        float height  = Mathf.Clamp(predicted.y - homePosition.y, 0f, 1.2f);
        return homePosition + goalRight * lateral + Vector3.up * height;
    }
}
