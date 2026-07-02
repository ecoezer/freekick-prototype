using UnityEngine;

/// <summary>
/// Nişan alınan dünya-uzayı hedef noktasını sağlayan strateji arayüzü.
/// IDirectionProvider yalnızca yön verir; balistik çözüm için tam nokta gerekir.
/// </summary>
public interface ITargetPointProvider
{
    /// <summary>Nişan alınan dünya-uzayı noktası (örn. kale düzlemi üzerinde).</summary>
    Vector3 GetTargetPoint();
}
