using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Oyun arayüzü: skor paneli, ŞUT GÜCÜ barı, FALSO GÜCÜ barı (merkezden dolan),
/// sonuç mesajı ve yardım metni.
/// Tüm UI hiyerarşisi Awake'te koddan kurulur — sahne dosyasında prefab/sprite gerekmez.
/// </summary>
public class GameHUD : MonoBehaviour
{
    // ─── Inspector ───────────────────────────────────────────

    [Header("References")]
    [SerializeField] private ShotInput    shotInput;
    [SerializeField] private MatchReferee referee;
    [SerializeField] private GameManager  gameManager;

    // ─── UI Elements (runtime-built) ─────────────────────────

    private Text  scoreText;
    private Text  streakText;
    private Text  messageText;
    private Text  powerPercentText;

    private RectTransform powerFill;
    private Image         powerFillImage;
    private RectTransform curveFill;
    private Image         curveFillImage;
    private RectTransform curveMarker;

    private const float BarWidth      = 400f;
    private const float CurveHalf     = BarWidth * 0.5f;

    private Coroutine messageRoutine;
    private Font uiFont;

    // Renk paleti
    private static readonly Color PanelBg    = new Color(0.05f, 0.08f, 0.12f, 0.72f);
    private static readonly Color BarBg      = new Color(0f, 0f, 0f, 0.55f);
    private static readonly Color LabelColor = new Color(1f, 1f, 1f, 0.85f);
    private static readonly Color CurveRight = new Color(0.25f, 0.8f, 1f);
    private static readonly Color CurveLeft  = new Color(1f, 0.75f, 0.25f);

    // ─── Unity Lifecycle ─────────────────────────────────────

    private void Awake()
    {
        uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        BuildCanvas();
    }

    private void OnEnable()
    {
        if (shotInput != null)
        {
            shotInput.OnPowerChanged += HandlePower;
            shotInput.OnCurveChanged += HandleCurve;
        }
        if (referee != null)
        {
            referee.OnAttemptStarted += ClearMessage;
            referee.OnOutcome        += HandleOutcome;
        }
        if (gameManager != null)
            gameManager.OnStatsChanged += RefreshStats;
    }

    private void OnDisable()
    {
        if (shotInput != null)
        {
            shotInput.OnPowerChanged -= HandlePower;
            shotInput.OnCurveChanged -= HandleCurve;
        }
        if (referee != null)
        {
            referee.OnAttemptStarted -= ClearMessage;
            referee.OnOutcome        -= HandleOutcome;
        }
        if (gameManager != null)
            gameManager.OnStatsChanged -= RefreshStats;
    }

    private void Start()
    {
        RefreshStats();
        HandlePower(0f);
        HandleCurve(0f);
    }

    // ─── Event Handlers ──────────────────────────────────────

    private void HandlePower(float power)
    {
        powerFill.sizeDelta = new Vector2(BarWidth * power, powerFill.sizeDelta.y);
        powerFillImage.color = power < 0.5f
            ? Color.Lerp(new Color(0.55f, 1f, 0.4f), Color.yellow, power * 2f)
            : Color.Lerp(Color.yellow, new Color(1f, 0.25f, 0.15f), (power - 0.5f) * 2f);

        powerPercentText.text = Mathf.RoundToInt(power * 100f) + "%";
    }

    private void HandleCurve(float curve)
    {
        // Merkezden sola/sağa dolan bar.
        bool right = curve >= 0f;
        curveFill.pivot     = new Vector2(right ? 0f : 1f, 0.5f);
        curveFill.sizeDelta = new Vector2(Mathf.Abs(curve) * CurveHalf, curveFill.sizeDelta.y);
        curveFillImage.color = right ? CurveRight : CurveLeft;

        curveMarker.anchoredPosition = new Vector2(curve * (CurveHalf - 4f), 0f);
    }

    private void RefreshStats()
    {
        if (gameManager == null) return;
        scoreText.text  = $"GOL {gameManager.Goals} / {gameManager.Shots}";
        streakText.text = $"Seri {gameManager.Streak}  ·  Rekor {gameManager.BestStreak}";
    }

    private void ClearMessage()
    {
        if (messageRoutine != null) StopCoroutine(messageRoutine);
        messageText.text = "";
    }

