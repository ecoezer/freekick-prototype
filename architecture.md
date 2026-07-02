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

## 4. Oyun Katmanı (v2 ile eklendi)

### F. Input v2 (Nişan + Falso)
- `ITargetPointProvider`: Kale düzlemindeki tam nişan noktasını sağlar.
- `AimTargetProvider`: Fare → kale düzlemi raycast; yatay/dikey sınırlar içinde nişangâh.
- `AimReticle`: Nişan noktasında billboard halka çizer (güçle renk değiştirir).
- `AimVisualController`: Balistik yay önizlemesi — `BallLauncher.ComputeLaunchVelocity`
  ve `BallPhysicsController` katsayılarıyla gerçek uçuşla tutarlı; yayın %65'i gösterilir.
- `ShotInput` v2: A/D ile falso (Curve) girişi; `ShotData`'ya Curve + TargetPoint koyar.

### G. Ballistics
- `BallLauncher.ComputeLaunchVelocity`: hedefe oturan parabol çözümü
  (yatay hız = Power, dikey hız = dy/t + g·t/2). Falso: çıkış yönü falsonun
  tersine bükülür, topa Y-spini verilir; Magnus uçuşta hedefe geri kıvırır.

### H. Opponents (Rakipler)
- `GoalkeeperController`: Idle → Reacting → Diving durum makinesi. Reaksiyon
  gecikmesi + kesişme tahmini (lineer XZ + yerçekimli Y) + tahmin gürültüsü.
  Topa teması `OnBallTouched` ile bildirir.
- `DefensiveWall` / `WallPlayer`: 9.15 m'ye dizilen 3-4 kişilik baraj; şutta zıplar.

### I. Gameplay (Hakem + Döngü)
- `GoalDetector`: Kale ağzı arkasındaki trigger; component tabanlı top tespiti.
- `WoodworkNotifier`: Direk/üst direk teması.
- `MatchReferee`: Sonuç öncelik sırası GOL > KURTARIŞ > BARAJ > DİREK > AUT;
  9 sn failsafe ile kaçak topu zorla resetler.
- `GameManager`: Skor/seri/rekor (PlayerPrefs); her şutta rastgele yeni frikik
  noktası (16-26 m, ±32°) seçer, barajı yeniden dizer.

### J. UI
- `GameHUD`: Canvas + skor paneli + güç barı + falso göstergesi + sonuç mesajı.
  Tüm hiyerarşi koddan kurulur; sahnede prefab/sprite gerekmez.

## 5. Sahne Üretimi ve Build
- `SceneGenerator.GenerateScene` (Editor): saha (şeritli çim, çizgiler), kale
  (direkler + yarı saydam file + trigger), kaleci, baraj, top, kamera, ışık/sis
  ve TÜM referans bağlantılarını (SerializedObject) kurar. Materyaller
  `Assets/Materials` altına asset olarak kaydedilir (build'de pembe obje olmaz).
- `Builder.BuildWebGL` (Editor): `Builds/WebGL` altına sıkıştırmasız WebGL build.
- Komut satırı: `Unity -batchmode -quit -projectPath . -executeMethod SceneGenerator.GenerateScene` → ardından `Builder.BuildWebGL`.
