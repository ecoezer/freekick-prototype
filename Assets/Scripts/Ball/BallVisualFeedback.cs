using UnityEngine;

/// <summary>
/// Şarj olurken topun ezilmesi (Squash) görsel efektini yönetir.
/// </summary>
public class BallVisualFeedback : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ShotInput shotInput;
    
    [Header("Squash Settings")]
    [SerializeField] private float minScaleY = 0.8f;
    [SerializeField] private float maxScaleXZ = 1.1f;
    [SerializeField] private float lerpSpeed = 15f;
    
    private Vector3 originalScale;
    private Vector3 targetScale;

    private void Awake()
    {
        originalScale = transform.localScale;
        targetScale = originalScale;
    }

    private void OnEnable()
    {
        if (shotInput != null)
        {
            shotInput.OnPowerChanged += HandlePowerChanged;
        }
    }

    private void OnDisable()
    {
        if (shotInput != null)
        {
            shotInput.OnPowerChanged -= HandlePowerChanged;
        }
    }

    private void Update()
    {
        // Smoothly interpolate current scale towards target scale
        if (transform.localScale != targetScale)
        {
            transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * lerpSpeed);
        }
    }

    private void HandlePowerChanged(float power)
    {
        if (power <= 0.01f)
        {
            targetScale = originalScale;
        }
        else
        {
            float targetY = Mathf.Lerp(originalScale.y, originalScale.y * minScaleY, power);
            float targetXZ = Mathf.Lerp(originalScale.x, originalScale.x * maxScaleXZ, power);
            
            targetScale = new Vector3(targetXZ, targetY, targetXZ);
        }
    }
}
