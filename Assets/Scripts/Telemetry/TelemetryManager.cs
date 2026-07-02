using System.Collections.Generic;
using UnityEngine;

namespace FreekickGame.Telemetry
{
    /// <summary>
    /// Oyuncunun davranışlarını ve eğlence metriklerini toplayan Telemetry sistemi.
    /// (SPM, Hold Time, Reset Delay vb.)
    /// </summary>
    public class TelemetryManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private ShotInput shotInput;
        [SerializeField] private BallLauncher ballLauncher;
        [SerializeField] private BallResetter ballResetter;

        [Header("Session Data (Read Only)")]
        public int totalShots = 0;
        public float sessionStartTime = 0f;

        // Metrik ölçümleri için zaman damgaları
        private float lastMousePressTime = 0f;
        private float lastShotReleaseTime = 0f;
        private float lastResetReadyTime = 0f;
        
        private void Start()
        {
            sessionStartTime = Time.realtimeSinceStartup;
            lastResetReadyTime = Time.realtimeSinceStartup; // İlk şut için başlangıç
        }

        private void OnEnable()
        {
            if (shotInput != null)
            {
                // Input sistemi henüz OnPointerDown/Up gibi eventlere tam sahip değilse Update'den de kontrol edebiliriz
                // Şimdilik shotInput power değişimini baz alabiliriz ama daha temizi Update içinde mouse takibi.
            }
            
            if (ballLauncher != null) ballLauncher.OnBallFired += HandleShotFired;
            if (ballResetter != null) ballResetter.OnBallReset += HandleBallReset;
        }

        private void OnDisable()
        {
            if (ballLauncher != null) ballLauncher.OnBallFired -= HandleShotFired;
            if (ballResetter != null) ballResetter.OnBallReset -= HandleBallReset;
        }

        private void Update()
        {
            // Şarjın başlama anını yakala
            if (Input.GetMouseButtonDown(0))
            {
                lastMousePressTime = Time.realtimeSinceStartup;
                
                // Reset olduktan sonra mouse'a ne kadar sürede basıldı?
                float resetDelay = lastMousePressTime - lastResetReadyTime;
                
                // Eğer oyun yeni başladıysa (ilk şut) reset delay anlamsız olur, o yüzden kısıtlayalım
                if (totalShots > 0)
                {
                    LogMetric("Reset_Delay", resetDelay);
                }
            }
        }

        private void HandleShotFired()
        {
            totalShots++;
            lastShotReleaseTime = Time.realtimeSinceStartup;

            // 1. Hold Distribution (Mouse Hold Duration)
            float holdDuration = lastShotReleaseTime - lastMousePressTime;
            LogMetric("Hold_Duration", holdDuration);

            // 2. SPM (Shots Per Minute)
            float totalSessionMinutes = (Time.realtimeSinceStartup - sessionStartTime) / 60f;
            float currentSPM = totalShots / Mathf.Max(totalSessionMinutes, 0.01f);
            LogMetric("SPM", currentSPM);
            
            // 3. Power Distribution 
            // Power değerini shotInput'tan alabiliriz (veya public bir property ekleyebiliriz)
            // Şimdilik basitçe Log içine not düşüyoruz
        }

        private void HandleBallReset()
        {
            lastResetReadyTime = Time.realtimeSinceStartup;
        }

        private void LogMetric(string metricName, float value)
        {
            // İleride Mixpanel, GameAnalytics vb. servislere bağlanacak. Şimdilik Console log.
            // Sadece Editor'de veya Debug modda görelim.
            Debug.Log($"<color=cyan>[Telemetry]</color> {metricName}: <b>{value:F2}</b>");
        }
    }
}
