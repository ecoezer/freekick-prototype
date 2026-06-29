using UnityEngine;

/// <summary>
/// Topun gideceği yönü LineRenderer kullanarak görselleştirir.
/// </summary>
[RequireComponent(typeof(LineRenderer))]
public class AimVisualController : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Yön hesabı için kullanılacak olan provider.")]
    [SerializeField] private MonoBehaviour directionProviderObject;
    
    [Tooltip("Topun merkezi (Çizginin başlangıç noktası)")]
    [SerializeField] private Transform ballTransform;

    [Header("Settings")]
    [Tooltip("Çizginin uzunluğu")]
    [SerializeField] private float lineLength = 5f;

    private LineRenderer lineRenderer;
    private IDirectionProvider directionProvider;
    private bool isAiming = false;

    private void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.enabled = false;

        directionProvider = directionProviderObject as IDirectionProvider;
        if (directionProvider == null)
        {
            Debug.LogError($"[AimVisualController] '{directionProviderObject?.name}' IDirectionProvider implement etmiyor!", this);
        }
    }

    private void Update()
    {
        // Yalnızca ekrana basılı tutulduğunda çizgiyi göster
        if (Input.GetButton("Fire1"))
        {
            if (!isAiming)
            {
                isAiming = true;
                lineRenderer.enabled = true;
            }

            UpdateLine();
        }
        else if (isAiming)
        {
            isAiming = false;
            lineRenderer.enabled = false;
        }
    }

    private void UpdateLine()
    {
        if (directionProvider == null || ballTransform == null) return;

        Vector3 startPos = ballTransform.position;
        // Topun biraz üstünden başlasın ki yerin içine girmesin
        startPos.y += 0.1f; 

        Vector3 direction = directionProvider.GetDirection(ballTransform.position);
        Vector3 endPos = startPos + (direction * lineLength);

        lineRenderer.SetPosition(0, startPos);
        lineRenderer.SetPosition(1, endPos);
    }
}
