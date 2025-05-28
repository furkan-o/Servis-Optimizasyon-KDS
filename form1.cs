using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows.Forms;
using Xceed.Document.NET;
using Xceed.Words.NET;


namespace ServisOptimizasyonSistemi
{
    #region ORS Model Sınıfları

    public class OrsGeocodeResponse
    {
        [JsonPropertyName("features")]
        public List<OrsFeature> Features { get; set; }
    }

    public class OrsFeature
    {
        [JsonPropertyName("geometry")]
        public OrsGeometry Geometry { get; set; }
    }

    public class OrsGeometry
    {
        [JsonPropertyName("coordinates")]
        public List<double> Coordinates { get; set; } // [boylam, enlem]
    }

    public class OrsMatrixRequest
    {
        [JsonPropertyName("locations")]
        public List<List<double>> Locations { get; set; }

        [JsonPropertyName("metrics")]
        public List<string> Metrics { get; set; } = new List<string> { "distance" }; 

        [JsonPropertyName("units")]
        public string Units { get; set; } = "km"; 
    }

    public class OrsMatrixResponse
    {
        [JsonPropertyName("distances")]
        public List<List<double?>> Distances { get; set; } // Ulaşılamayan rotalar için null olabilir
    }

    #endregion

    public partial class MainForm : Form
    {
        private List<string> bolgeler;
        private List<Calisan> calisanlar;
        private Dictionary<string, List<ServisUcret>> servisUcretleri;
        private double[,] mesafeMatrisi;
        private List<int> optimumRota;
        private double toplamMesafe;
        private Dictionary<string, List<ServisSecimi>> onbellek = new Dictionary<string, List<ServisSecimi>>(); 

