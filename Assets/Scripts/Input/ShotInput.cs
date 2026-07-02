using UnityEngine;
using System;

/// <summary>
/// Oyuncu fare girişini dinler ve ShotData üretir.
///
/// Sorumluluk: YALNIZCA input okuma ve ShotData üretme.
///   - Fizik hesabı yapmaz, oyun durumunu bilmez.
///   - Yön/hedef hesabını IDirectionProvider'a delege eder (DIP + OCP).
///
/// Kullanım (iki fazlı basılı tutma):
///   Faz 1 — GÜÇ : Sol tık basılı tutulur, güç 0'dan %100'e dolar.
///   Faz 2 — FALSO: Güç dolduktan sonra basılı tutmaya devam edilirse falso
///                  ibresi önce TAM SOLA, oraya ulaşınca TAM SAĞA doğru salınır
///                  (ping-pong). Bırakıldığı andaki değer şuta uygulanır.
///   Bırak → şut. Falso istemiyorsan güç dolar dolmaz bırak (falso 0).
/// </summary>
public class ShotInput : MonoBehaviour
{
    // ─── Inspector ───────────────────────────────────────────

    [Header("Charge Settings")]
    [Tooltip("Saniyede ne kadar güç yükleneceği. Örn: 0.9 ≈ 1.1 saniyede maks güç.")]
    [SerializeField, Range(0.2f, 3f)] private float chargeSpeed = 0.9f;

    [Tooltip("Bu gücün altındaki bırakışlar şut sayılmaz (yanlışlıkla tık koruması).")]
    [SerializeField, Range(0f, 0.3f)] private float minPowerToFire = 0.07f;

    [Header("Curve Sweep Settings")]
    [Tooltip("Falso ibresinin salınım hızı (birim/sn). 1.2 ≈ tam sol→tam sağ 1.7 sn.")]
    [SerializeField, Range(0.3f, 4f)] private float curveSweepSpeed = 1.2f;

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
    public bool IsCharging => phase != ChargePhase.Idle;

    // ─── Private State ───────────────────────────────────────

    private enum ChargePhase
    {
        Idle,   // Basılı değil.
        Power,  // Güç doluyor.
        Curve   // Güç doldu, falso ibresi salınıyor.
    }

    private IDirectionProvider   directionProvider;
    private ITargetPointProvider targetProvider;
    private ChargePhase phase = ChargePhase.Idle;
    private float currentPower;
    private float currentCurve;
    private float curveDirection = -1f; // Önce sola.
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

        if (Input.GetButtonDown("Fire1")) BeginCharge();
        if (Input.GetButton("Fire1") && phase != ChargePhase.Idle) ContinueCharge();
        if (Input.GetButtonUp("Fire1") && phase != ChargePhase.Idle) ReleaseShot();
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
            ResetChargeState();
        }
    }

    // ─── Private Methods ─────────────────────────────────────

    private void BeginCharge()
    {
        phase          = ChargePhase.Power;
        currentPower   = 0f;
        currentCurve   = 0f;
        curveDirection = -1f; // Falso salınımı her şutta önce sola başlar.
        OnCurveChanged?.Invoke(0f);
    }

    private void ContinueCharge()
    {
        if (phase == ChargePhase.Power)
        {
            currentPower = Mathf.Clamp01(currentPower + chargeSpeed * Time.deltaTime);
            OnPowerChanged?.Invoke(currentPower);

            // Güç doldu → falso salınım fazına geç.
            if (currentPower >= 1f)
                phase = ChargePhase.Curve;
        }
        else // ChargePhase.Curve
        {
            currentCurve += curveDirection * curveSweepSpeed * Time.deltaTime;

            // Uçlarda yön değiştir: tam sola ulaşınca sağa, tam sağa ulaşınca sola.
            if (currentCurve <= -1f) { currentCurve = -1f; curveDirection = 1f; }
            else if (currentCurve >= 1f) { currentCurve = 1f; curveDirection = -1f; }

            OnCurveChanged?.Invoke(currentCurve);
        }
    }

    private void ReleaseShot()
    {
        // Kazara tık koruması: neredeyse hiç güç yüklenmediyse şut iptal.
        if (currentPower < minPowerToFire)
        {
            ResetChargeState();
            return;
        }

        Vector3 direction = directionProvider.GetDirection(ballTransform.position);
        float   power     = currentPower;
        float   curve     = currentCurve; // Bırakıldığı andaki falso aynen uygulanır.

        ShotData data = targetProvider != null
            ? new ShotData(direction, power, curve, targetProvider.GetTargetPoint())
            : new ShotData(direction, power);

        ResetChargeState();

        OnShotReady?.Invoke(data);
    }

    private void ResetChargeState()
    {
        phase          = ChargePhase.Idle;
        currentPower   = 0f;
        currentCurve   = 0f;
        curveDirection = -1f;
        OnPowerChanged?.Invoke(0f);
        OnCurveChanged?.Invoke(0f);
    }
}
