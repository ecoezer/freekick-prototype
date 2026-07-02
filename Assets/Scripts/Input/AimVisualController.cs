using UnityEngine;

/// <summary>
/// Şarj sırasında topun izleyeceği yayı (trajectory) LineRenderer ile önizler.
/// BallLauncher'ın balistik çözümünü ve BallPhysicsController'ın drag/Magnus
/// katsayılarını kullanır — önizleme gerçek uçuşla tutarlıdır.
/// Yayın tamamı değil, ilk bölümü gösterilir (oyun tamamen kolaylaşmasın).
/// </summary>
[RequireComponent(typeof(LineRenderer))]
public class AimVisualController : MonoBehaviour
{
    // ─── Inspector ───────────────────────────────────────────

    [Header("References")]
    [SerializeField] private ShotInput     shotInput;
    [SerializeField] private Transform     ballTransform;
    [SerializeField] private BallLauncher  ballLauncher;
    [SerializeField] private BallPhysicsController ballPhysics;
    [SerializeField] private MonoBehaviour targetProviderObject;

    [Header("Preview Settings")]
    [Tooltip("Yayın ne kadarı gösterilir (0.65 = %65'i).")]
    [SerializeField, Range(0.2f, 1f)] private float previewFraction = 0.65f;

    [SerializeField, Range(10, 60)] private int   maxPoints = 36;
    [SerializeField] private float simulationStep = 0.05f;

    [Header("Juice")]
    [Tooltip("Max güçteki titreme (shake) miktarı.")]
    [SerializeField] private float maxPowerShakeAmount = 0.06f;

    // ─── Private State ───────────────────────────────────────

    private LineRenderer lineRenderer;
    private ITargetPointProvider targetProvider;
    private bool isAiming;

    // ─── Unity Lifecycle ─────────────────────────────────────

    private void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.enabled       = false;
        lineRenderer.useWorldSpace = true;

        targetProvider = targetProviderObject as ITargetPointProvider;
        if (targetProvider == null)
            Debug.LogError($"[AimVisualController] '{targetProviderObject?.name}' ITargetPointProvider implement etmiyor!", this);
    }

    private void OnEnable()
    {
        if (shotInput != null) shotInput.OnPowerChanged += HandlePowerChanged;
    }

    private void OnDisable()
    {
        if (shotInput != null) shotInput.OnPowerChanged -= HandlePowerChanged;
    }

    // ─── Event Handlers ──────────────────────────────────────

    private void HandlePowerChanged(float power)
    {
        if (power > 0.01f)
        {
            if (!isAiming)
            {
                isAiming = true;
                lineRenderer.enabled = true;
            }
            UpdateArc(power);
        }
        else if (isAiming)
        {
            isAiming = false;
            lineRenderer.enabled = false;
        }
    }

    // ─── Private Methods ─────────────────────────────────────

    private void UpdateArc(float power)
    {
        if (targetProvider == null || ballTransform == null || ballLauncher == null) return;

        Vector3 from   = ballTransform.position;
        Vector3 target = targetProvider.GetTargetPoint();
        float   curve  = shotInput.CurrentCurve;

        Vector3 velocity = ballLauncher.ComputeLaunchVelocity(from, target, power, curve, out Vector3 spin);

        // Uçuş süresi tahmini → önizlenecek süre.
        Vector3 flat = target - from; flat.y = 0f;
        float flightTime  = flat.magnitude / Mathf.Max(new Vector3(velocity.x, 0, velocity.z).magnitude, 1f);
        float previewTime = flightTime * previewFraction;

        float drag   = ballPhysics != null ? ballPhysics.AirDragCoefficient : 0.01f;
        float magnus = ballPhysics != null ? ballPhysics.MagnusCoefficient  : 0.012f;
        float mass   = 0.45f;

        int points = Mathf.Min(maxPoints, Mathf.Max(4, Mathf.CeilToInt(previewTime / simulationStep)));
        lineRenderer.positionCount = points;

        Vector3 p = from + Vector3.up * 0.05f;
        Vector3 v = velocity;

        for (int i = 0; i < points; i++)
        {
            lineRenderer.SetPosition(i, p);

            // Gerçek fizikle aynı model: yerçekimi + v² drag + Magnus.
            Vector3 accel = Physics.gravity
                          - drag * v.magnitude * v
                          + (magnus * Vector3.Cross(spin, v)) / mass;

            v += accel * simulationStep;
            p += v * simulationStep;

            if (p.y < 0.03f) { lineRenderer.positionCount = i + 1; break; }
        }

        // Max güçte hafif titreme (juice).
        if (power >= 0.99f && lineRenderer.positionCount > 1)
        {
            int last = lineRenderer.positionCount - 1;
            lineRenderer.SetPosition(last,
                lineRenderer.GetPosition(last) + Random.insideUnitSphere * maxPowerShakeAmount);
        }

        UpdateColor(power);
    }

    private void UpdateColor(float power)
    {
        Color startColor, endColor;

        if (power < 0.5f)
        {
            float t = power / 0.5f;
            endColor   = Color.Lerp(Color.white, Color.yellow, t);
            startColor = Color.Lerp(Color.white, Color.yellow, t * 0.5f);
        }
        else
        {
            float t = (power - 0.5f) / 0.5f;
            endColor   = Color.Lerp(Color.yellow, Color.red, t);
            startColor = Color.Lerp(Color.yellow, Color.red, t * 0.5f);
        }

        lineRenderer.startColor = startColor;
        lineRenderer.endColor   = endColor;
    }
}
