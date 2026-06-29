using UnityEngine;

/// <summary>
/// Topun havadaki ve yerdeki fiziksel sürtünme (drag) değerlerini yönetir.
/// Zemin temasını algılar ve duruma göre Rigidbody'yi günceller.
///
/// Sorumluluk (SRP): 
///   - Yalnızca fizik (sürtünme) yönetimi ve çarpışma tespiti.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class BallPhysicsController : MonoBehaviour
{
    // ─── Inspector ───────────────────────────────────────────

    [Header("References")]
    [Tooltip("Top fırlatıldığında haberdar olmak için referans.")]
    [SerializeField] private BallLauncher ballLauncher;

    [Header("Physics Settings - Air (InFlight)")]
    [Tooltip("Havadayken uygulanacak doğrusal sürtünme (Hava direnci).")]
    [SerializeField] private float airDrag = 0.5f;
    [Tooltip("Havadayken uygulanacak açısal sürtünme.")]
    [SerializeField] private float airAngularDrag = 0.05f;

    [Header("Physics Settings - Ground (Rolling)")]
    [Tooltip("Yere temas ettiğinde uygulanacak doğrusal sürtünme (Çim sürtünmesi).")]
    [SerializeField] private float groundDrag = 2.5f;
    [Tooltip("Yere temas ettiğinde uygulanacak açısal sürtünme.")]
    [SerializeField] private float groundAngularDrag = 2.5f;
    [Tooltip("Zemin objelerinin etiketi (Zemin algılaması için).")]
    [SerializeField] private string groundTag = "Untagged";

    // ─── Private State ───────────────────────────────────────

    private Rigidbody rb;
    private bool isInFlight;

    // ─── Unity Lifecycle ─────────────────────────────────────

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        SetIdlePhysics();
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

    private void OnCollisionEnter(Collision collision)
    {
        // Eğer top havadaysa ve zemine (veya başka bir yüzeye) çarptıysa Rolling (Yerde) duruma geç
        if (isInFlight && (collision.gameObject.CompareTag(groundTag) || collision.gameObject.name == "Pitch"))
        {
            SetRollingPhysics();
        }
    }

    // ─── Event Handlers ──────────────────────────────────────

    private void HandleBallFired()
    {
        SetInFlightPhysics();
    }

    // ─── Private Methods ─────────────────────────────────────

    /// <summary>
    /// Top fırlatılmayı beklerken (Idle) sürtünme durumu.
    /// Zemin üzerindeymiş gibi yüksek drag ile bekletilir.
    /// </summary>
    private void SetIdlePhysics()
    {
        isInFlight = false;
        rb.linearDamping = groundDrag;   // linearVelocity sürtünmesi (Unity 6.x linearDamping olarak adlandırılabilir, ancak drag da kullanılabilir. Scriptte standart kullanacağız).
        rb.drag = groundDrag; 
        rb.angularDrag = groundAngularDrag;
    }

    /// <summary>
    /// Şut çekildiğinde (Havadayken) uygulanacak fizik ayarları.
    /// Düşük drag, Magnus ve falso (ileride) için uygun zemin hazırlar.
    /// </summary>
    private void SetInFlightPhysics()
    {
        isInFlight = true;
        
        // Rigidbody ayarları
        rb.drag = airDrag;
        rb.angularDrag = airAngularDrag;
    }

    /// <summary>
    /// Zeminle temas sonrasında (Yuvarlanma) devreye girecek fizik ayarları.
    /// Topun doğal şekilde yavaşlamasını sağlar.
    /// </summary>
    private void SetRollingPhysics()
    {
        isInFlight = false;
        
        // Yüksek zemin sürtünmesi
        rb.drag = groundDrag;
        rb.angularDrag = groundAngularDrag;
    }
}
