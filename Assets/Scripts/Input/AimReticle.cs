using UnityEngine;

/// <summary>
/// Nişan alınan noktada, kale düzlemi üzerinde bir halka (reticle) çizer.
/// Top uçuştayken gizlenir.
/// </summary>
[RequireComponent(typeof(LineRenderer))]
public class AimReticle : MonoBehaviour
{
    // ─── Inspector ───────────────────────────────────────────

    [Header("References")]
    [SerializeField] private MonoBehaviour targetProviderObject;
    [SerializeField] private BallLauncher  ballLauncher;
    [SerializeField] private BallResetter  ballResetter;
    [SerializeField] private ShotInput     shotInput;

    [Header("Reticle")]
    [SerializeField, Range(0.1f, 1f)] private float radius = 0.28f;
    [SerializeField, Range(8, 64)]    private int   segments = 28;

    // ─── Private State ───────────────────────────────────────

    private LineRenderer lineRenderer;
    private ITargetPointProvider targetProvider;
    private bool isVisible = true;

    // ─── Unity Lifecycle ─────────────────────────────────────

    private void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.loop          = true;
        lineRenderer.positionCount = segments;
        lineRenderer.useWorldSpace = true;

        targetProvider = targetProviderObject as ITargetPointProvider;
        if (targetProvider == null)
            Debug.LogError($"[AimReticle] '{targetProviderObject?.name}' ITargetPointProvider implement etmiyor!", this);
    }

    private void OnEnable()
    {
        if (ballLauncher != null) ballLauncher.OnBallFired += Hide;
        if (ballResetter != null) ballResetter.OnBallReset += Show;
    }

    private void OnDisable()
    {
        if (ballLauncher != null) ballLauncher.OnBallFired -= Hide;
        if (ballResetter != null) ballResetter.OnBallReset -= Show;
    }

    private void Update()
    {
        if (!isVisible || targetProvider == null) return;

        DrawRing(targetProvider.GetTargetPoint());
        UpdateColor();
    }

    // ─── Private Methods ─────────────────────────────────────

    private void Hide() { isVisible = false; lineRenderer.enabled = false; }
    private void Show() { isVisible = true;  lineRenderer.enabled = true; }

    private void DrawRing(Vector3 center)
    {
        // Halkayı kameraya dönük (billboard) çiz — her açıdan okunur.
        Transform cam = Camera.main != null ? Camera.main.transform : null;
        Vector3 right = cam != null ? cam.right : Vector3.right;
        Vector3 up    = cam != null ? cam.up    : Vector3.up;

        for (int i = 0; i < segments; i++)
        {
            float a = (i / (float)segments) * Mathf.PI * 2f;
            Vector3 p = center + (right * Mathf.Cos(a) + up * Mathf.Sin(a)) * radius;
            lineRenderer.SetPosition(i, p);
        }
    }

    private void UpdateColor()
    {
        float power = shotInput != null ? shotInput.CurrentPower : 0f;
        Color c = power < 0.5f
            ? Color.Lerp(Color.white, Color.yellow, power * 2f)
            : Color.Lerp(Color.yellow, Color.red, (power - 0.5f) * 2f);

        lineRenderer.startColor = c;
        lineRenderer.endColor   = c;
    }
}
