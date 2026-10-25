# Gatefall — Sonsuz Kuşatma

iOS ve Android hedefli, Unity ile geliştirilen özgün bir 3D sonsuz savunma prototipi.
Bu ilk sürüm mağazaya hazır veya yayımlanmış bir oyun değildir.

## Güncel durum (25 Ekim 2026)

- Editörde oynanabilir: başlangıç, oyun, duraklatma, geliştirme seçimi, titan dalgası, oyun sonu ve yeniden başlatma akışları çalışıyor.
- Çekirdek simülasyon testleri: **16/16 geçti** (`bash Scripts/verify.sh`).
- Editör duman testi: **PASS** — ateş, duraklatma, yükseltme, boss, oyun sonu ve yeniden başlatma doğrulandı (`Evidence/runtime-verification.txt`).
- Kapı çarpanları ve hasar yazıları ekran-uzayı UI olarak çiziliyor; TextMesh font atlası bozulması giderildi.
- Kalabalık yoğunluğu ve titan ölçeği referans videoya yaklaştırıldı, kamera oyuncunun arkasına alındı.
- **Bilinen engel:** Bu Mac'te standalone oyuncu derlemesi `Library/PlayerDataCache/.../unity_builtin_extra` yazımında başarısız oluyor; Android/iOS Build Support modülleri de kurulu değil. Bu nedenle APK/AAB/IPA üretilmedi.
- Editör Game view ekran yakalaması bu makinede döşemeli (tiled) çıkıyor; bu bir yakalama/GUI artefaktı, oyun mantığı değil. Depoda yalnızca doğrulama metni tutuluyor.


## Aç ve oyna

1. Unity **6.3 LTS / 6000.3.27f1** ile bu klasörü proje olarak aç.
2. İlk açılışta paketlerin ve kodların içe aktarılmasını bekle.
3. Başlangıç sahnesi otomatik oluşturulur. **Game** sekmesinde **Play** düğmesine bas.
4. **Savunmayı Başlat** ile oyna. Gerekirse `Gatefall > Prepare playable project` menüsünü kullan.

### Kontroller

- **Telefon:** Ekranı sağa sola sürükle. Ateş otomatiktir.
- **Bilgisayar:** Fareyle sürükle veya A/D, sol/sağ ok tuşlarını kullan.
- **P / Esc:** Duraklat veya devam et.
- Dalgayı tamamlayınca üç geliştirmeden birini seç. Her beşinci dalgada titan gelir.
- Savunma canın biterse koşu sona erer. Rekor cihazda saklanır.

## Mevcut oynanış

- Sonsuz dalgalar; normal, hızlı ve zırhlı düşmanlar; hareketli, saldıran titan.
- ×2/×3 kapıları; ilerleyen dalgalarda ×3/×4. Her birlik yalnızca bir kez çoğalır.
- Güç, atış hızı, çoklu atış, onarım, kalkan ve birlik hızı geliştirmeleri.
- Prosedürel 3D robotlar, çöl geçidi, animasyonlar ve sentezlenmiş ses efektleri.
- Başlangıç, oyun, duraklatma, geliştirme seçimi ve oyun sonu ekranları.
- Türkçe arayüz, dikey ekran ve güvenli alan uyarlaması; yerel rekor ve ses tercihi.
- Odağı kaybedince duraklama. Sunucu, hesap veya ağ bağlantısı gerekmez.

## Proje yapısı

- `Assets/Gatefall/Scripts/Core`: Unity'den bağımsız, deterministik oyun kuralları.
- `Assets/Gatefall/Scripts/Runtime`: 3D görünüm, kontroller, arayüz ve sesler.
- `Assets/Gatefall/Editor`: otomatik sahne oluşturma, mobil ve Mac paket komutları.
- `Tests/Core`: harici test paketine ihtiyaç duymayan çalıştırılabilir C# testleri.

## Testler

.NET 8 SDK kuruluysa:

```bash
bash Scripts/verify.sh
```

Unity kurulu ve lisansı etkinse, `UNITY_EDITOR` değişkenini gerçek editör yoluna ayarlayarak
aynı komuta Unity içe aktarma/derleme kontrolünü ekleyebilirsin.

Görsel çalışma testi için editörü `-executeMethod Gatefall.Editor.ProjectSetup.RunSmoke -gatefall-smoke`
argümanlarıyla çalıştır. Grafik erişimi ve etkin Unity lisansı gerekir. `Evidence/` içine
başlangıç, oyun, duraklatma, geliştirme, boss ve oyun sonu ekranları ile sonuç dosyası yazar.

## Mobil paketler

İlgili Android veya iOS Build Support modülünü kurduktan sonra:

- `Gatefall > Build > Android test APK`: cihazda deneme paketi.
- `Gatefall > Build > Android App Bundle`: Google Play için AAB dosyası üretim yolu.
- `Gatefall > Build > iOS Xcode project`: Xcode projesi üretim yolu.
- `Gatefall > Build > Mac prototype`: yerel Mac deneme sürümü.

Paket adı geçici olarak `com.prototype.gatefall`. Mağaza için benzersiz kimlik,
Android imzalama anahtarı, Apple takım/sertifika ayarları, güncel platform gereksinimleri,
gerçek cihaz testleri ve mağaza bilgileri ayrıca tamamlanmalıdır. Anahtar ve şifreler
projeye eklenmemiştir. Henüz imzalı APK/AAB/IPA hazırlandığı iddia edilmez.

## Sonraki geliştirmeler

Özgün karakter/model çalışması, telefonda performans ve pil ölçümü, oyun dengesi,
kalıcı ilerleme, farklı arenalar, erişilebilirlik ayarları ve mağaza hazırlıkları.
Bu prototipte ekran okuyucu veya sistem yazı boyutu desteği doğrulanmış değildir.