    private void HandleOutcome(MatchReferee.ShotOutcome outcome)
    {
        string msg; Color color;
        switch (outcome)
        {
            case MatchReferee.ShotOutcome.Goal:     msg = "GOOOL!";            color = new Color(0.2f, 1f, 0.3f); break;
            case MatchReferee.ShotOutcome.Saved:    msg = "KALECİ KURTARDI!";  color = new Color(1f, 0.65f, 0.1f); break;
            case MatchReferee.ShotOutcome.Wall:     msg = "BARAJA TAKILDI!";   color = new Color(1f, 0.55f, 0.4f); break;
            case MatchReferee.ShotOutcome.Woodwork: msg = "DİREK!";            color = new Color(1f, 0.9f, 0.2f); break;
            default:                                msg = "AUT!";              color = new Color(1f, 0.35f, 0.3f); break;
        }

        if (messageRoutine != null) StopCoroutine(messageRoutine);
        messageRoutine = StartCoroutine(ShowMessage(msg, color));
    }

    private IEnumerator ShowMessage(string msg, Color color)
    {
        messageText.text  = msg;
        messageText.color = color;

        // Kısa bir "pop" ölçek animasyonu.
        RectTransform rt = messageText.rectTransform;
        float t = 0f;
        while (t < 0.18f)
        {
            t += Time.deltaTime;
            rt.localScale = Vector3.one * Mathf.Lerp(1.6f, 1f, t / 0.18f);
            yield return null;
        }
        rt.localScale = Vector3.one;

        yield return new WaitForSeconds(1.6f);

        // Yumuşak kaybolma.
        t = 0f;
        Color c = messageText.color;
        while (t < 0.4f)
        {
            t += Time.deltaTime;
            messageText.color = new Color(c.r, c.g, c.b, 1f - t / 0.4f);
            yield return null;
        }
        messageText.text = "";
        messageRoutine = null;
    }

    // ─── UI Construction ─────────────────────────────────────

    private void BuildCanvas()
    {
        GameObject canvasGO = new GameObject("HUDCanvas");
        canvasGO.transform.SetParent(transform, false);

        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight  = 0.5f;

        canvasGO.AddComponent<GraphicRaycaster>();

        // ── Skor paneli (sol üst) ──
        Image scorePanel = MakeImage(canvasGO.transform, "ScorePanel", PanelBg);
        SetRect(scorePanel.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(20, -20), new Vector2(360, 108));

        scoreText  = MakeText(scorePanel.transform, "ScoreText", 44, FontStyle.Bold, TextAnchor.UpperLeft);
        SetRect(scoreText.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(18, -10), new Vector2(330, 54));

        streakText = MakeText(scorePanel.transform, "StreakText", 26, FontStyle.Normal, TextAnchor.UpperLeft);
        streakText.color = new Color(1f, 1f, 1f, 0.7f);
        SetRect(streakText.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(20, -66), new Vector2(330, 34));

        // ── Sonuç mesajı (orta) ──
        messageText = MakeText(canvasGO.transform, "MessageText", 96, FontStyle.Bold, TextAnchor.MiddleCenter);
        SetRect(messageText.rectTransform, new Vector2(0.5f, 0.62f), new Vector2(0.5f, 0.62f), Vector2.zero, new Vector2(1400, 140));
        var outline = messageText.gameObject.AddComponent<Outline>();
        outline.effectColor    = new Color(0f, 0f, 0f, 0.85f);
        outline.effectDistance = new Vector2(3, -3);

        // ── Alt panel: ŞUT GÜCÜ + FALSO GÜCÜ barları ──
        Image bottomPanel = MakeImage(canvasGO.transform, "BottomPanel", PanelBg);
        SetRect(bottomPanel.rectTransform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 26), new Vector2(700, 118));

        // — Şut gücü satırı —
        Text powerLabel = MakeText(bottomPanel.transform, "PowerLabel", 24, FontStyle.Bold, TextAnchor.MiddleRight);
        powerLabel.text  = "ŞUT GÜCÜ";
        powerLabel.color = LabelColor;
        SetRect(powerLabel.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(10, -14), new Vector2(160, 34));

        Image powerBg = MakeImage(bottomPanel.transform, "PowerBarBG", BarBg);
        SetRect(powerBg.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(186, -16), new Vector2(BarWidth + 8, 30));

