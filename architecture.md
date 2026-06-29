# ⚽ Freekick Game Architecture

Bu doküman, Freekick oyununun başlangıç prototipi için hazırlanan mimari yapıyı açıklar.

## 1. Temel Prensipler
- **Unity 6, URP, C#**
- **SOLID Prensipleri:** Her script tek bir sorumluluğa (SRP) sahiptir ve değişime kapalı, gelişime açık (OCP) olacak şekilde tasarlanmıştır.
- **Clean Architecture:** Katmanlı yapı, scriptlerin birbirine olan bağımlılıklarını minimize eder. (Örneğin, Input modülü yönü hesaplamak yerine bunu IDirectionProvider'dan ister).
- **Gereksiz Singleton Yok:** Her şey event tabanlı iletişim (Observer Pattern) ve referans enjeksiyonu ile çalışır.
- **Event-Driven:** Sistemler arası iletişim MonoBehaviour olayları (Action) üzerinden yapılır.

## 2. Modüller ve Sorumlulukları

### A. Data (Veri)
- `ShotData`: Şutun yönü (Direction) ve gücü (Power) bilgisini taşıyan saf C# veri sınıfı. (İleride Curve ve Spin eklenecek).

### B. Input (Giriş)
- `IDirectionProvider`: Yön hesaplama stratejisi (Interface).
- `GoalDirectionProvider`: IDirectionProvider'ın prototip implementasyonu (sabit olarak kaleye nişan alır).
- `ShotInput`: Fare girişini (basma/basılı tutma/bırakma) okur. Güç hesaplar ve şut verisini (`ShotData`) hazırlar.

### C. Ball (Top Fiziği ve Durumu)
- `BallLauncher`: Rigidbody'ye impulse kuvveti uygulayarak topu fırlatır.
- `BallStateTracker`: Topun fiziksel durumunu (hareketsiz/hareket halinde) izler ve durduğunda haber verir.
- `BallResetter`: Top durduğunda belirli bir süre (3 saniye) bekler ve topu başlangıç pozisyonuna taşır.

### D. Coordination (Koordinasyon)
- `ShotController`: `ShotInput` ve `BallLauncher` arasındaki köprüdür. Input'tan gelen veriyi Launcher'a aktarır, state geçişlerini düzenler.

### E. Camera (Kamera)
- `FreekickCameraController`: Topun durumuna (Fırlatıldı/Sıfırlandı) göre kamerayı topun arkasında sabit tutar (Idle) veya fırlatıldıktan sonra takip eder (Follow).

## 3. Data Flow (Veri Akışı)
1. **Input:** Oyuncu fareye basılı tutar (`ShotInput`).
2. **Event:** Fare bırakıldığında `OnShotReady(ShotData)` tetiklenir.
3. **Coordination:** `ShotController` bu event'i dinler ve `BallLauncher.Fire(ShotData)` çağrısını yapar. (Aynı zamanda yeni input'u geçici olarak kapatır).
4. **Physics:** `BallLauncher` topu fırlatır ve `OnBallFired` event'ini tetikler.
5. **Camera:** `FreekickCameraController` `OnBallFired`'ı duyar ve topu takip etmeye başlar.
6. **State Tracking:** `BallStateTracker` topun hızını denetler. Hız belirli bir eşiğin altına düşünce `OnBallStopped` event'ini fırlatır.
7. **Reset:** `BallResetter`, topun durduğunu anlar, 3 saniye bekler ve topu sıfırlar. Ardından `OnBallReset` event'ini tetikler.
8. **End of Loop:** `ShotController` yeni şut atılmasına izin verir, kamera başlangıç pozisyonuna döner.

## 4. İleriye Dönük Genişletilebilirlik (Freeze List)
Aşağıdaki modüller ileride eklenecek yeni özellikler karşısında (Curve, Spin, Goalkeeper vb.) **değiştirilmeyecektir**:
- `ShotController` (Sadece koordinatör)
- `BallStateTracker` (Sadece hızı izler)
- `BallResetter` (Sadece pozisyonu geri alır)
- `IDirectionProvider` (Interface sabit kalır)

*Gelecekte Eklenecekler:* Nişan alma (`AimDirectionProvider`), falso sistemi (`ShotData`'ya Curve eklenmesi ve Launcher'ın modifikasyonu), Kaleci (`GoalkeeperController`).
