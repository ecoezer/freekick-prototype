using UnityEngine;

/// <summary>
/// Şutun yönünü hesaplayan strateji için arayüz.
///
/// OCP uyumu: ShotInput bu interface'i kullanır; yön hesabının detayları
/// bu arayüzün arkasında gizlenir. Yeni bir yön stratejisi (nişan alma, 
/// rastgele vb.) eklemek için yalnızca bu interface'i implement eden
/// yeni bir class yazmak yeterlidir. ShotInput'a dokunulmaz.
///
/// Mevcut implementasyonlar:
///   GoalDirectionProvider — Prototype: sabit kale yönü
///
/// Planlanan implementasyonlar:
///   AimDirectionProvider  — Oyuncunun mouse ile nişan aldığı yön
/// </summary>
public interface IDirectionProvider
{
    /// <summary>
    /// Verilen 'fromPosition' noktasından hedefe doğru
    /// normalize edilmiş yön vektörünü döndürür.
    /// </summary>
    /// <param name="fromPosition">Başlangıç noktası (genellikle topun pozisyonu).</param>
    Vector3 GetDirection(Vector3 fromPosition);
}
