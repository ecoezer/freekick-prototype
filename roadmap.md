# ⚽ Freekick Game Roadmap

Bu yol haritası, oyunun sıfırdan ileri seviye özelliklere kadar nasıl gelişeceğini adım adım gösterir. Mevcut yapı, bu büyüme öngörülerek SOLID prensipleriyle tasarlanmıştır.

## 🏁 Aşama 1: Temel Prototip (Mevcut Durum)
- [x] GameObject'lerin listelenmesi ve ayarlanması (Scene, Physics, Tags).
- [x] `ShotData` yapısının oluşturulması.
- [x] OCP uyumlu yön hesaplama (`IDirectionProvider` & `GoalDirectionProvider`).
- [x] Mouse basılı tutularak güç yükleme sistemi (`ShotInput`).
- [x] Topa impulse uygulama ve fırlatma (`BallLauncher`).
- [x] Topun durumunu izleme ve 3 saniye sonra başa dönme (`BallStateTracker` & `BallResetter`).
- [x] Şut atılmadan önce topun arkasında duran, atıldıktan sonra takip eden dinamik kamera (`FreekickCameraController`).
- [x] Scriptler arası event tabanlı koordinasyon (`ShotController`).

## 🎯 Aşama 2: Nişan Alma ve UI (Tamamlandı)
- [x] **Hedef Belirleme:** `AimDirectionProvider` eklenerek oyuncunun ekranda nişan aldığı noktaya doğru şut atabilmesi.
- [x] **Görsel Yardımcılar:** Topun gidiş yönünü gösteren LineRenderer (`AimVisualController`).
- [x] **UI Güç Barı:** `ShotInput.OnPowerChanged` eventini dinleyerek dolan bir güç barı arayüzü (`PowerBarView`).
- [x] **Gol Tespiti:** Kaledeki görünmez collider ile çarpışmayı algılayan `GoalDetector` scriptinin yazılması.

## 🌪️ Aşama 3: Gelişmiş Fizik ve Kaleci
- [ ] **Curve (Falso) ve Spin:** `ShotData` sınıfına Curve/Spin alanlarının eklenmesi.
- [ ] **Magnus Etkisi:** `BallLauncher` veya yeni bir fizik scripti ile top havadayken rotasyon ve falso hesaplamalarının eklenmesi.
- [ ] **Kaleci Sistemi:** `GoalkeeperController` scripti ile topun geliş yönünü tahmin eden ve ona göre animasyon tetikleyen basit bir kaleci yapay zekası.
- [ ] **Baraj (Wall):** Savunma barajındaki oyuncuların zıplama ve top sekme fizikleri.

## 🏆 Aşama 4: Oyun Döngüsü ve Meta Sistemler
- [ ] **Skor Sistemi:** Atılan golleri ve kaçan şutları tutan `ScoreManager`.
- [ ] **Menü Sistemi:** Ana Menü, Ayarlar ve Oyun İçi (Pause) menülerin Clean Architecture'a uygun şekilde eklenmesi.
- [ ] **Ses ve Efektler (Audio & VFX):** Şut sesi, direk sesi, seyirci tepkileri ve topun arkasında çıkan `TrailRenderer` efektleri.
- [ ] **Takım ve Turnuva:** Takım seçimi, turnuva ağacı ve ilerleme sistemleri.

## 🌐 Aşama 5: Multiplayer ve Optimizasyon
- [ ] **Online Altyapı:** Sıra tabanlı veya eşzamanlı multiplayer desteği.
- [ ] **Performans Optimizasyonu:** Object Pooling, URP grafik optimizasyonları ve mobil platformlar için ayarlamalar.
