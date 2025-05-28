# Servis Optimizasyon Karar Destek Sistemi

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

![Program](https://github.com/furkan-o/Servis-Optimizasyon-KDS/blob/main/Picture1.png?raw=true)

Bu proje, çalışanların adreslerine göre optimum servis güzergahı ve en uygun servis araç kombinasyonunu belirleyen bir karar destek sistemidir.

## Özellikler

- Çalışan adreslerini JSON dosyalarından okur.
- Google Maps Distance Matrix API ile mesafe matrisi oluşturur.
- Genetik algoritma ile optimum servis rotası (Gezgin Satıcı Problemi) çözer.
- Servis araçlarının kapasite ve kilometre bazlı ücretlerini dikkate alarak en uygun maliyetli kombinasyonu bulur.
- Sonuçları Word veya metin dosyası olarak raporlayabilir.

## Kurulum

1. **Gereksinimler**
   - .NET 8.0 (veya üzeri)
   - Windows işletim sistemi
   - ~~[Google Maps Distance Matrix API](https://developers.google.com/maps/documentation/distance-matrix/overview) anahtarı~~
   - [OpenRouteService API](https://openrouteservice.org/dev/#/api-docs/v2/matrix/%7Bprofile%7D/post) anahtarı

2. **Bağımlılıklar**
   - [Xceed.Words.NET](https://github.com/xceedsoftware/DocX)
   - [Newtonsoft.Json](https://www.newtonsoft.com/json)
   - [DocX](https://github.com/xceedsoftware/DocX)

3. **Projeyi Çalıştırma**
   - Projeyi Visual Studio ile açın.
   - `form1.cs` dosyasındaki `MesafeMatrisiHesapla` fonksiyonunda `"API BURAYA"` kısmına kendi Google API anahtarınızı, `"ŞİRKET ADRESİ"` kısmına başlangıç adresini, `"İl bilgisi Ekle"` kısmına çalışılacak ili (adres verilerinde eksik varsa) girin.
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
`Yapısı:`
 {
    "kilometreAraligi": "10-20",
    "baslangicKm": 10,
    "bitisKm": 20,
    "servisUcretleri": {
      "19": 1000,
      "27": 2000,
      "46": 3000
    }
  },

- `[bolge].json` : Her bölge için çalışan adresleri.
`Yapısı:`
{
    "adSoyad": "Gordon Norman",
    "yaklasikAdres": "Hamidiye mah. Fatih cd."
  },
  {
    "adSoyad": "Samuel Serif",
    "yaklasikAdres": "Seyrantepe mah. Bahar sk."
  },

## Lisans

Bu proje [MIT Lisansı](LICENSE) ile lisanslanmıştır.

## Katkı

Katkıda bulunmak isterseniz lütfen bir pull request gönderin veya issue açın.

---

**Not:** Google API anahtarınızı kimseyle paylaşmayınız. API kullanımında kota ve ücretlendirme Google tarafından belirlenmektedir.
