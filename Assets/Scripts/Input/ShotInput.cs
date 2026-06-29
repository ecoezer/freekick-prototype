using UnityEngine;
using System;

/// <summary>
/// Oyuncu fare/dokunmatik girişini dinler ve ShotData üretir.
///
/// Sorumluluk: YALNIZCA input okuma ve ShotData üretme.
///   - Fizik hesabı yapmaz.
///   - Oyun durumunu bilmez.
///   - Yön hesabını IDirectionProvider'a delege eder (DIP + OCP).
///
/// Kullanım:
///   Sol fare tuşuna basılı tutarak güç yüklenir.
///   Bırakıldığında OnShotReady eventi ile ShotData iletilir.
/// </summary>
public class ShotInput : MonoBehaviour
{
    // ─── Inspector ───────────────────────────────────────────

    [Header("Charge Settings")]
    [Tooltip("Saniyede ne kadar güç yükleneceği. Örn: 0.8 = ~1.25 saniyede maks güce ulaşır.")]
    [SerializeField, Range(0.2f, 3f)] private float chargeSpeed = 0.8f;

    [Header("References")]
    [Tooltip("Topu temsil eden Transform. Yön hesabı için başlangıç noktasını belirler.")]
    [SerializeField] private Transform ballTransform;

    [Tooltip("IDirectionProvider implementasyonu. Inspector'dan GoalDirectionProvider atanır.")]
    [SerializeField] private MonoBehaviour directionProviderObject;

    // ─── Events ──────────────────────────────────────────────

    /// <summary>
    /// Şut verisi hazır olduğunda (mouse bırakıldığında) tetiklenir.
    /// ShotController bu eventi dinler.
    /// </summary>
    public event Action<ShotData> OnShotReady;

    /// <summary>
    /// Güç dolum değeri değiştiğinde tetiklenir. Değer aralığı: [0, 1].
    /// PowerBar UI bu eventi dinleyebilir.
    /// </summary>
    public event Action<float> OnPowerChanged;

    // ─── Private State ───────────────────────────────────────

    private IDirectionProvider directionProvider;
    private float              currentPower;
    private bool               isCharging;
    private bool               isInputEnabled = true;

    // ─── Unity Lifecycle ─────────────────────────────────────

    private void Awake()
    {
        // Inspector'dan atanan MonoBehaviour'u interface olarak çek.
        // Bu sayede herhangi bir IDirectionProvider implementasyonu çalışır.
        directionProvider = directionProviderObject as IDirectionProvider;

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

        // Input kapatıldığında aktif yükleme de kesilir ve UI sıfırlanır.
        if (!enabled)
        {
            isCharging   = false;
            currentPower = 0f;
            OnPowerChanged?.Invoke(0f);
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
    }

    private void ContinueCharge()
    {
        currentPower = Mathf.Clamp01(currentPower + chargeSpeed * Time.deltaTime);
        OnPowerChanged?.Invoke(currentPower);
    }

    private void ReleaseShot()
    {
        isCharging = false;

        Vector3  direction = directionProvider.GetDirection(ballTransform.position);
        ShotData data      = new ShotData(direction, currentPower);

        // Gücü sıfırla ve UI'yı bilgilendir.
        currentPower = 0f;
        OnPowerChanged?.Invoke(0f);

        // ShotController'ı bilgilendir.
        OnShotReady?.Invoke(data);
    }
}
