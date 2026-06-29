using UnityEngine;

/// <summary>
/// Şut verisini taşıyan saf veri sınıfı (POCO — Plain Old C# Object).
/// MonoBehaviour değildir; yalnızca veri tutulur, mantık eklenmez.
///
/// Genişletilebilirlik:
///   İleride Curve (float) ve Spin (Vector3) alanları yeni
///   optional constructor parametresi olarak eklenebilir.
///   Mevcut alanlar ve imza değişmez.
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
    /// Constructor'da Clamp01 ile sınırlandırılır.
    /// </summary>
    public float Power { get; }

    // ─── Constructor ─────────────────────────────────────────

    /// <summary>
    /// Yeni bir ShotData örneği oluşturur.
    /// </summary>
    /// <param name="direction">Vuruş yönü — normalize edilmemiş olabilir, içeride normalize edilir.</param>
    /// <param name="power">Vuruş gücü — 0 ile 1 arasında clamp edilir.</param>
    public ShotData(Vector3 direction, float power)
    {
        Direction = direction.normalized;
        Power     = Mathf.Clamp01(power);
    }

    // ─── Debug ───────────────────────────────────────────────

    public override string ToString()
        => $"ShotData [Direction: {Direction:F2}, Power: {Power:F2}]";
}
