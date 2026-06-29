using UnityEngine;

/// <summary>
/// IDirectionProvider'ın prototype implementasyonu.
/// Her zaman sabit kale merkezine doğru yön döndürür.
///
/// Sorumluluk: YALNIZCA yön hesabı — başka hiçbir oyun mantığı yoktur.
///
/// Kullanım: Ball GameObject'e veya ayrı bir boş GameObject'e eklenir.
/// Inspector'dan goalCenter Transform'u atanır.
/// </summary>
public class GoalDirectionProvider : MonoBehaviour, IDirectionProvider
{
    // ─── Inspector ───────────────────────────────────────────

    [Header("References")]
    [Tooltip("Yönün hesaplanacağı hedef Transform (kale merkezi).")]
    [SerializeField] private Transform goalCenter;

    // ─── IDirectionProvider ──────────────────────────────────

    /// <summary>
    /// Verilen 'fromPosition' noktasından kale merkezine doğru
    /// normalize edilmiş yön vektörü döndürür.
    /// </summary>
    public Vector3 GetDirection(Vector3 fromPosition)
    {
        return (goalCenter.position - fromPosition).normalized;
    }
}
