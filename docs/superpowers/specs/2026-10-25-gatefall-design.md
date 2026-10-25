# Gatefall — ilk oynanabilir sürüm

Kullanıcı, paylaştığı reklam videosundaki kalabalık ve çarpan mekaniklerinden esinlenen,
App Store ve Google Play'e hazırlanabilecek sonsuz bir Unity oyununun geliştirilmesini istedi.
Bu sürüm bir oynanış prototipidir. Yayınlanmış veya mağazaya hazır ürün değildir.

## İlk sürüm

- Unity 6.3 LTS, C#, gerçek 3D, dikey ekran, Türkçe arayüz.
- Mavi top, parmakla sürükleme veya klavye ile yatay kontrol, otomatik ateş.
- Atışları bir kez çoğaltan iki hareketli kapı; sınırlı aktif birlik sayısı.
- Kırmızı düşmanlar oyuncunun savunma çizgisine doğru ilerler.
- Her beşinci dalgada büyük, animasyonlu bir boss; diğer dalgalarda farklı düşmanlar.
- Dalga sonunda üç geliştirme arasından seçim. Atış, güç, çoklu atış ve savunma seçenekleri.
- Can, skor, rekor, dalga göstergesi; duraklatma, ses ayarı, yeniden başlama.
- Uygulama odağı kaybedilince duraklama, çentik ve güvenli ekran alanına uyum.
- İnternetsiz tek oyuncu; hesap, sunucu, reklam, ödeme ve kişisel veri toplama yok.

## Uygulama kararları

Geçici ad Gatefall. Özgün, prosedürel, stilize robotlar ve çöl geçidi kullanılır;
videonun marka ve modelleri kopyalanmaz. Unity'nin yerleşik render hattı ve uGUI kullanılır.
Oyun kuralları Unity'den bağımsız C# sınıfında çalışır. Görselleştirme bu durumu okur.
Simülasyon sabit adımla ilerler; atış çarpışmaları hareket aralığı boyunca kontrol edilir.
Mermi/birlik sayısı en fazla 240, aktif düşman sayısı en fazla 64 olur.

## Başarı ölçütü

Başlat → ateş et → kapıdan çoğalt → dalgayı bitir → geliştirme seç → devam et;
can bittiğinde skor kaydedilip yeni koşuya başlanabilmesi. Boss ve normal dalga
geçişleri, duraklatma ve yeniden başlama otomatik testlerle doğrulanır.
Editör lisansı veya kurulumu engel olursa kaynaklar ve doğrulanan kısımlar teslim edilir;
çalıştırılmayan kontroller açıkça belirtilir.

## Daha sonraki sürümler

Telefon performans ölçümleri, özgün sanat/ses üretimi, dengeleme, kalıcı ilerleme,
mağaza görselleri, gelir modeli ve imzalı mobil paketler bu ilk prototipin devamıdır.
