using UnityEngine;

/// <summary>
/// Şut verisini taşıyan saf veri sınıfı (POCO — Plain Old C# Object).
/// MonoBehaviour değildir; yalnızca veri tutulur, mantık eklenmez.
/// </summary>
[System.Serializable]
public class ShotData
{
    // ─── Properties ──────────────────────────────────────────

    /// <summary>
    /// Topun hareket edeceği normalize edilmiş yön vektörü.
    /// Constructor'da otomatik olarak normalize edilir.
    /// </summary>
    public Vector3 Direction { get; }

    /// <summary>
    /// Vuruş gücü. 0 (sıfır) ile 1 (maksimum) arasındadır.
    /// </summary>
    public float Power { get; }

    /// <summary>
    /// Falso miktarı. -1 (tam sol falso) ile +1 (tam sağ falso) arasındadır.
    /// </summary>
    public float Curve { get; }

    /// <summary>
    /// Nişan alınan dünya-uzayı hedef noktası (kale düzlemi üzerinde).
    /// HasTarget false ise anlamsızdır.
    /// </summary>
    public Vector3 TargetPoint { get; }

    /// <summary>
    /// TargetPoint geçerli mi? (Nişangâh tabanlı şut / eski yön tabanlı şut ayrımı)
    /// </summary>
    public bool HasTarget { get; }

    // ─── Constructors ────────────────────────────────────────

    /// <summary>Eski (yön tabanlı) constructor — geriye dönük uyumluluk.</summary>
    public ShotData(Vector3 direction, float power)
    {
        Direction   = direction.normalized;
        Power       = Mathf.Clamp01(power);
        Curve       = 0f;
        TargetPoint = Vector3.zero;
        HasTarget   = false;
    }

    /// <summary>Nişangâh tabanlı tam şut verisi.</summary>
    public ShotData(Vector3 direction, float power, float curve, Vector3 targetPoint)
    {
        Direction   = direction.normalized;
        Power       = Mathf.Clamp01(power);
        Curve       = Mathf.Clamp(curve, -1f, 1f);
        TargetPoint = targetPoint;
        HasTarget   = true;
    }

    // ─── Debug ───────────────────────────────────────────────

    public override string ToString()
        => $"ShotData [Dir: {Direction:F2}, Power: {Power:F2}, Curve: {Curve:F2}, Target: {TargetPoint:F2}]";
}
