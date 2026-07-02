using UnityEngine;
using System;

/// <summary>
/// Oyuncu fare/klavye girişini dinler ve ShotData üretir.
///
/// Sorumluluk: YALNIZCA input okuma ve ShotData üretme.
///   - Fizik hesabı yapmaz, oyun durumunu bilmez.
///   - Yön/hedef hesabını IDirectionProvider'a delege eder (DIP + OCP).
///
/// Kullanım:
///   Fare        → nişan alma (AimTargetProvider hedefi hesaplar)
///   Sol tık     → basılı tut: güç yüklenir, bırak: şut
///   A / D (←/→) → şarj sırasında falso ayarı
/// </summary>
public class ShotInput : MonoBehaviour
{
    // ─── Inspector ───────────────────────────────────────────

    [Header("Charge Settings")]
    [Tooltip("Saniyede ne kadar güç yükleneceği. Örn: 0.9 ≈ 1.1 saniyede maks güç.")]
    [SerializeField, Range(0.2f, 3f)] private float chargeSpeed = 0.9f;

    [Tooltip("Bu gücün altındaki bırakışlar şut sayılmaz (yanlışlıkla tık koruması).")]
    [SerializeField, Range(0f, 0.3f)] private float minPowerToFire = 0.07f;

    [Header("Curve Settings")]
    [Tooltip("A/D basılıyken falsonun saniyedeki değişim hızı.")]
    [SerializeField, Range(0.5f, 5f)] private float curveChangeSpeed = 1.6f;

    [Header("References")]
    [Tooltip("Topu temsil eden Transform. Yön hesabı için başlangıç noktasını belirler.")]
    [SerializeField] private Transform ballTransform;

    [Tooltip("IDirectionProvider implementasyonu (AimTargetProvider atanır).")]
    [SerializeField] private MonoBehaviour directionProviderObject;

    // ─── Events ──────────────────────────────────────────────

    /// <summary>Şut verisi hazır olduğunda (mouse bırakıldığında) tetiklenir.</summary>
    public event Action<ShotData> OnShotReady;

    /// <summary>Güç dolum değeri değiştiğinde tetiklenir. Aralık: [0, 1].</summary>
    public event Action<float> OnPowerChanged;

    /// <summary>Falso değeri değiştiğinde tetiklenir. Aralık: [-1, 1].</summary>
    public event Action<float> OnCurveChanged;

    // ─── Public Read-Only State ──────────────────────────────

    /// <summary>Anlık güç (yay önizlemesi için).</summary>
    public float CurrentPower => currentPower;

    /// <summary>Anlık falso (yay önizlemesi için).</summary>
    public float CurrentCurve => currentCurve;

    /// <summary>Şu anda güç yükleniyor mu?</summary>
    public bool IsCharging => isCharging;

    // ─── Private State ───────────────────────────────────────

    private IDirectionProvider directionProvider;
    private ITargetPointProvider targetProvider;
    private float currentPower;
    private float currentCurve;
    private bool  isCharging;
    private bool  isInputEnabled = true;

    // ─── Unity Lifecycle ─────────────────────────────────────

    private void Awake()
    {
        directionProvider = directionProviderObject as IDirectionProvider;
        targetProvider    = directionProviderObject as ITargetPointProvider;

        if (directionProvider == null)
            Debug.LogError(
                $"[ShotInput] '{directionProviderObject?.name}' IDirectionProvider implement etmiyor!", this);
    }

    private void Update()
    {
        if (!isInputEnabled) return;

        HandleCharging();
    }

    // ─── Public API ──────────────────────────────────────────

    /// <summary>
    /// Input'u etkinleştirir veya devre dışı bırakır.
    /// ShotController şut atıldıktan sonra kapatır, top sıfırlandığında açar.
    /// </summary>
    public void SetEnabled(bool enabled)
    {
        isInputEnabled = enabled;

        if (!enabled)
        {
            isCharging   = false;
            currentPower = 0f;
            currentCurve = 0f;
            OnPowerChanged?.Invoke(0f);
            OnCurveChanged?.Invoke(0f);
        }
    }

    // ─── Private Methods ─────────────────────────────────────

    private void HandleCharging()
    {
        if (Input.GetButtonDown("Fire1")) BeginCharge();
        if (Input.GetButton("Fire1") && isCharging) ContinueCharge();
        if (Input.GetButtonUp("Fire1") && isCharging) ReleaseShot();
    }

    private void BeginCharge()
    {
        isCharging   = true;
        currentPower = 0f;
        currentCurve = 0f;
        OnCurveChanged?.Invoke(0f);
    }

    private void ContinueCharge()
    {
        currentPower = Mathf.Clamp01(currentPower + chargeSpeed * Time.deltaTime);
        OnPowerChanged?.Invoke(currentPower);

        HandleCurveInput();
    }

    private void HandleCurveInput()
    {
        float dir = 0f;
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))  dir -= 1f;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) dir += 1f;

        if (dir != 0f)
        {
            currentCurve = Mathf.Clamp(currentCurve + dir * curveChangeSpeed * Time.deltaTime, -1f, 1f);
            OnCurveChanged?.Invoke(currentCurve);
        }
    }

    private void ReleaseShot()
    {
        isCharging = false;

        // Kazara tık koruması: neredeyse hiç güç yüklenmediyse şut iptal.
        if (currentPower < minPowerToFire)
        {
            currentPower = 0f;
            currentCurve = 0f;
            OnPowerChanged?.Invoke(0f);
            OnCurveChanged?.Invoke(0f);
            return;
        }

        Vector3 direction = directionProvider.GetDirection(ballTransform.position);

        ShotData data = targetProvider != null
            ? new ShotData(direction, currentPower, currentCurve, targetProvider.GetTargetPoint())
            : new ShotData(direction, currentPower);

        currentPower = 0f;
        currentCurve = 0f;
        OnPowerChanged?.Invoke(0f);
        OnCurveChanged?.Invoke(0f);

        OnShotReady?.Invoke(data);
    }
}
