using UnityEngine;

/// <summary>
/// Fare pozisyonunu KALE DÜZLEMİNE (dikey düzlem, kale çizgisi üzerinde)
/// raycast ederek nişan noktası hesaplar.
///
/// Eski AimDirectionProvider zemin düzlemine nişan alıyordu (yükseklik seçilemiyordu).
/// Bu provider ile oyuncu üst köşe / alt köşe ayrımı yapabilir.
/// </summary>
public class AimTargetProvider : MonoBehaviour, IDirectionProvider, ITargetPointProvider
{
    // ─── Inspector ───────────────────────────────────────────

    [Header("References")]
    [Tooltip("Kale merkezi (düzlemin geçtiği nokta).")]
    [SerializeField] private Transform goalCenter;

    [Header("Aim Bounds (kale merkezine göre)")]
    [Tooltip("Yatayda nişan alınabilecek maksimum ofset (m). Kale 3.66 m yarı-genişliktedir.")]
    [SerializeField, Range(3f, 10f)] private float horizontalLimit = 5.2f;

    [Tooltip("Nişanın minimum yüksekliği (m).")]
    [SerializeField, Range(0f, 1f)] private float minHeight = 0.12f;

    [Tooltip("Nişanın maksimum yüksekliği (m). Üst direk 2.44 m'dir.")]
    [SerializeField, Range(2f, 6f)] private float maxHeight = 3.4f;

    // ─── Private State ───────────────────────────────────────

    private Camera  mainCamera;
    private Vector3 lastValidPoint;

    // ─── Unity Lifecycle ─────────────────────────────────────

    private void Start()
    {
        mainCamera     = Camera.main;
        lastValidPoint = goalCenter != null
            ? goalCenter.position + Vector3.up * 1.2f
            : Vector3.forward * 20f;
    }

    // ─── ITargetPointProvider ────────────────────────────────

    /// <summary>
    /// Farenin baktığı, kale düzlemi üzerindeki (sınırlanmış) nokta.
    /// </summary>
    public Vector3 GetTargetPoint()
    {
        if (mainCamera == null) mainCamera = Camera.main;
        if (mainCamera == null || goalCenter == null) return lastValidPoint;

        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);

        // Kale düzlemi: normali kameraya (topa) doğru bakan dikey düzlem.
        Vector3 planeNormal = -goalCenter.forward;
        if (Vector3.Dot(planeNormal, ray.direction) >= 0f) planeNormal = -planeNormal;

        Plane goalPlane = new Plane(planeNormal, goalCenter.position);

        if (goalPlane.Raycast(ray, out float enter))
        {
            Vector3 hit = ray.GetPoint(enter);

            // Kale merkezine göre lokal sınırlar (yatay ofset + yükseklik).
            Vector3 right  = goalCenter.right;
            float   xLocal = Vector3.Dot(hit - goalCenter.position, right);
            xLocal = Mathf.Clamp(xLocal, -horizontalLimit, horizontalLimit);

            float y = Mathf.Clamp(hit.y, minHeight, maxHeight);

            lastValidPoint = goalCenter.position + right * xLocal + Vector3.up * (y - goalCenter.position.y);
            lastValidPoint.y = y;
        }

        return lastValidPoint;
    }

    // ─── IDirectionProvider ──────────────────────────────────

    /// <summary>Toptan nişan noktasına normalize yön (geriye dönük uyumluluk).</summary>
    public Vector3 GetDirection(Vector3 fromPosition)
    {
        return (GetTargetPoint() - fromPosition).normalized;
    }
}