        public MainForm()
        {
            InitializeComponent();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            try
            {
                // Bölgeleri yükle
                bolgeler = BolgeleriGetir();
                cmbBolgeler.Items.AddRange(bolgeler.ToArray());

                if (cmbBolgeler.Items.Count > 0)
                {
                    cmbBolgeler.SelectedIndex = 0;
                }

                // Servis ücretlerini yükle
                servisUcretleri = ServisUcretleriniOku();

                lblDurum.Text = "Sistem hazır. Lütfen bir bölge seçiniz.";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Sistem başlatılırken bir hata oluştu: {ex.Message}", "Hata",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void btnOptimizasyonYap_Click(object sender, EventArgs e)
        {
            if (cmbBolgeler.SelectedItem == null)
            {
                MessageBox.Show("Lütfen bir bölge seçiniz.", "Uyarı",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string seciliBolge = cmbBolgeler.SelectedItem.ToString();
            lblDurum.Text = $"{seciliBolge} bölgesi için optimizasyon yapılıyor...";
            Application.DoEvents(); 

            try
            {
                // Asenkron işlemleri çalıştırmak için bir Görev (Task) başlat
                Task.Run(async () =>
                {
                    // Bölgedeki çalışanları oku
                    calisanlar = CalisanlariOku(seciliBolge);

                    // UI thread'ine geri dönerek arayüzü güncelle
                    this.Invoke((MethodInvoker)delegate
                    {
                        lblCalisanSayisi.Text = calisanlar.Count.ToString();
                        lstCalisanlar.Items.Clear();
                        foreach (var calisan in calisanlar)
                        {
                            lstCalisanlar.Items.Add($"{calisan.AdSoyad} - {calisan.YaklasikAdres}");
                        }
                        lblDurum.Text = "Mesafe matrisi hesaplanıyor...";
                    });

                    // Mesafe matrisini hesapla
                    mesafeMatrisi = await MesafeMatrisiHesaplaAsync(calisanlar); 

                    this.Invoke((MethodInvoker)delegate
                    {
                        lblDurum.Text = "Genetik algoritma ile optimum rota belirleniyor...";
                    });

                    // Genetik algoritma ile en uygun rotayı bul
                    optimumRota = GezginSaticiProblemiCoz(mesafeMatrisi);
                    toplamMesafe = RotaMesafesiHesapla(optimumRota, mesafeMatrisi);

                    this.Invoke((MethodInvoker)delegate
                    {
                        lblToplamMesafe.Text = toplamMesafe.ToString("N2") + " km";
                        lblDurum.Text = "En uygun servis kombinasyonu hesaplanıyor...";
                    });

                    // En uygun servis kombinasyonunu belirle
                    var enUygunSecim = EnUygunServisSeciminiYap(calisanlar.Count, new List<double> { toplamMesafe }, servisUcretleri);

                    // Servis sayısını belirle
                    int toplamServisSayisi = enUygunSecim.Sum(s => s.ServisSayisi);

                    // Kapasite listesi oluştur
                    List<int> kapasiteListesi = new List<int>();
                    foreach (var secim in enUygunSecim)
                    {
                        for (int i = 0; i < secim.ServisSayisi; i++)
                        {
                            kapasiteListesi.Add(secim.ServisTipi);
                        }
                    }

                    this.Invoke((MethodInvoker)delegate
                    {
                        lblDurum.Text = $"Toplam {toplamServisSayisi} servis için rota optimizasyonu yapılıyor...";
                    });

                    // Çalışanların indeks listesini oluştur
                    var calisanIndeksListesi = Enumerable.Range(1, calisanlar.Count).ToList();
                    List<List<int>> sonRotalar = new List<List<int>>();
                    List<double> servisMesafeleri = new List<double>();

                    // Kapasite sıralamasına göre personeli parça parça ayırıp rota çıkar
                    int guncelIndeks = 0;
                    foreach (var kapasite in kapasiteListesi)
                    {
                        if (guncelIndeks >= calisanIndeksListesi.Count) break;
                        int alinanMiktar = Math.Min(kapasite, calisanIndeksListesi.Count - guncelIndeks);
                        List<int> grup = calisanIndeksListesi.GetRange(guncelIndeks, alinanMiktar);
                        guncelIndeks += alinanMiktar;

                        // Bu grup için tek rota optimizasyonu
                        var rota = TekilRotaOptimizeEt(grup, mesafeMatrisi);
                        sonRotalar.Add(rota);

                        // Bu grup için mesafeyi hesapla
                        double mesafe = RotaMesafesiHesapla(rota, mesafeMatrisi);
                        servisMesafeleri.Add(mesafe);
                    }

                    // UI'ya yazma
                    this.Invoke((MethodInvoker)delegate
                    {
                        lstOptimumRota.Items.Clear();
                        for (int i = 0; i < sonRotalar.Count; i++)
                        {
                            lstOptimumRota.Items.Add($"Servis {i + 1}");
                            lstOptimumRota.Items.Add("Şirket"); // Başlangıç noktası

                            foreach (var indeks in sonRotalar[i])
                            {
                                if (indeks > 0 && indeks <= calisanlar.Count)
                                {
                                    lstOptimumRota.Items.Add(calisanlar[indeks - 1].AdSoyad);
                                }
                            }
                            lstOptimumRota.Items.Add($"Mesafe: {servisMesafeleri[i]:N2} km");
                            lstOptimumRota.Items.Add("");
                        }

                        lblDurum.Text = "En uygun servis seçimi hesaplanıyor...";
                    });

                    try
                    {
                        // Sonuçları göster
                        this.Invoke((MethodInvoker)delegate
                        {
                            dgvSonuclar.Rows.Clear();
                            foreach (var secenek in enUygunSecim)
                            {
                                int satirIndeksi = dgvSonuclar.Rows.Add();
                                dgvSonuclar.Rows[satirIndeksi].Cells["colServisTipi"].Value = $"{secenek.ServisTipi} kişilik";
                                dgvSonuclar.Rows[satirIndeksi].Cells["colServisSayisi"].Value = secenek.ServisSayisi;
                                dgvSonuclar.Rows[satirIndeksi].Cells["colKisiSayisi"].Value = secenek.TasinanKisiSayisi;
                                dgvSonuclar.Rows[satirIndeksi].Cells["colBirimFiyat"].Value = secenek.BirimFiyat.ToString("C2");
                                dgvSonuclar.Rows[satirIndeksi].Cells["colToplamFiyat"].Value = secenek.ToplamFiyat.ToString("C2");
                            }

                            double toplamMaliyet = enUygunSecim.Sum(s => s.ToplamFiyat);
                            lblToplamMaliyet.Text = toplamMaliyet.ToString("C2");

                            lblDurum.Text = "Optimizasyon tamamlandı.";
                            tabControl1.SelectedIndex = 2; // Sonuçlar sekmesine geç
                        });
                    }
                    catch (Exception ex)
                    {
                        this.Invoke((MethodInvoker)delegate
                        {
                            MessageBox.Show($"Servis seçimi sırasında bir hata oluştu: {ex.Message}", "Hata",
                                MessageBoxButtons.OK, MessageBoxIcon.Error);
                            lblDurum.Text = "Hata oluştu.";
                        });
                    }
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Optimizasyon sırasında bir hata oluştu: {ex.Message}", "Hata",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                lblDurum.Text = "Hata oluştu.";
            }
        }


        /// Temel Gezgin Satıcı Problemi için rota optimizasyonu yapar
        private List<int> TekilRotaOptimizeEt(List<int> altIndeksler, double[,] anaMesafeMatrisi)
        {
            // Yerel matrisi oluşturmak için bir haritalama yapar
            Dictionary<int, int> yerelHarita = new Dictionary<int, int>();
            for (int i = 0; i < altIndeksler.Count; i++)
            {
                yerelHarita[altIndeksler[i]] = i + 1; // 1-tabanlı indeksleme
            }

            // Şirket (0. indeks) dahil olmak üzere yerel indeks sayısını al
            int altGrupBoyutu = altIndeksler.Count;
            double[,] yerelMatris = new double[altGrupBoyutu + 1, altGrupBoyutu + 1];

            // Yerel matrisi doldur
            for (int i = 0; i <= altGrupBoyutu; i++)
            {
                for (int j = 0; j <= altGrupBoyutu; j++)
                {
                    if (i == j)
                    {
                        yerelMatris[i, j] = 0;
                        continue;
                    }

                    if (i == 0) // Şirketten çalışana
                    {
                        int genelJIndeksi = altIndeksler[j - 1]; // j yerel indeksi (1-tabanlı)
                        yerelMatris[i, j] = anaMesafeMatrisi[0, genelJIndeksi];
                    }
                    else if (j == 0) // Çalışandan şirkete
                    {
                        int genelIIndeksi = altIndeksler[i - 1]; // i yerel indeksi (1-tabanlı)
                        yerelMatris[i, j] = anaMesafeMatrisi[genelIIndeksi, 0];
                    }
                    else // Çalışandan çalışana
                    {
                        int genelIIndeksi = altIndeksler[i - 1];
                        int genelJIndeksi = altIndeksler[j - 1];
                        yerelMatris[i, j] = anaMesafeMatrisi[genelIIndeksi, genelJIndeksi];
                    }
                }
            }

            // Tek araçlı gezgin satıcı problemini çöz
            var yerelRota = TekilGezginSaticiProblemiCalistir(yerelMatris, altGrupBoyutu);

            // Yerel rotayı genel ana rotaya dönüştür
            // yerelRota, yerel indeksler (1..altGrupBoyutu) olarak döner
            // Genel indekslere dönüştürmek için altIndeksler[yerelIndeks-1] kullanılır
            List<int> nihaiRota = new List<int>();
            foreach (var yerelIndeks in yerelRota)
            {
                // yerelIndeks=1..altGrupBoyutu => altIndeksler[yerelIndeks-1]
                nihaiRota.Add(altIndeksler[yerelIndeks - 1]);
            }
            return nihaiRota;
        }


        /// Tekil Gezgin Satıcı Problemi (TSP) çözümü için genetik algoritmayı çalıştıran fonksiyon.
        private List<int> TekilGezginSaticiProblemiCalistir(double[,] yerelMatris, int boyut)
        {
            // Genetik algoritma parametreleri
            int populasyonBuyuklugu = 250;
            int jenerasyonSayisi = 500;
            double caprazlamaOrani = 0.9;
            double mutasyonOrani = 0.1; 
            double elitizmOrani = 0.05;

            // Başlangıç populasyonunu oluştur
            List<List<int>> populasyon = new List<List<int>>();
            Random rastgele = new Random();

            for (int i = 0; i < populasyonBuyuklugu; i++)
            {
                // Rastgele bir rota oluştur (1'den boyuta kadar olan sayılarla)
                List<int> rota = Enumerable.Range(1, boyut).ToList();

                // Rotayı karıştır (Fisher-Yates shuffle)
                for (int j = 0; j < boyut; j++)
                {
                    int k = rastgele.Next(j, boyut);
                    int gecici = rota[j];
                    rota[j] = rota[k];
                    rota[k] = gecici;
                }
                populasyon.Add(rota);
            }

            // Jenerasyonları çalıştır
            for (int jenerasyonNo = 0; jenerasyonNo < jenerasyonSayisi; jenerasyonNo++)
            {
                // Her bireyin uygunluk değerini hesapla
                Dictionary<int, double> uygunlukHaritasi = new Dictionary<int, double>();
                for (int i = 0; i < populasyon.Count; i++)
                {
                    double mesafe = RotaMesafesiHesaplaLocal(populasyon[i], yerelMatris);
                    uygunlukHaritasi[i] = (mesafe <= 0) ? 0.0 : 1.0 / mesafe; // Mesafe ne kadar küçükse uygunluk o kadar büyük
                }

                List<List<int>> yeniPopulasyon = new List<List<int>>();

                // En iyi bireyleri doğrudan yeni populasyona aktar
                int elitSayisi = (int)(populasyonBuyuklugu * elitizmOrani);
                if (elitSayisi > 0 && uygunlukHaritasi.Any())
                {
                    var enIyiBireyler = uygunlukHaritasi.OrderByDescending(x => x.Value)
                                                 .Take(elitSayisi)
                                                 .Select(x => new List<int>(populasyon[x.Key]));
                    yeniPopulasyon.AddRange(enIyiBireyler);
                }

                // Yeni populasyonun geri kalanını doldur
                while (yeniPopulasyon.Count < populasyonBuyuklugu)
                {
                    // Ebeveyn seçimi (Turnuva Seçimi)
                    int ebeveyn1Indeks = TurnuvaSecimi(uygunlukHaritasi, rastgele, populasyon);
                    int ebeveyn2Indeks = TurnuvaSecimi(uygunlukHaritasi, rastgele, populasyon);

                    List<int> cocuk;
                    // Çaprazlama
                    if (rastgele.NextDouble() < caprazlamaOrani && boyut > 1)
                    {
                        cocuk = Caprazla(new List<int>(populasyon[ebeveyn1Indeks]), new List<int>(populasyon[ebeveyn2Indeks]), rastgele);
                    }
                    else // Çaprazlama yapılmazsa daha iyi olan ebeveyni al
                    {
                        cocuk = new List<int>(uygunlukHaritasi[ebeveyn1Indeks] > uygunlukHaritasi[ebeveyn2Indeks] ? populasyon[ebeveyn1Indeks] : populasyon[ebeveyn2Indeks]);
                    }

                    // Mutasyon
                    if (rastgele.NextDouble() < mutasyonOrani && boyut > 1)
                    {
                        MutasyonUygula(cocuk, rastgele);
                    }
                    yeniPopulasyon.Add(cocuk);
                }
                populasyon = yeniPopulasyon; // Eski populasyonu yenisiyle değiştir
            }

            // Son populasyondaki en iyi bireyi bul
            Dictionary<int, double> sonUygunlukHaritasi = new Dictionary<int, double>();
            for (int i = 0; i < populasyon.Count; i++)
            {
                double mesafe = RotaMesafesiHesaplaLocal(populasyon[i], yerelMatris);
                sonUygunlukHaritasi[i] = mesafe;
            }

            if (!sonUygunlukHaritasi.Any()) return new List<int>(); // Uygun birey yoksa boş liste döndür

            int enIyiSonIndeks = sonUygunlukHaritasi.OrderBy(x => x.Value).First().Key;
            return populasyon[enIyiSonIndeks];
        }


        private double RotaMesafesiHesaplaLocal(List<int> rota, double[,] yerelMatris)
        {
            double toplamMesafe = 0;
            if (rota == null || rota.Count == 0) return double.MaxValue;

            // Rota başlangıç noktasından (şirket, indeks 0) ilk durağa olan mesafeyi ekle
            toplamMesafe += yerelMatris[0, rota[0]];
            // Duraklar arası mesafeleri ekle
            for (int i = 0; i < rota.Count - 1; i++)
            {
                toplamMesafe += yerelMatris[rota[i], rota[i + 1]];
            }
            return toplamMesafe;
        }


        private void btnRaporOlustur_Click(object sender, EventArgs e)
        {
            if (optimumRota == null || calisanlar == null)
            {
                MessageBox.Show("Henüz optimizasyon yapılmadı.", "Uyarı",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            SaveFileDialog kaydetDialogu = new SaveFileDialog();
            kaydetDialogu.Filter = "Word Dosyası|*.docx|Metin Dosyası|*.txt";
            kaydetDialogu.Title = "Raporu Kaydet";

            if (kaydetDialogu.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    string dosyaYolu = kaydetDialogu.FileName;
                    string uzanti = Path.GetExtension(dosyaYolu).ToLower();

                    switch (uzanti)
                    {
                        case ".docx":
                            RaporuWordOlarakKaydet(dosyaYolu);
                            break;
                        case ".txt":
                            RaporuMetinOlarakKaydet(dosyaYolu);
                            break;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Rapor oluşturulurken bir hata oluştu: {ex.Message}", "Hata",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        #region Veri Okuma İşlemleri

        private List<string> BolgeleriGetir()
        {
            // Çalışma dizinindeki ".json" uzantılı dosyaları al, "servisucretleri.json" hariç
            return Directory.GetFiles(".", "*.json")
                .Where(dosya => !Path.GetFileName(dosya).Equals("servisucretleri.json", StringComparison.OrdinalIgnoreCase))
                .Select(dosya => Path.GetFileNameWithoutExtension(dosya)) 
                .ToList();
        }

        private List<Calisan> CalisanlariOku(string bolgeAdi)
        {
            string dosyaYolu = $"{bolgeAdi}.json";
            string jsonIcerik = File.ReadAllText(dosyaYolu);
            return JsonSerializer.Deserialize<List<Calisan>>(jsonIcerik,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }); 
        }

        private Dictionary<string, List<ServisUcret>> ServisUcretleriniOku()
        {
            string dosyaYolu = "servisucretleri.json";
            string jsonIcerik = File.ReadAllText(dosyaYolu);
            var kilometreGruplari = JsonSerializer.Deserialize<List<KilometreGrubu>>(jsonIcerik,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            var ucretSozlugu = new Dictionary<string, List<ServisUcret>>();
            foreach (var grup in kilometreGruplari)
            {
                string grupAdi = $"{grup.BaslangicKm}-{grup.BitisKm}"; 
                var ucretler = new List<ServisUcret>
                {
                    new ServisUcret { Kapasite = 19, Ucret = grup.ServisUcretleri._19 },
                    new ServisUcret { Kapasite = 27, Ucret = grup.ServisUcretleri._27 },
                    new ServisUcret { Kapasite = 46, Ucret = grup.ServisUcretleri._46 }
                };
                ucretSozlugu.Add(grupAdi, ucretler);
            }
            return ucretSozlugu;
        }

        #endregion

        #region Mesafe Matrisi İşlemleri (OpenRouteService ile)

        private async Task<List<double>> AdresIcinKoordinatAlAsync(string adres, string orsApiAnahtari, HttpClient istemci)
        {
            string jeokodlamaUrl = $"https://api.openrouteservice.org/geocode/search?api_key={orsApiAnahtari}&text={Uri.EscapeDataString(adres)}&size=1";
            try
            {
                HttpResponseMessage yanit = await istemci.GetAsync(jeokodlamaUrl);
                if (yanit.IsSuccessStatusCode)
                {
                    var jeokodlamaYaniti = await yanit.Content.ReadFromJsonAsync<OrsGeocodeResponse>();
                    if (jeokodlamaYaniti != null && jeokodlamaYaniti.Features != null && jeokodlamaYaniti.Features.Count > 0)
                    {
                        return jeokodlamaYaniti.Features[0].Geometry.Coordinates;
                    }
                }
                else
                {
                    Console.WriteLine($"'{adres}' için jeokodlama başarısız oldu: {yanit.StatusCode} - {await yanit.Content.ReadAsStringAsync()}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"'{adres}' için jeokodlama istisnası: {ex.Message}");
            }
            return null; 
        }

        private async Task<double[,]> MesafeMatrisiHesaplaAsync(List<Calisan> calisanListesi) 
        {
            int calisanSayisi = calisanListesi.Count;
            // Matris boyutu: şirket (0. indeks) + çalışan sayısı
            double[,] hesaplananMesafeMatrisi = new double[calisanSayisi + 1, calisanSayisi + 1];

            // OpenRouteService API Anahtarı (Kendi anahtarınızla değiştir)
            string orsApiAnahtari = "API BURAYA";


            using (HttpClient istemci = new HttpClient())
            {
                istemci.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                istemci.DefaultRequestHeaders.UserAgent.ParseAdd("ServisOptimizasyonSistemi/1.0"); 

                List<string> tumAdresler = new List<string>
                {
                    // Şirket adresi (0. indeks)
                    "ŞİRKET ADRESİ"
                };
                foreach (var calisan in calisanListesi)
                {
                    tumAdresler.Add(calisan.YaklasikAdres + ", İl bilgisi Ekle"); // Adres yazmayı bilmeyen çalışanlar için il verisini manüel ekle
                }

                List<List<double>> konumKoordinatlari = new List<List<double>>();
                this.Invoke((MethodInvoker)delegate { lblDurum.Text = "Adresler koordinatlara çevriliyor..."; });

                for (int i = 0; i < tumAdresler.Count; i++)
                {
                    this.Invoke((MethodInvoker)delegate { lblDurum.Text = $"Koordinat alınıyor: {i + 1}/{tumAdresler.Count}..."; });
                    List<double> koordinatlar = await AdresIcinKoordinatAlAsync(tumAdresler[i], orsApiAnahtari, istemci);
                    if (koordinatlar != null)
                    {
                        konumKoordinatlari.Add(koordinatlar);
                    }
                    else
                    {
                        // Bir adres için jeokodlama hatasını işle:
                        // Seçenek 1: Hata göster ve durdur.
                        // Seçenek 2: Bu adresi içeren rotalar için çok büyük bir mesafe kullan.
                        // Seçenek 3: Bu adresi atla (calisanlar listesini ve indeksleri ayarlamayı gerektirebilir).
                        this.Invoke((MethodInvoker)delegate
                        {
                            MessageBox.Show($"'{tumAdresler[i]}' adresi için koordinat bulunamadı. Bu adres optimizasyon dışı kalabilir veya hatalara yol açabilir.", "Jeokodlama Hatası", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        });
                        // Basitlik için, muhtemelen sorunlara yol açacak veya daha sonra MaxValue ile işlenecek bir yer tutucu
                        // Daha sağlam bir çözüm, bu konumu kullanılamaz olarak işaretlemek olacaktır.
                        konumKoordinatlari.Add(new List<double> { 0, 0 }); // Geçersiz yer tutucu
                    }
                    // ORS ücretsiz katman hız sınırı 
                    await Task.Delay(250);
                }

                // Başarısız jeokodlama için temel kontrol
                if (konumKoordinatlari.Count != tumAdresler.Count || konumKoordinatlari.Any(k => k[0] == 0 && k[1] == 0 && !tumAdresler[konumKoordinatlari.IndexOf(k)].Contains("0,0")))
                {
                    this.Invoke((MethodInvoker)delegate
                    {
                        MessageBox.Show($"Tüm adresler için koordinat alınamadı. Mesafe matrisi hesaplanamıyor.", "Kritik Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        lblDurum.Text = "Koordinat hatası. İşlem durduruldu.";
                    });
                    return hesaplananMesafeMatrisi; 
                }

                if (konumKoordinatlari.Count > 50)
                {
                    this.Invoke((MethodInvoker)delegate
                    {
                        MessageBox.Show("OpenRouteService ücretsiz planı en fazla 50 konumu destekler. Mevcut konum sayısı: " + konumKoordinatlari.Count, "API Sınırı Aşıldı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        lblDurum.Text = "API sınırı aşıldı.";
                    });
                    // Başarısızlığı veya kısmi veriyi belirtmek için MaxValue ile başlat
                    for (int i = 0; i < hesaplananMesafeMatrisi.GetLength(0); i++)
                        for (int j = 0; j < hesaplananMesafeMatrisi.GetLength(1); j++)
                            hesaplananMesafeMatrisi[i, j] = (i == j) ? 0 : double.MaxValue;
                    return hesaplananMesafeMatrisi;
                }

                this.Invoke((MethodInvoker)delegate { lblDurum.Text = "Mesafe matrisi OpenRouteService'den alınıyor..."; });

                OrsMatrixRequest matrisIstekVerisi = new OrsMatrixRequest
                {
                    Locations = konumKoordinatlari 
                };

                string matrisApiUrl = "https://api.openrouteservice.org/v2/matrix/driving-car";
                HttpContent icerik = new StringContent(JsonSerializer.Serialize(matrisIstekVerisi), Encoding.UTF8, "application/json");
                istemci.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(orsApiAnahtari); // API anahtarını yetkilendirme başlığına ekle

                try
                {
                    HttpResponseMessage matrisYanitMesaji = await istemci.PostAsync(matrisApiUrl, icerik);

                    if (matrisYanitMesaji.IsSuccessStatusCode)
                    {
                        var orsMatrisYaniti = await matrisYanitMesaji.Content.ReadFromJsonAsync<OrsMatrixResponse>();
                        if (orsMatrisYaniti != null && orsMatrisYaniti.Distances != null)
                        {
                            for (int i = 0; i < orsMatrisYaniti.Distances.Count; i++)
                            {
                                for (int j = 0; j < orsMatrisYaniti.Distances[i].Count; j++)
                                {
                                    if (i < hesaplananMesafeMatrisi.GetLength(0) && j < hesaplananMesafeMatrisi.GetLength(1))
                                    {
                                        // Ulaşılamayan rotalar için null gelebilir, bu durumda MaxValue ata
                                        hesaplananMesafeMatrisi[i, j] = orsMatrisYaniti.Distances[i][j] ?? double.MaxValue;
                                    }
                                }
                            }
                        }
                        else
                        {
                            this.Invoke((MethodInvoker)delegate { MessageBox.Show("ORS Matris API'sinden geçersiz yanıt.", "API Hatası", MessageBoxButtons.OK, MessageBoxIcon.Error); });
                        }
                    }
                    else
                    {
                        string hataIcerigi = await matrisYanitMesaji.Content.ReadAsStringAsync();
                        this.Invoke((MethodInvoker)delegate { MessageBox.Show($"ORS Matris API hatası: {matrisYanitMesaji.StatusCode} - {hataIcerigi}", "API Hatası", MessageBoxButtons.OK, MessageBoxIcon.Error); });
                    }
                }
                catch (Exception ex)
                {
                    this.Invoke((MethodInvoker)delegate { MessageBox.Show($"Mesafe matrisi alınırken bir hata oluştu: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error); });
                }
            }
            this.Invoke((MethodInvoker)delegate { lblDurum.Text = "Mesafe matrisi hesaplandı."; });
            return hesaplananMesafeMatrisi;
        }
        #endregion

        #region Genetik Algoritma İşlemleri

        private bool RotaGecerliMi(List<int> rota, int kapasite)
        {
            int rotadakiToplamCalisanSayisi = rota.Count;
            return rotadakiToplamCalisanSayisi <= kapasite;
        }

        private List<int> GezginSaticiProblemiCoz(double[,] anaMesafeMatrisi) 
        {
            int boyut = anaMesafeMatrisi.GetLength(0) - 1; // Şirket hariç durak sayısı
            if (boyut <= 0) return new List<int>();

            int populasyonBuyuklugu = 200;
            int jenerasyonSayisi = 500;
            double caprazlamaOrani = 0.9;
            double mutasyonOrani = 0.15;
            double elitizmOrani = 0.05;

            List<List<int>> populasyon = new List<List<int>>();
            Random rastgele = new Random();

            // Başlangıç populasyonunu oluştur
            for (int i = 0; i < populasyonBuyuklugu; i++)
            {
                List<int> rota = Enumerable.Range(1, boyut).ToList(); // 1'den boyuta kadar (çalışan indeksleri)
                // Rotayı karıştır
                for (int j = rota.Count - 1; j > 0; j--)
                {
                    int k = rastgele.Next(j + 1);
                    (rota[j], rota[k]) = (rota[k], rota[j]); // Değiş tokuş
                }
                populasyon.Add(rota);
            }

            // Jenerasyonları çalıştır
            for (int jenerasyonNo = 0; jenerasyonNo < jenerasyonSayisi; jenerasyonNo++)
            {
                Dictionary<int, double> uygunlukHaritasi = new Dictionary<int, double>();
                for (int i = 0; i < populasyon.Count; i++)
                {
                    double mesafe = RotaMesafesiHesapla(populasyon[i], anaMesafeMatrisi);
                    // Mesafe 0 veya geçersizse uygunluk 0, aksi halde 1/mesafe
                    uygunlukHaritasi[i] = (mesafe <= 0 || double.IsInfinity(mesafe) || double.IsNaN(mesafe) || mesafe == double.MaxValue) ? 0.0 : 1.0 / mesafe;
                }

                List<List<int>> yeniPopulasyon = new List<List<int>>();
                int elitSayisi = (int)(populasyonBuyuklugu * elitizmOrani);

                // Elitizm
                if (elitSayisi > 0 && uygunlukHaritasi.Any())
                {
                    var enIyiBireyler = uygunlukHaritasi.OrderByDescending(x => x.Value)
                                             .Take(elitSayisi)
                                             .Select(x => new List<int>(populasyon[x.Key])); // Kopyala
                    yeniPopulasyon.AddRange(enIyiBireyler);
                }

                // Yeni populasyonun geri kalanını doldur
                while (yeniPopulasyon.Count < populasyonBuyuklugu)
                {
                    int ebeveyn1Indeks = TurnuvaSecimi(uygunlukHaritasi, rastgele, populasyon);
                    int ebeveyn2Indeks = TurnuvaSecimi(uygunlukHaritasi, rastgele, populasyon);

                    List<int> cocuk;
                    if (rastgele.NextDouble() < caprazlamaOrani && boyut > 1)
                    {
                        cocuk = Caprazla(new List<int>(populasyon[ebeveyn1Indeks]), new List<int>(populasyon[ebeveyn2Indeks]), rastgele);
                    }
                    else
                    {
                        // Daha iyi olan ebeveyni seç
                        cocuk = new List<int>(uygunlukHaritasi.ContainsKey(ebeveyn1Indeks) && uygunlukHaritasi.ContainsKey(ebeveyn2Indeks) && uygunlukHaritasi[ebeveyn1Indeks] > uygunlukHaritasi[ebeveyn2Indeks] ? populasyon[ebeveyn1Indeks] : populasyon[ebeveyn2Indeks]);
                    }

                    if (rastgele.NextDouble() < mutasyonOrani && boyut > 1)
                    {
                        MutasyonUygula(cocuk, rastgele);
                    }
                    yeniPopulasyon.Add(cocuk);
                }
                populasyon = yeniPopulasyon;
            }

            // Son populasyondaki en iyi rotayı bul
            Dictionary<int, double> sonUygunlukHaritasi = new Dictionary<int, double>();
            for (int i = 0; i < populasyon.Count; i++)
            {
                double mesafe = RotaMesafesiHesapla(populasyon[i], anaMesafeMatrisi);
                sonUygunlukHaritasi[i] = mesafe;
            }

            if (!sonUygunlukHaritasi.Any() || sonUygunlukHaritasi.All(kvp => kvp.Value == double.MaxValue))
            {
                this.Invoke((MethodInvoker)delegate {
                    MessageBox.Show("Optimum rota bulunamadı. Tüm mesafeler çok yüksek veya hesaplanamadı.", "TSP Hatası", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                });
                return new List<int>(); // Geçerli rota bulunamazsa boş liste döndür
            }

            int enIyiSonIndeks = sonUygunlukHaritasi.OrderBy(x => x.Value).First().Key;
            return populasyon[enIyiSonIndeks];
        }

        private int TurnuvaSecimi(Dictionary<int, double> uygunlukHaritasi, Random rastgele, List<List<int>> mevcutPopulasyon) // Parametre adı güncellendi
        {
            int turnuvaBoyutu = 3;
            if (!uygunlukHaritasi.Any())
            {
                // Bu durum idealde oluşmamalıdır.
                if (mevcutPopulasyon.Any()) return rastgele.Next(mevcutPopulasyon.Count); // Yedek çözüm, ideal değil
                throw new InvalidOperationException("Uygunluk haritası boş, turnuva seçimi yapılamaz.");
            }
            if (uygunlukHaritasi.Count < turnuvaBoyutu) turnuvaBoyutu = uygunlukHaritasi.Count;

            List<int> adayIndeksleri = new List<int>();
            List<int> populasyonAnahtarlari = uygunlukHaritasi.Keys.ToList();

            for (int i = 0; i < turnuvaBoyutu; i++)
            {
                adayIndeksleri.Add(populasyonAnahtarlari[rastgele.Next(populasyonAnahtarlari.Count)]);
            }
            // Adaylar arasından en yüksek uygunluğa sahip olanı seç
            return adayIndeksleri.OrderByDescending(indeks => uygunlukHaritasi[indeks]).First();
        }

        private List<int> Caprazla(List<int> ebeveyn1, List<int> ebeveyn2, Random rastgele)
        {
            int boyut = ebeveyn1.Count;
            if (boyut == 0) return new List<int>();
            if (boyut == 1) return new List<int>(ebeveyn1); // Tek elemanlıysa kopyasını döndür

            List<int> cocuk = new List<int>(new int[boyut]); // Boş çocuk listesi
            bool[] cocuktaVar = new bool[boyut + 2];

            int baslangic = rastgele.Next(boyut);
            int bitis = rastgele.Next(boyut);

            if (baslangic > bitis) (baslangic, bitis) = (bitis, baslangic); // baslangic <= bitis sağla

            // Ebeveyn1'den bir bölümü çocuğa kopyala
            for (int i = baslangic; i <= bitis; i++)
            {
                cocuk[i] = ebeveyn1[i];
                if (ebeveyn1[i] >= 0 && ebeveyn1[i] < cocuktaVar.Length) cocuktaVar[ebeveyn1[i]] = true;
            }

            // Ebeveyn2'den kalan elemanları çocuğa ekle
            int cocukIndeks = (bitis + 1) % boyut;
            for (int i = 0; i < boyut; i++)
            {
                int ebeveyn2ElemanIndeksi = (bitis + 1 + i) % boyut; 
                int eleman = ebeveyn2[ebeveyn2ElemanIndeksi];

                bool elemanCocuktaMevcut = false;
                if (eleman >= 0 && eleman < cocuktaVar.Length) elemanCocuktaMevcut = cocuktaVar[eleman];

                if (!elemanCocuktaMevcut)
                {
                    // Atamadan önce cocukIndeks'in geçerli olduğundan emin olun
                    if (cocukIndeks < cocuk.Count)
                    {
                        cocuk[cocukIndeks] = eleman;
                        if (eleman >= 0 && eleman < cocuktaVar.Length) cocuktaVar[eleman] = true;
                    }
                    cocukIndeks = (cocukIndeks + 1) % boyut;
                }
            }
            return cocuk;
        }

        private void MutasyonUygula(List<int> rota, Random rastgele) 
        {
            int boyut = rota.Count;
            if (boyut < 2) return; // Mutasyon için en az 2 eleman gerekli

            // İki rastgele indeks seç
            int indeks1 = rastgele.Next(boyut);
            int indeks2 = rastgele.Next(boyut);

            // İndeksler aynıysa, farklı bir ikinci indeks seç (ters çevirme mutasyonu için)
            if (indeks1 == indeks2)
            {
                if (boyut > 1) indeks2 = (indeks1 + 1 + rastgele.Next(boyut - 1)) % boyut;
                else return;
            }

            // İndeksleri sırala (baslangic <= bitis)
            int baslangic = Math.Min(indeks1, indeks2);
            int bitis = Math.Max(indeks1, indeks2);

            // Seçilen aralıktaki elemanları ters çevir
            while (baslangic < bitis)
            {
                (rota[baslangic], rota[bitis]) = (rota[bitis], rota[baslangic]); // Değiş tokuş
                baslangic++;
                bitis--;
            }
        }

        private double RotaMesafesiHesapla(List<int> rota, double[,] anaMesafeMatrisi) 
        {
            double toplamMesafe = 0;
            if (rota == null || !rota.Any()) return double.MaxValue;

            // Matris sınırlarını kontrol et
            int matrisBoyutu = anaMesafeMatrisi.GetLength(0);
            if (matrisBoyutu == 0) return double.MaxValue;
            // Rotadaki indekslerin geçerliliğini kontrol et
            if (rota.Any(indeks => indeks < 0 || indeks >= matrisBoyutu)) return double.MaxValue; // Geçersiz indeks

            // Şirketten (0. indeks) ilk durağa olan mesafe
            if (0 >= matrisBoyutu || rota[0] >= matrisBoyutu) return double.MaxValue; // Sınır kontrolü
            toplamMesafe += anaMesafeMatrisi[0, rota[0]];

            // Duraklar arası mesafeler
            for (int i = 0; i < rota.Count - 1; i++)
            {
                if (rota[i] >= matrisBoyutu || rota[i + 1] >= matrisBoyutu) return double.MaxValue; // Sınır kontrolü
                toplamMesafe += anaMesafeMatrisi[rota[i], rota[i + 1]];
            }
            return toplamMesafe;
        }

        private List<ServisSecimi> ServisSeciminiOptimizeEt(int kalanCalisanSayisi, int[] kapasiteler, double rotaMesafesiKm)
        {
            string anahtar = $"{kalanCalisanSayisi}_{rotaMesafesiKm.ToString(System.Globalization.CultureInfo.InvariantCulture)}";
            if (onbellek.ContainsKey(anahtar)) return onbellek[anahtar];

            if (kalanCalisanSayisi == 0) return new List<ServisSecimi>(); 
            if (kalanCalisanSayisi < 0) return null; 

            List<ServisSecimi> enIyiGenelKombinasyon = null;
            double enIyiKombinasyonIcinMinMaliyet = double.MaxValue;

            // Önce daha büyük kapasiteleri dene
            foreach (var kapasite in kapasiteler.OrderByDescending(c => c))
            {
                // Mevcut kapasite tam olarak kullanılabiliyorsa
                if (kalanCalisanSayisi >= kapasite)
                {
                    var altProblemSonucu = ServisSeciminiOptimizeEt(kalanCalisanSayisi - kapasite, kapasiteler, rotaMesafesiKm);
                    // Kalan kısım için geçerli bir çözüm varsa
                    if (altProblemSonucu != null)
                    {
                        double mevcutSecimMaliyeti = KapasiteIcinMaliyetAl(kapasite, rotaMesafesiKm);
                        if (mevcutSecimMaliyeti == double.MaxValue) continue; // Maliyet geçersizse atla

                        double mevcutKombinasyonMaliyeti = mevcutSecimMaliyeti + altProblemSonucu.Sum(sr => sr.ToplamFiyat);

                        if (enIyiGenelKombinasyon == null || mevcutKombinasyonMaliyeti < enIyiKombinasyonIcinMinMaliyet)
                        {
                            enIyiKombinasyonIcinMinMaliyet = mevcutKombinasyonMaliyeti;
                            enIyiGenelKombinasyon = new List<ServisSecimi>
                            {
                                // BirimFiyat'ın bu tek servis için toplam maliyet olduğunu varsayarsak
                                new ServisSecimi { ServisTipi = kapasite, ServisSayisi = 1, TasinanKisiSayisi = kapasite, ToplamFiyat = mevcutSecimMaliyeti, BirimFiyat = mevcutSecimMaliyeti }
                            };
                            enIyiGenelKombinasyon.AddRange(altProblemSonucu);
                        }
                    }
                }
                // Mevcut kapasite kalan çalışanlardan büyükse ancak onları kapsayabiliyorsa
                else if (kalanCalisanSayisi > 0 && kapasite >= kalanCalisanSayisi)
                {
                    double mevcutSecimMaliyeti = KapasiteIcinMaliyetAl(kapasite, rotaMesafesiKm);
                    if (mevcutSecimMaliyeti == double.MaxValue) continue; // Maliyet geçersizse atla

                    if (enIyiGenelKombinasyon == null || mevcutSecimMaliyeti < enIyiKombinasyonIcinMinMaliyet)
                    {
                        enIyiKombinasyonIcinMinMaliyet = mevcutSecimMaliyeti;
                        enIyiGenelKombinasyon = new List<ServisSecimi>
                        {
                            new ServisSecimi { ServisTipi = kapasite, ServisSayisi = 1, TasinanKisiSayisi = kalanCalisanSayisi, ToplamFiyat = mevcutSecimMaliyeti, BirimFiyat = mevcutSecimMaliyeti }
                        };
                    }
                }
            }
            // Sonucu belleğe al (çözüm bulunamazsa null olabilir)
            onbellek[anahtar] = enIyiGenelKombinasyon;
            return enIyiGenelKombinasyon;
        }


        private double KapasiteIcinMaliyetAl(int kapasite, double rotaMesafesiKm)
        {
            // Ücret hesaplama mantığı için rotaMesafesiKm'nin pozitif olduğundan emin olun
            double mevcutRotaMesafesi = (rotaMesafesiKm <= 0) ? 1.0 : rotaMesafesiKm;

            if (this.servisUcretleri == null || !this.servisUcretleri.Any())
            {
                Console.WriteLine("Servis ücretleri yüklenmemiş.");
                return double.MaxValue; 
            }

            // Rota mesafesine uygun kilometre grubunu bul
            string kilometreGrubuAnahtari = this.servisUcretleri.Keys.FirstOrDefault(grupAnahtari =>
            {
                string[] aralik = grupAnahtari.Split('-');
                return aralik.Length == 2 &&
                       int.TryParse(aralik[0], out int baslangic) &&
                       int.TryParse(aralik[1], out int bitis) &&
                       mevcutRotaMesafesi >= baslangic && mevcutRotaMesafesi <= bitis;
            });

            if (string.IsNullOrEmpty(kilometreGrubuAnahtari))
            {
                // Yedek mantık: doğrudan eşleşme yoksa en uygun grubu bulmaya çalışın
                // Bu genellikle mevcutRotaMesafesi tüm aralıkları aşarsa en yüksek tanımlanmış aralığı kullanmak,
                // veya tüm aralıkların altındaysa en düşük tanımlanmış aralığı kullanmak anlamına gelir.
                // Mevcut yedek (EnUygunServisSeciminiYap'a benzer şekilde) en büyük BitisKm'ye sahip grubu seçer.
                kilometreGrubuAnahtari = this.servisUcretleri.Keys
                    .Select(g => new { Anahtar = g, Parcalar = g.Split('-') })
                    .Where(g => g.Parcalar.Length == 2 && int.TryParse(g.Parcalar[0], out _) && int.TryParse(g.Parcalar[1], out _))
                    .OrderByDescending(g => int.Parse(g.Parcalar[1])) // BitisKm'ye göre azalan sırada sırala
                    .FirstOrDefault()?.Anahtar;

                // Hala anahtar yoksa, mutlak ilk olanı almayı dene
                if (string.IsNullOrEmpty(kilometreGrubuAnahtari))
                {
                    kilometreGrubuAnahtari = this.servisUcretleri.Keys.FirstOrDefault();
                }

                if (string.IsNullOrEmpty(kilometreGrubuAnahtari))
                {
                    Console.WriteLine($"Servis ücretleri tanımlı değil veya uygun bir kilometre grubu bulunamadı. Rota mesafesi: {mevcutRotaMesafesi}");
                    return double.MaxValue;
                }
                // Yedek grubun kullanıldığını günlüğe kaydet
                Console.WriteLine($"Rota mesafesi ({mevcutRotaMesafesi} km) için tam kilometre grubu bulunamadı. Alternatif grup: {kilometreGrubuAnahtari} kullanılıyor.");
            }

            if (this.servisUcretleri.TryGetValue(kilometreGrubuAnahtari, out var ucretlerListesi))
            {
                var servisUcretBilgisi = ucretlerListesi.FirstOrDefault(u => u.Kapasite == kapasite);
                if (servisUcretBilgisi != null)
                {
                    return servisUcretBilgisi.Ucret;
                }
                else
                {
                    Console.WriteLine($"'{kilometreGrubuAnahtari}' kilometre grubu için {kapasite} kişilik servis ücreti bulunamadı.");
                    return double.MaxValue;
                }
            }
            else
            {
                Console.WriteLine($"Beklenmedik hata: Kilometre grubu '{kilometreGrubuAnahtari}' servis ücretleri sözlüğünde bulunamadı.");
                return double.MaxValue;
            }
        }

        private List<ServisSecimi> EnUygunServisSeciminiYap(int calisanSayisi, List<double> servisMesafeListesi, Dictionary<string, List<ServisUcret>> servisUcretleriSozlugu) // Parametre adları güncellendi
        {
            Console.WriteLine("EnUygunServisSeciminiYap metodu başladı.");
            if (calisanSayisi == 0) return new List<ServisSecimi>();

            try
            {
                List<ServisSecimi> tumKombinasyonlarToplami = new List<ServisSecimi>();
                object kilitNesnesi = new object(); // Paralel işlemler için kilit nesnesi

                foreach (double rotaMesafesiKm in servisMesafeListesi)
                {
                    // Sıfıra bölme veya negatiften kaçının
                    double mevcutRotaMesafesi = (rotaMesafesiKm <= 0) ? 1.0 : rotaMesafesiKm;

                    string kilometreGrubuAnahtari = servisUcretleriSozlugu.Keys.FirstOrDefault(grupAnahtari =>
                    {
                        string[] aralik = grupAnahtari.Split('-');
                        return aralik.Length == 2 &&
                               int.TryParse(aralik[0], out int baslangic) &&
                               int.TryParse(aralik[1], out int bitis) &&
                               mevcutRotaMesafesi >= baslangic && mevcutRotaMesafesi <= bitis;
                    });

                    if (string.IsNullOrEmpty(kilometreGrubuAnahtari))
                    {
                        // En uygun yedek kilometre grubunu bul
                        kilometreGrubuAnahtari = servisUcretleriSozlugu.Keys
                            .Select(g => new { Anahtar = g, Parcalar = g.Split('-') })
                            .Where(g => g.Parcalar.Length == 2 && int.TryParse(g.Parcalar[1], out _)) // BitisKm parse edilebilsin
                            .OrderByDescending(g => int.Parse(g.Parcalar[1])) // En yüksek BitisKm'ye göre
                            .FirstOrDefault()?.Anahtar ?? servisUcretleriSozlugu.Keys.FirstOrDefault(); // Hiçbiri uymazsa ilkini al

                        if (string.IsNullOrEmpty(kilometreGrubuAnahtari))
                        {
                            Console.WriteLine($"Servis ücretleri tanımlı değil. Rota mesafesi: {mevcutRotaMesafesi}");
                            throw new Exception($"Servis ücretleri tanımlı değil.");
                        }
                        Console.WriteLine($"Rota mesafesi ({mevcutRotaMesafesi} km) için tam kilometre grubu bulunamadı. Alternatif grup: {kilometreGrubuAnahtari} kullanılıyor.");
                    }

                    var ucretlerListesi = servisUcretleriSozlugu[kilometreGrubuAnahtari];
                    // Kapasiteye göre ücretleri içeren bir sözlük oluştur
                    Dictionary<int, double> kapasiteUcretHaritasi = ucretlerListesi.ToDictionary(u => u.Kapasite, u => u.Ucret);

                    // Her servis tipi için maksimum olası sayıyı hesapla
                    int maks19Kapasiteli = kapasiteUcretHaritasi.ContainsKey(19) ? (int)Math.Ceiling((double)calisanSayisi / 19.0) : 0;
                    int maks27Kapasiteli = kapasiteUcretHaritasi.ContainsKey(27) ? (int)Math.Ceiling((double)calisanSayisi / 27.0) : 0;
                    int maks46Kapasiteli = kapasiteUcretHaritasi.ContainsKey(46) ? (int)Math.Ceiling((double)calisanSayisi / 46.0) : 0;

                    // Tüm olası servis kombinasyonlarını paralel olarak değerlendir
                    Parallel.For(0, maks19Kapasiteli + 1, s19 =>
                    {
                        for (int s27 = 0; s27 <= maks27Kapasiteli; s27++)
                        {
                            for (int s46 = 0; s46 <= maks46Kapasiteli; s46++)
                            {
                                int mevcutToplamKapasite = (s19 * 19) + (s27 * 27) + (s46 * 46);
                                // Toplam kapasite çalışan sayısını karşılıyorsa ve en az bir servis varsa
                                if (mevcutToplamKapasite >= calisanSayisi && (s19 > 0 || s27 > 0 || s46 > 0))
                                {
                                    double toplamMaliyet = 0;
                                    if (s19 > 0 && kapasiteUcretHaritasi.ContainsKey(19)) toplamMaliyet += s19 * kapasiteUcretHaritasi[19];
                                    if (s27 > 0 && kapasiteUcretHaritasi.ContainsKey(27)) toplamMaliyet += s27 * kapasiteUcretHaritasi[27];
                                    if (s46 > 0 && kapasiteUcretHaritasi.ContainsKey(46)) toplamMaliyet += s46 * kapasiteUcretHaritasi[46];

                                    List<ServisSecimi> mevcutKombinasyonDetaylari = new List<ServisSecimi>();
                                    int kalanCalisanSayisi = calisanSayisi;

                                    // Her servis tipinden kaç kişinin taşındığını ve maliyetini hesapla
                                    if (s19 > 0 && kapasiteUcretHaritasi.ContainsKey(19))
                                    {
                                        int tasinan = Math.Min(s19 * 19, kalanCalisanSayisi);
                                        mevcutKombinasyonDetaylari.Add(new ServisSecimi { ServisTipi = 19, ServisSayisi = s19, TasinanKisiSayisi = tasinan, BirimFiyat = kapasiteUcretHaritasi[19] / mevcutRotaMesafesi, ToplamFiyat = s19 * kapasiteUcretHaritasi[19] });
                                        kalanCalisanSayisi -= tasinan;
                                    }
                                    if (s27 > 0 && kapasiteUcretHaritasi.ContainsKey(27) && kalanCalisanSayisi > 0)
                                    {
                                        int tasinan = Math.Min(s27 * 27, kalanCalisanSayisi);
                                        mevcutKombinasyonDetaylari.Add(new ServisSecimi { ServisTipi = 27, ServisSayisi = s27, TasinanKisiSayisi = tasinan, BirimFiyat = kapasiteUcretHaritasi[27] / mevcutRotaMesafesi, ToplamFiyat = s27 * kapasiteUcretHaritasi[27] });
                                        kalanCalisanSayisi -= tasinan;
                                    }
                                    if (s46 > 0 && kapasiteUcretHaritasi.ContainsKey(46) && kalanCalisanSayisi > 0)
                                    {
                                        int tasinan = Math.Min(s46 * 46, kalanCalisanSayisi);
                                        mevcutKombinasyonDetaylari.Add(new ServisSecimi { ServisTipi = 46, ServisSayisi = s46, TasinanKisiSayisi = tasinan, BirimFiyat = kapasiteUcretHaritasi[46] / mevcutRotaMesafesi, ToplamFiyat = s46 * kapasiteUcretHaritasi[46] });
                                    }

                                    if (mevcutKombinasyonDetaylari.Any())
                                    {
                                        lock (kilitNesnesi) // Liste erişimini senkronize et
                                        {
                                            tumKombinasyonlarToplami.Add(new ServisSecimi { ServisTipi = 0, ServisSayisi = s19 + s27 + s46, TasinanKisiSayisi = calisanSayisi, ToplamFiyat = toplamMaliyet, Detaylar = mevcutKombinasyonDetaylari });
                                        }
                                    }
                                }
                            }
                        }
                    });
                }

                if (!tumKombinasyonlarToplami.Any())
                {
                    Console.WriteLine("Uygun servis kombinasyonu bulunamadı.");
                    return new List<ServisSecimi>(); // Boş liste döndür
                }

                // En düşük maliyetli ve sonra en az servis sayısına sahip kombinasyonu seç
                var enUygunKombinasyon = tumKombinasyonlarToplami
                    .OrderBy(komb => komb.ToplamFiyat)
                    .ThenBy(komb => komb.ServisSayisi)
                    .FirstOrDefault();

                Console.WriteLine("En Uygun Servis Seçimi Yapıldı.");
                return enUygunKombinasyon?.Detaylar ?? new List<ServisSecimi>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"En Uygun Servis Seçimi sırasında hata: {ex.Message}\n{ex.StackTrace}");
                throw; 
            }
        }
        #endregion

        #region Rapor İşlemleri

        private void RaporuWordOlarakKaydet(string dosyaYolu)
        {
            try
            {
                using (var belge = DocX.Create(dosyaYolu)) 
                {
                    belge.InsertParagraph("SERVİS OPTİMİZASYON RAPORU").FontSize(16).Bold().Alignment = Alignment.center;
                    belge.InsertParagraph(); 

                    belge.InsertParagraph($"Tarih: {DateTime.Now:dd.MM.yyyy HH:mm:ss}").FontSize(12);
                    belge.InsertParagraph($"Bölge: {cmbBolgeler.SelectedItem?.ToString() ?? "Belirtilmemiş"}");
                    belge.InsertParagraph($"Çalışan Sayısı: {calisanlar?.Count ?? 0}");
                    belge.InsertParagraph($"Hat Toplam Mesafe (Genel TSP): {toplamMesafe:N2} km");
                    belge.InsertParagraph();

                    belge.InsertParagraph("SERVİS ROTALARI VE MESAFELERİ").FontSize(14).Bold();
                    if (lstOptimumRota.Items.Count > 0)
                    {
                        foreach (var oge in lstOptimumRota.Items) belge.InsertParagraph(oge.ToString()).FontSize(10);
                    }
                    else belge.InsertParagraph("Optimize edilmiş rota bilgisi bulunmamaktadır.").FontSize(10);
                    belge.InsertParagraph();

                    belge.InsertParagraph("ÇALIŞAN LİSTESİ").FontSize(14).Bold();
                    if (calisanlar != null && calisanlar.Any())
                    {
                        var calisanTablosu = belge.AddTable(calisanlar.Count + 1, 3); 
                        calisanTablosu.Design = TableDesign.TableGrid; 
                        // Başlık satırı
                        calisanTablosu.Rows[0].Cells[0].Paragraphs.First().Append("No").Bold();
                        calisanTablosu.Rows[0].Cells[1].Paragraphs.First().Append("Ad Soyad").Bold();
                        calisanTablosu.Rows[0].Cells[2].Paragraphs.First().Append("Yaklaşık Adres").Bold();
                        // Veri satırları
                        for (int i = 0; i < calisanlar.Count; i++)
                        {
                            calisanTablosu.Rows[i + 1].Cells[0].Paragraphs.First().Append((i + 1).ToString());
                            calisanTablosu.Rows[i + 1].Cells[1].Paragraphs.First().Append(calisanlar[i].AdSoyad);
                            calisanTablosu.Rows[i + 1].Cells[2].Paragraphs.First().Append(calisanlar[i].YaklasikAdres);
                        }
                        belge.InsertTable(calisanTablosu);
                    }
                    else belge.InsertParagraph("Çalışan listesi bulunmamaktadır.").FontSize(10);
                    belge.InsertParagraph();

                    belge.InsertParagraph("SERVİS SEÇİMİ VE MALİYET").FontSize(14).Bold();
                    if (dgvSonuclar.Rows.Count > 0)
                    {
                        var servisTablosu = belge.AddTable(dgvSonuclar.Rows.Cast<DataGridViewRow>().Count(r => !r.IsNewRow) + 1, 5); 
                        servisTablosu.Design = TableDesign.TableGrid;
                        // Başlık satırı
                        servisTablosu.Rows[0].Cells[0].Paragraphs.First().Append("Servis Tipi").Bold();
                        servisTablosu.Rows[0].Cells[1].Paragraphs.First().Append("Servis Sayısı").Bold();
                        servisTablosu.Rows[0].Cells[2].Paragraphs.First().Append("Taşınan Kişi").Bold();
                        servisTablosu.Rows[0].Cells[3].Paragraphs.First().Append("Birim Fiyat").Bold();
                        servisTablosu.Rows[0].Cells[4].Paragraphs.First().Append("Toplam Fiyat").Bold();
                        // Veri satırları
                        int satirIndeksi = 1;
                        foreach (DataGridViewRow satir in dgvSonuclar.Rows)
                        {
                            if (satir.IsNewRow) continue; 
                            servisTablosu.Rows[satirIndeksi].Cells[0].Paragraphs.First().Append(satir.Cells["colServisTipi"].Value?.ToString() ?? "");
                            servisTablosu.Rows[satirIndeksi].Cells[1].Paragraphs.First().Append(satir.Cells["colServisSayisi"].Value?.ToString() ?? "");
                            servisTablosu.Rows[satirIndeksi].Cells[2].Paragraphs.First().Append(satir.Cells["colKisiSayisi"].Value?.ToString() ?? "");
                            servisTablosu.Rows[satirIndeksi].Cells[3].Paragraphs.First().Append(satir.Cells["colBirimFiyat"].Value?.ToString() ?? "");
                            servisTablosu.Rows[satirIndeksi].Cells[4].Paragraphs.First().Append(satir.Cells["colToplamFiyat"].Value?.ToString() ?? "");
                            satirIndeksi++;
                        }
                        belge.InsertTable(servisTablosu);
                        belge.InsertParagraph($"\nTOPLAM MALİYET: {lblToplamMaliyet.Text}").Bold().Alignment = Alignment.right;
                    }
                    else belge.InsertParagraph("Servis seçimi bilgisi bulunmamaktadır.").FontSize(10);

                    belge.Save(); 
                    MessageBox.Show($"Word Raporu başarıyla kaydedildi: {dosyaYolu}", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Word Raporu oluşturulurken bir hata oluştu: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void RaporuMetinOlarakKaydet(string dosyaYolu)
        {
            try
            {
                StringBuilder metinOlusturucu = new StringBuilder(); 
                metinOlusturucu.AppendLine("SERVİS OPTİMİZASYON RAPORU");
                metinOlusturucu.AppendLine("==========================");
                metinOlusturucu.AppendLine($"Tarih: {DateTime.Now:dd.MM.yyyy HH:mm:ss}");
                metinOlusturucu.AppendLine($"Bölge: {cmbBolgeler.SelectedItem?.ToString() ?? "Belirtilmemiş"}");
                metinOlusturucu.AppendLine($"Çalışan Sayısı: {calisanlar?.Count ?? 0}");
                metinOlusturucu.AppendLine($"Hat Toplam Mesafe (Genel TSP): {toplamMesafe:N2} km\n");

                metinOlusturucu.AppendLine("SERVİS ROTALARI VE MESAFELERİ");
                metinOlusturucu.AppendLine("-----------------------------");
                if (lstOptimumRota.Items.Count > 0) foreach (var oge in lstOptimumRota.Items) metinOlusturucu.AppendLine(oge.ToString());
                else metinOlusturucu.AppendLine("Optimize edilmiş rota bilgisi bulunmamaktadır.");
                metinOlusturucu.AppendLine();

                metinOlusturucu.AppendLine("ÇALIŞAN LİSTESİ");
                metinOlusturucu.AppendLine("--------------");
                if (calisanlar != null && calisanlar.Any())
                {
                    for (int i = 0; i < calisanlar.Count; i++) metinOlusturucu.AppendLine($"{i + 1}. {calisanlar[i].AdSoyad} - {calisanlar[i].YaklasikAdres}");
                }
                else metinOlusturucu.AppendLine("Çalışan listesi bulunmamaktadır.");
                metinOlusturucu.AppendLine();

                metinOlusturucu.AppendLine("SERVİS SEÇİMİ VE MALİYET");
                metinOlusturucu.AppendLine("-------------------------");
                if (dgvSonuclar.Rows.Count > 0)
                {
                    metinOlusturucu.AppendLine(String.Format("{0,-20} | {1,-15} | {2,-15} | {3,-20} | {4,-15}", "Servis Tipi", "Servis Sayısı", "Taşınan Kişi", "Birim Fiyat", "Toplam Fiyat"));
                    metinOlusturucu.AppendLine(new string('-', 90)); 
                    foreach (DataGridViewRow satir in dgvSonuclar.Rows)
                    {
                        if (satir.IsNewRow) continue;
                        metinOlusturucu.AppendLine(String.Format("{0,-20} | {1,-15} | {2,-15} | {3,-20} | {4,-15}",
                            satir.Cells["colServisTipi"].Value?.ToString() ?? "", satir.Cells["colServisSayisi"].Value?.ToString() ?? "",
                            satir.Cells["colKisiSayisi"].Value?.ToString() ?? "", satir.Cells["colBirimFiyat"].Value?.ToString() ?? "",
                            satir.Cells["colToplamFiyat"].Value?.ToString() ?? ""));
                    }
                    metinOlusturucu.AppendLine($"\nTOPLAM MALİYET: {lblToplamMaliyet.Text}");
                }
                else metinOlusturucu.AppendLine("Servis seçimi bilgisi bulunmamaktadır.");

                File.WriteAllText(dosyaYolu, metinOlusturucu.ToString(), Encoding.UTF8); 
                MessageBox.Show($"Metin Raporu başarıyla kaydedildi: {dosyaYolu}", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Metin Raporu oluşturulurken bir hata oluştu: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        #endregion

        private void lblToplamMesafe_Click(object sender, EventArgs e)
        {

        }
    }

    #region Model Sınıfları 

    public class Calisan
    {
        public string AdSoyad { get; set; }
        public string YaklasikAdres { get; set; }
    }

    public class KilometreGrubu
    {
        public string KilometreAraligi { get; set; }
        public int BaslangicKm { get; set; }
        public int BitisKm { get; set; }
        public ServisUcretDetaylari ServisUcretleri { get; set; } 
    }


    public class ServisUcretDetaylari
    {
        [JsonPropertyName("19")]
        public double _19 { get; set; } // 19 kişilik servis ücreti

        [JsonPropertyName("27")]
        public double _27 { get; set; } // 27 kişilik servis ücreti

        [JsonPropertyName("46")]
        public double _46 { get; set; } // 46 kişilik servis ücreti
    }

    public class ServisUcret
    {
        public int Kapasite { get; set; }
        public double Ucret { get; set; }
    }

    public class ServisSecimi
    {
        public int ServisTipi { get; set; } // Örneğin 19, 27, 46 kişilik
        public int ServisSayisi { get; set; } // Bu tipten kaç adet servis olduğu
        public int TasinanKisiSayisi { get; set; } // Bu servislerle taşınan toplam kişi sayısı
        public double BirimFiyat { get; set; } // Servis başına düşen maliyet
        public double ToplamFiyat { get; set; } // Bu servis tipi için toplam maliyet
        public List<ServisSecimi> Detaylar { get; set; } // Eğer bir kombinasyonun detayıysa (EnUygunServisSeciminiYap içinde kullanılıyor)
    }
    #endregion
}