        Image pFill = MakeImage(powerBg.transform, "PowerBarFill", Color.white);
        powerFillImage = pFill;
        powerFill = pFill.rectTransform;
        powerFill.anchorMin = new Vector2(0, 0.5f);
        powerFill.anchorMax = new Vector2(0, 0.5f);
        powerFill.pivot     = new Vector2(0, 0.5f);
        powerFill.anchoredPosition = new Vector2(4, 0);
        powerFill.sizeDelta = new Vector2(0, 22);

        powerPercentText = MakeText(bottomPanel.transform, "PowerPercent", 24, FontStyle.Bold, TextAnchor.MiddleLeft);
        powerPercentText.color = LabelColor;
        SetRect(powerPercentText.rectTransform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-14, -14), new Vector2(84, 34));

        // — Falso gücü satırı —
        Text curveLabel = MakeText(bottomPanel.transform, "CurveLabel", 24, FontStyle.Bold, TextAnchor.MiddleRight);
        curveLabel.text  = "FALSO GÜCÜ";
        curveLabel.color = LabelColor;
        SetRect(curveLabel.rectTransform, new Vector2(0, 0), new Vector2(0, 0), new Vector2(10, 18), new Vector2(160, 34));

        Image curveBg = MakeImage(bottomPanel.transform, "CurveBarBG", BarBg);
        SetRect(curveBg.rectTransform, new Vector2(0, 0), new Vector2(0, 0), new Vector2(186, 16), new Vector2(BarWidth + 8, 26));

        // Merkez çizgisi.
        Image centerTick = MakeImage(curveBg.transform, "CenterTick", new Color(1f, 1f, 1f, 0.55f));
        SetRect(centerTick.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(2, 26));

        // Merkezden dolan falso barı.
        Image cFill = MakeImage(curveBg.transform, "CurveFill", CurveRight);
        curveFillImage = cFill;
        curveFill = cFill.rectTransform;
        curveFill.anchorMin = new Vector2(0.5f, 0.5f);
        curveFill.anchorMax = new Vector2(0.5f, 0.5f);
        curveFill.pivot     = new Vector2(0f, 0.5f);
        curveFill.anchoredPosition = Vector2.zero;
        curveFill.sizeDelta = new Vector2(0, 18);

        // Falso imleci.
        Image marker = MakeImage(curveBg.transform, "CurveMarker", Color.white);
        curveMarker = marker.rectTransform;
        SetRect(curveMarker, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(6, 26));

        // Uç etiketleri: SOL / SAĞ falso yönleri.
        Text leftHint = MakeText(bottomPanel.transform, "CurveHintLeft", 18, FontStyle.Bold, TextAnchor.MiddleCenter);
        leftHint.text = "SOL";
        leftHint.color = CurveLeft;
        SetRect(leftHint.rectTransform, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-96, 18), new Vector2(46, 30));

        Text rightHint = MakeText(bottomPanel.transform, "CurveHintRight", 18, FontStyle.Bold, TextAnchor.MiddleCenter);
        rightHint.text = "SAĞ";
        rightHint.color = CurveRight;
        SetRect(rightHint.rectTransform, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-46, 18), new Vector2(46, 30));

        // ── Yardım metni (sol alt) ──
        Text help = MakeText(canvasGO.transform, "HelpText", 22, FontStyle.Normal, TextAnchor.LowerLeft);
        help.text  = "Fare: Nişan  ·  Basılı tut: Güç dolar  ·  Dolunca falso ibresi salınır  ·  Bırak: Şut";
        help.color = new Color(1f, 1f, 1f, 0.6f);
        SetRect(help.rectTransform, new Vector2(0, 0), new Vector2(0, 0), new Vector2(24, 18), new Vector2(900, 30));
    }

    private Text MakeText(Transform parent, string name, int size, FontStyle style, TextAnchor anchor)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        Text t = go.AddComponent<Text>();
        t.font      = uiFont;
        t.fontSize  = size;
        t.fontStyle = style;
        t.alignment = anchor;
        t.color     = Color.white;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow   = VerticalWrapMode.Overflow;
        t.raycastTarget = false;
        return t;
    }

    private Image MakeImage(Transform parent, string name, Color color)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        Image img = go.AddComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
        return img;
    }

    private void SetRect(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos, Vector2 size)
    {
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot     = new Vector2(anchorMin.x == anchorMax.x ? anchorMin.x : 0.5f,
                                   anchorMin.y == anchorMax.y ? anchorMin.y : 0.5f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta        = size;
    }
}
