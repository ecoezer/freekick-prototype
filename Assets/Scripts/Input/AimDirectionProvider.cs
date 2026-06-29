using UnityEngine;

/// <summary>
/// Oyuncunun fare veya dokunmatik ekran aracılığıyla ekranda tıkladığı noktayı
/// 3D dünyaya çevirerek (Raycast ile) şut yönünü hesaplayan sınıf.
/// </summary>
public class AimDirectionProvider : MonoBehaviour, IDirectionProvider
{
    [Header("Settings")]
    [Tooltip("Yön hesaplaması için raycast'in çarpacağı düzlemin Y eksenindeki yüksekliği (genelde 0 veya yer seviyesi).")]
    [SerializeField] private float groundPlaneY = 0f;

    private Camera mainCamera;

    private void Start()
    {
        mainCamera = Camera.main;
    }

    /// <summary>
    /// Farenin anlık ekran konumunu baz alarak, toptan hedefe doğru olan yönü döndürür.
    /// </summary>
    public Vector3 GetDirection(Vector3 fromPosition)
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        // Farenin bulunduğu pikselden kameraya doğru bir ışın oluştur
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        
        // Zemin düzlemini oluştur (Normali yukarı bakan, groundPlaneY yüksekliğinde bir düzlem)
        Plane groundPlane = new Plane(Vector3.up, new Vector3(0, groundPlaneY, 0));

        // Işın ile düzlem kesişiyorsa
        if (groundPlane.Raycast(ray, out float enter))
        {
            // Kesişme noktasını al
            Vector3 hitPoint = ray.GetPoint(enter);
            
            // Y eksenindeki farkı yok say ki top yukarı doğru saçmalamasın, sadece düzlemde yön bulalım
            hitPoint.y = fromPosition.y;

            // Toptan kesişim noktasına giden vektörü hesapla ve normalize et
            Vector3 direction = (hitPoint - fromPosition).normalized;
            return direction;
        }

        // Eğer bir şekilde kesişmezse (kamera ufka bakıyorsa vs.) ileri yönü dön
        return Vector3.forward;
    }
}
