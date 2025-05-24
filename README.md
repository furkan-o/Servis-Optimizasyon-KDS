# Servis Optimizasyon Karar Destek Sistemi

Bu proje, çalışanların adreslerine göre optimum servis güzergahı ve en uygun servis araç kombinasyonunu belirleyen bir karar destek sistemidir. Windows Forms (.NET 8) ile geliştirilmiştir.

## Özellikler

- Çalışan adreslerini JSON dosyalarından okur.
- Google Maps Distance Matrix API ile mesafe matrisi oluşturur.
- Genetik algoritma ile optimum servis rotası (Gezgin Satıcı Problemi) çözer.
- Servis araçlarının kapasite ve kilometre bazlı ücretlerini dikkate alarak en uygun maliyetli kombinasyonu bulur.
- Sonuçları Word veya metin dosyası olarak raporlayabilir.
- Kullanıcı dostu arayüz.

## Kurulum

1. **Gereksinimler**
   - .NET 8.0 (veya üzeri)
   - Windows işletim sistemi
   - [Google Maps Distance Matrix API](https://developers.google.com/maps/documentation/distance-matrix/overview) anahtarı

2. **Bağımlılıklar**
   - [Xceed.Words.NET](https://github.com/xceedsoftware/DocX)
   - [Newtonsoft.Json](https://www.newtonsoft.com/json)
   - [DocX](https://github.com/xceedsoftware/DocX)

3. **Projeyi Çalıştırma**
   - Projeyi Visual Studio ile açın.
   - `form1.cs` dosyasındaki `MesafeMatrisiHesapla` fonksiyonunda `"API BURAYA"` kısmına kendi Google API anahtarınızı `"ŞİRKET ADRESİ"` kısmına başlangıç adresi girin.
   - Gerekli NuGet paketlerini yükleyin.
   - Projeyi başlatın.

## Kullanım

1. Uygulamayı başlatın.
2. Ana ekranda bir bölge seçin.
3. "Optimizasyon Yap" butonuna tıklayın.
4. Sonuçlar sekmesinde optimum rota, servis kombinasyonu ve maliyetleri görüntüleyin.
5. İsterseniz rapor oluşturup kaydedebilirsiniz.

## Dosya Yapısı

- `form1.cs` : Ana uygulama kodları ve algoritmalar.
- `servisucretleri.json` : Servis araçlarının kilometre ve kapasite bazlı ücretleri.
- `[bolge].json` : Her bölge için çalışan adresleri.

## Lisans

Bu proje [MIT Lisansı](LICENSE.txt) ile lisanslanmıştır.

## Katkı

Katkıda bulunmak isterseniz lütfen bir pull request gönderin veya issue açın.

---

**Not:** Google API anahtarınızı kimseyle paylaşmayınız. API kullanımında kota ve ücretlendirme Google tarafından belirlenmektedir.
