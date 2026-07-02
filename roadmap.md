# ⚽ Freekick Game Roadmap

Bu yol haritası, oyunun sıfırdan ileri seviye özelliklere kadar nasıl gelişeceğini adım adım gösterir. Mevcut yapı, bu büyüme öngörülerek SOLID prensipleriyle tasarlanmıştır.

## 🏁 Aşama 1: Temel Prototip (Tamamlandı)
- [x] GameObject'lerin listelenmesi ve ayarlanması (Scene, Physics, Tags).
- [x] `ShotData` yapısının oluşturulması.
- [x] OCP uyumlu yön hesaplama (`IDirectionProvider` & `GoalDirectionProvider`).
- [x] Mouse basılı tutularak güç yükleme sistemi (`ShotInput`).
- [x] Topa hız atama ve fırlatma (`BallLauncher`).
- [x] Topun durumunu izleme ve durunca başa dönme (`BallStateTracker` & `BallResetter`).
- [x] Şut öncesi topun arkasında duran, sonrasında takip eden kamera (`FreekickCameraController`).
- [x] Scriptler arası event tabanlı koordinasyon (`ShotController`).

## 🎯 Aşama 2: Nişan Alma ve UI (Tamamlandı)
- [x] **Hedef Belirleme:** `AimTargetProvider` — fare, KALE DÜZLEMİNDE nişangâh kontrol eder (üst köşe/alt köşe seçilebilir).
- [x] **Görsel Yardımcılar:** Balistik yay önizlemesi (`AimVisualController`) + nişangâh halkası (`AimReticle`).
- [x] **UI Güç Barı:** `GameHUD` — güç barı, falso göstergesi, skor paneli, sonuç mesajları (tamamı koddan kurulur).
- [x] **Gol Tespiti:** Kale ağzının arkasındaki trigger ile `GoalDetector`.

## 🌪️ Aşama 3: Gelişmiş Fizik ve Kaleci (Tamamlandı)
- [x] **Curve (Falso) ve Spin:** `ShotData`'ya Curve + TargetPoint eklendi; A/D ile falso.
- [x] **Magnus Etkisi:** `BallPhysicsController` (drag + Magnus) ve `BallLauncher` balistik çözümü;
      çıkış yönü falsonun tersine bükülür, Magnus topu hedefe geri kıvırır ("bend it").
- [x] **Kaleci Sistemi:** `GoalkeeperController` — reaksiyon gecikmesi, kesişme tahmini
      (lineer + yerçekimi), yana koşu ve dalış; `predictionNoise` ile zorluk ayarı.
- [x] **Baraj (Wall):** `DefensiveWall` + `WallPlayer` — 9.15 m'de dizilir, şutta zıplar.

## 🏆 Aşama 4: Oyun Döngüsü ve Meta Sistemler (Çekirdek tamam)
- [x] **Sonuç Hakemi:** `MatchReferee` — GOL / KURTARIŞ / BARAJ / DİREK / AUT + failsafe reset.
- [x] **Skor Sistemi:** `GameManager` — gol/şut/seri; rekor seri PlayerPrefs ile kalıcı.
- [x] **Rastgele Frikik Noktası:** Her şutta 16-26 m, ±32° yeni pozisyon; baraj yeniden dizilir.
- [ ] **Menü Sistemi:** Ana Menü, Ayarlar ve Pause menüleri.
- [ ] **Ses ve Efektler:** Şut sesi, direk sesi, seyirci; gol konfetisi (partikül).
- [ ] **Takım ve Turnuva:** Takım seçimi, turnuva ağacı ve ilerleme sistemleri.

## 🌐 Aşama 5: Multiplayer ve Optimizasyon
- [ ] **Online Altyapı:** Sıra tabanlı veya eşzamanlı multiplayer desteği.
- [ ] **Performans Optimizasyonu:** Object Pooling, mobil dokunmatik kontroller.
