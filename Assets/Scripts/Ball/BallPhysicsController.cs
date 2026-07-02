using UnityEngine;
using System;

/// <summary>
/// Ball Simulation Document v1'e göre topun tüm fizik davranışını yönetir.
///
/// Sorumluluk (SRP): Havadaki ve yerdeki fizik kuvvetlerinin hesaplanması.
///   - Aerodinamik hava direnci (v² orantılı drag)
///   - Magnus etkisi (falso / curve)
///   - Zemin sürtünmesi (rolling friction)
///   - Sekme sönümleme (bounce kill)
///
/// Unity'nin varsayılan damping değeri 0 tutulur.
/// Tüm kuvvetler FixedUpdate içinde velocity manipülasyonu ile uygulanır.
/// PhysX sadece çarpışma çözümlemesi (collision resolution) için kullanılır.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class BallPhysicsController : MonoBehaviour
{
    // ─── Inspector: References ───────────────────────────────

    [Header("References")]
    [Tooltip("Top fırlatıldığında haberdar olmak için referans.")]
    [SerializeField] private BallLauncher ballLauncher;

    // ─── Inspector: Aerodynamic Drag (Hava Direnci) ──────────

    [Header("Air Physics - Aerodynamic Drag")]
    [Tooltip("Hava direnci katsayısı. Yüksek değer = top havada daha çabuk yavaşlar.")]
    [SerializeField, Range(0.001f, 0.05f)] private float airDragCoefficient = 0.008f;

    // ─── Inspector: Magnus Effect (Falso) ────────────────────

    [Header("Air Physics - Magnus Effect (Spin)")]
    [Tooltip("Magnus kuvvet katsayısı. Yüksek değer = daha belirgin falso etkisi.")]
    [SerializeField, Range(0f, 1f)] private float magnusCoefficient = 0.012f;

    // ─── Inspector: Ground Friction (Zemin Sürtünmesi) ───────

    [Header("Ground Physics - Friction")]
    [Tooltip("Zemin sürtünme katsayısı. Yüksek değer = top yerde daha çabuk durur.")]
    [SerializeField, Range(0.5f, 10f)] private float groundFriction = 3f;

    // ─── Inspector: Bounce Kill (Sekme Sönümleme) ────────────

    [Header("Ground Physics - Bounce Kill")]
    [Tooltip("Bu dikey hız eşiğinin altındaki sekmeler sıfırlanır (jitter önleme).")]
    [SerializeField, Range(0.1f, 2f)] private float bounceKillThreshold = 0.5f;

    // ─── Public Read-Only (önizleme tutarlılığı için) ────────

    public float AirDragCoefficient => airDragCoefficient;
    public float MagnusCoefficient  => magnusCoefficient;

    // ─── State Enum ──────────────────────────────────────────

    private enum BallPhysicsState
    {
        Idle,       // Şut bekleniyor, fizik hesabı yapılmaz.
        InFlight,   // Havada — aerodinamik drag + magnus uygulanır.
        OnGround    // Yerde — zemin sürtünmesi uygulanır.
    }

    // ─── Events ──────────────────────────────────────────────

    /// <summary>Top zemine ilk temas ettiğinde tetiklenir.</summary>
    public event Action OnGroundContact;

    // ─── Private State ───────────────────────────────────────

    private Rigidbody rb;
    private BallPhysicsState currentState = BallPhysicsState.Idle;

    // ─── Unity Lifecycle ─────────────────────────────────────

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        // Unity'nin varsayılan damping'ini devre dışı bırak.
        // Tüm sürtünme hesaplamaları bu script tarafından yapılır.
        rb.linearDamping  = 0f;
        rb.angularDamping = 0f;
    }

    private void OnEnable()
    {
        if (ballLauncher != null)
            ballLauncher.OnBallFired += HandleBallFired;
    }

    private void OnDisable()
    {
        if (ballLauncher != null)
            ballLauncher.OnBallFired -= HandleBallFired;
    }

    private void FixedUpdate()
    {
        switch (currentState)
        {
            case BallPhysicsState.Idle:
                break;

            case BallPhysicsState.InFlight:
                ApplyAerodynamicDrag();
                ApplyMagnusEffect();
                break;

            case BallPhysicsState.OnGround:
                ApplyGroundFriction();
                break;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        // Yalnızca zemin temasında state değiştir; kaleci/direk/baraj temasında
        // top hâlâ uçuştadır ve Magnus devam etmelidir.
        bool isGround = collision.gameObject.name == "Pitch";

        if (currentState == BallPhysicsState.InFlight && isGround)
        {
            currentState = BallPhysicsState.OnGround;
            KillBounceIfNeeded();
            OnGroundContact?.Invoke();
        }
        else if (currentState == BallPhysicsState.OnGround && isGround)
        {
            KillBounceIfNeeded();
        }
    }

    // ─── Public API ──────────────────────────────────────────

    /// <summary>BallResetter tarafından çağrılır. State'i Idle'a döndürür.</summary>
    public void ResetState()
    {
        currentState = BallPhysicsState.Idle;
        rb.linearDamping  = 0f;
        rb.angularDamping = 0f;
    }

    // ─── Event Handlers ──────────────────────────────────────

    private void HandleBallFired()
    {
        currentState = BallPhysicsState.InFlight;
    }

    // ─── Air Physics ─────────────────────────────────────────

    /// <summary>
    /// Aerodinamik hava direnci: F_drag = -k * |v|² * v_hat
    /// </summary>
    private void ApplyAerodynamicDrag()
    {
        Vector3 velocity = rb.linearVelocity;
        float speed = velocity.magnitude;

        if (speed < 0.01f) return;

        Vector3 dragAcceleration = -airDragCoefficient * speed * velocity;
        rb.linearVelocity += dragAcceleration * Time.fixedDeltaTime;
    }

    /// <summary>
    /// Magnus etkisi: F_magnus = Cm * (ω × v)
    /// Topun dönüş yönüne göre havada eğrilmesini sağlar (falso).
    /// </summary>
    private void ApplyMagnusEffect()
    {
        Vector3 angularVel = rb.angularVelocity;

        if (angularVel.sqrMagnitude < 0.001f) return;

        Vector3 magnusForce = magnusCoefficient * Vector3.Cross(angularVel, rb.linearVelocity);
        Vector3 magnusAcceleration = magnusForce / rb.mass;
        rb.linearVelocity += magnusAcceleration * Time.fixedDeltaTime;
    }

    // ─── Ground Physics ──────────────────────────────────────

    /// <summary>
    /// Zemin sürtünmesi: v_new = v_old * (1 - μ * dt)
    /// </summary>
    private void ApplyGroundFriction()
    {
        Vector3 velocity = rb.linearVelocity;

        Vector3 horizontalVel = new Vector3(velocity.x, 0f, velocity.z);
        float frictionFactor = 1f - (groundFriction * Time.fixedDeltaTime);
        frictionFactor = Mathf.Max(frictionFactor, 0f);

        rb.linearVelocity = new Vector3(
            horizontalVel.x * frictionFactor,
            velocity.y,
            horizontalVel.z * frictionFactor
        );
    }

    /// <summary>
    /// Sekme sönümleme (Bounce Kill):
    /// Dikey hız çok düşükse sıfırlanır — top titremek yerine yere oturur.
    /// </summary>
    private void KillBounceIfNeeded()
    {
        Vector3 velocity = rb.linearVelocity;

        if (Mathf.Abs(velocity.y) < bounceKillThreshold)
        {
            rb.linearVelocity = new Vector3(velocity.x, 0f, velocity.z);
        }
    }
}
