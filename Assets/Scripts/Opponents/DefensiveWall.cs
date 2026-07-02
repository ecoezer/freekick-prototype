using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Savunma barajı: top ile kale arasına, 9.15 m mesafeye dizilen oyuncu duvarı.
///
/// Sorumluluklar:
///   - Configure(ballPos): barajı yeni frikik noktasına göre yerleştirir,
///     oyuncu sayısını (3-4) rastgele seçer.
///   - Şut anında oyuncular zıplar (alçak şutları ancak zıplamadan önce/sonra geçersin).
///   - Topla teması MatchReferee'ye event ile bildirir.
/// </summary>
public class DefensiveWall : MonoBehaviour
{
    // ─── Inspector ───────────────────────────────────────────

    [Header("References")]
    [SerializeField] private BallLauncher ballLauncher;
    [SerializeField] private Transform    goalCenter;

    [Header("Wall Settings")]
    [Tooltip("Barajın toptan uzaklığı (kural gereği 9.15 m).")]
    [SerializeField] private float wallDistance = 9.15f;

    [Tooltip("Oyuncular arası yatay mesafe (m).")]
    [SerializeField] private float playerSpacing = 0.55f;

    [Header("Jump Settings")]
    [Tooltip("Şuttan ne kadar sonra zıplanır (s).")]
    [SerializeField] private float jumpDelay = 0.12f;

    [Tooltip("Zıplama yüksekliği (m).")]
    [SerializeField] private float jumpHeight = 0.5f;

    [Tooltip("Zıplamanın toplam süresi (s).")]
    [SerializeField] private float jumpDuration = 0.55f;

    // ─── Events ──────────────────────────────────────────────

    /// <summary>Top barajdaki herhangi bir oyuncuya çarptığında tetiklenir.</summary>
    public event Action OnBallTouched;

    // ─── Private State ───────────────────────────────────────

    private readonly List<WallPlayer> players = new List<WallPlayer>();
    private readonly List<Vector3>    basePositions = new List<Vector3>();

    // ─── Unity Lifecycle ─────────────────────────────────────

    private void Awake()
    {
        // Çocuk objelerdeki WallPlayer'ları topla ve event'lerine abone ol.
        GetComponentsInChildren(true, players);
        foreach (WallPlayer p in players)
            p.OnBallHit += HandleBallHit;
    }

    private void OnEnable()
    {
        if (ballLauncher != null) ballLauncher.OnBallFired += HandleBallFired;
    }

    private void OnDisable()
    {
        if (ballLauncher != null) ballLauncher.OnBallFired -= HandleBallFired;
    }

    // ─── Public API ──────────────────────────────────────────

    /// <summary>
    /// Barajı verilen frikik noktasına göre yerleştirir.
    /// GameManager her yeni şut pozisyonunda çağırır.
    /// </summary>
    public void Configure(Vector3 ballPosition)
    {
        StopAllCoroutines();

        Vector3 toGoal = goalCenter.position - ballPosition;
        toGoal.y = 0f;
        float distToGoal = toGoal.magnitude;
        Vector3 dir = toGoal.normalized;

        // Top kaleye 10.5 m'den yakınsa baraj kale önüne sıkışır.
        float placement = Mathf.Min(wallDistance, distToGoal - 1.5f);
        Vector3 center  = ballPosition + dir * placement;
        center.y = 0f;

        transform.position = center;
        transform.rotation = Quaternion.LookRotation(-dir, Vector3.up); // Yüzü topa dönük.

        // 3-4 oyuncu rastgele.
        int activeCount = UnityEngine.Random.Range(3, players.Count + 1);

        basePositions.Clear();
        for (int i = 0; i < players.Count; i++)
        {
            bool active = i < activeCount;
            players[i].gameObject.SetActive(active);

            // Sırayı topun kale merkez hattına ortala.
            float offset = (i - (activeCount - 1) * 0.5f) * playerSpacing;
            Vector3 localPos = new Vector3(offset, 0f, 0f);
            players[i].transform.localPosition = localPos;
            basePositions.Add(players[i].transform.position);
        }
    }

    // ─── Event Handlers ──────────────────────────────────────

    private void HandleBallFired()
    {
        StartCoroutine(JumpRoutine());
    }

    private void HandleBallHit()
    {
        OnBallTouched?.Invoke();
    }

    // ─── Jump Animation ──────────────────────────────────────

    private IEnumerator JumpRoutine()
    {
        yield return new WaitForSeconds(jumpDelay);

        // Zıplamadan önceki taban pozisyonlarını al (Configure sonrası güncel).
        var starts = new List<Vector3>();
        foreach (WallPlayer p in players) starts.Add(p.transform.position);

        float t = 0f;
        while (t < jumpDuration)
        {
            t += Time.deltaTime;
            float phase = Mathf.Clamp01(t / jumpDuration);
            float height = 4f * jumpHeight * phase * (1f - phase); // Parabol.

            for (int i = 0; i < players.Count; i++)
            {
                if (!players[i].gameObject.activeSelf) continue;
                Vector3 pos = starts[i];
                pos.y = height;
                players[i].transform.position = pos;
            }
            yield return null;
        }

        for (int i = 0; i < players.Count; i++)
        {
            if (!players[i].gameObject.activeSelf) continue;
            Vector3 pos = players[i].transform.position;
            pos.y = 0f;
            players[i].transform.position = pos;
        }
    }
}
