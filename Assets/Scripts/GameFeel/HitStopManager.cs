using UnityEngine;

namespace FreekickGame.GameFeel
{
    /// <summary>
    /// Şut atıldığı anda oyunun çok kısa süreliğine "donmasını" (Freeze Frame / Hit Stop) sağlar.
    /// Bu etki, vuruşun gücünü (impact) hissettirmek için kullanılır.
    ///
    /// NOT: Coroutine/WaitForSecondsRealtime kullanılmaz — WebGL'de timeScale'in
    /// 0.1'de takılı kalmasına yol açıyordu. Restore işlemi Update içinde
    /// unscaled zamanla yapılır; ayrıca watchdog beklenmedik durumda bile
    /// zamanı normale döndürür.
    /// </summary>
    public class HitStopManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private BallLauncher ballLauncher;

        [Header("Hit Stop Settings")]
        [Tooltip("Hit Stop sırasında zamanın akış hızı (0 = tam donma, 0.1 = yavaş çekim)")]
        [SerializeField] private float hitStopTimeScale = 0.1f;

        [Tooltip("Hit Stop etkisinin ne kadar süreceği (gerçek zaman olarak saniye)")]
        [SerializeField] private float hitStopDuration = 0.05f;

        /// <summary>Unscaled zamana göre restore anı; aktif hitstop yoksa -1.</summary>
        private float restoreAt = -1f;

        private void OnEnable()
        {
            if (ballLauncher != null)
            {
                ballLauncher.OnBallFired += TriggerHitStop;
            }
        }

        private void OnDisable()
        {
            // Güvenlik: obje kapanırken zaman asla yavaş kalmasın.
            Time.timeScale = 1f;
            restoreAt = -1f;

            if (ballLauncher != null)
            {
                ballLauncher.OnBallFired -= TriggerHitStop;
            }
        }

        private void Update()
        {
            if (restoreAt >= 0f)
            {
                if (Time.unscaledTime >= restoreAt)
                {
                    Time.timeScale = 1f;
                    restoreAt = -1f;
                }
            }
            else if (Time.timeScale < 1f)
            {
                // Watchdog: hitstop aktif değilken zaman yavaşsa düzelt.
                Time.timeScale = 1f;
            }
        }

        private void TriggerHitStop()
        {
            Time.timeScale = hitStopTimeScale;
            restoreAt = Time.unscaledTime + hitStopDuration;
        }
    }
}
