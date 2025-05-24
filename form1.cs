using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using Xceed.Document.NET;
using Xceed.Words.NET;


namespace ServisOptimizasyonSistemi
{
    public partial class MainForm : Form
    {
        private List<string> bolgeler;
        private List<Calisan> calisanlar;
        private Dictionary<string, List<ServisUcret>> servisUcretleri;
        private double[,] mesafeMatrisi;
        private List<int> optimumRota;
        private double toplamMesafe;

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

                lblDurum.Text = "Sistem hazır. Lütfen bir bölge seçin.";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Sistem başlatılırken hata oluştu: {ex.Message}", "Hata",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void btnOptimizasyonYap_Click(object sender, EventArgs e)
        {
            if (cmbBolgeler.SelectedItem == null)
            {
                MessageBox.Show("Lütfen bir bölge seçin.", "Uyarı",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string seciliBolge = cmbBolgeler.SelectedItem.ToString();
            lblDurum.Text = $"{seciliBolge} bölgesi için optimizasyon yapılıyor...";
            Application.DoEvents();

            try
            {
                // Asenkron işlemleri çalıştırmak için bir Task başlat
                Task.Run(async () =>
                {
                    // Bölgedeki çalışanları oku
                    calisanlar = CalisanlariOku(seciliBolge);

                    // UI thread'ine geri dön
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
                    mesafeMatrisi = await MesafeMatrisiHesapla(calisanlar);

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
                    int toplamServis = enUygunSecim.Sum(s => s.ServisSayisi);

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
                        lblDurum.Text = $"Toplam {toplamServis} servis için rota optimizasyonu yapılıyor...";
                    });

                    // Çalışanların index listesini oluştur
                    var calisanIndexList = Enumerable.Range(1, calisanlar.Count).ToList();
                    List<List<int>> sonRotalar = new List<List<int>>();
                    List<double> servisMesafeleri = new List<double>();

                    // Kapasite sıralamasına göre personeli parça parça ayırıp rota çıkar
                    int currentIndex = 0;
                    foreach (var kapasite in kapasiteListesi)
                    {
                        if (currentIndex >= calisanIndexList.Count) break;
                        int alinanMiktar = Math.Min(kapasite, calisanIndexList.Count - currentIndex);
                        List<int> grup = calisanIndexList.GetRange(currentIndex, alinanMiktar);
                        currentIndex += alinanMiktar;

                        // Bu grup için tek rota optimizasyonu
                        var rota = OptimizeSingleRoute(grup, mesafeMatrisi);
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
                            lstOptimumRota.Items.Add("Şirket");

                            foreach (var index in sonRotalar[i])
                            {
                                if (index > 0 && index <= calisanlar.Count)
                                {
                                    lstOptimumRota.Items.Add(calisanlar[index - 1].AdSoyad);
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
                                int rowIndex = dgvSonuclar.Rows.Add();
                                dgvSonuclar.Rows[rowIndex].Cells["colServisTipi"].Value = $"{secenek.ServisTipi} kişilik";
                                dgvSonuclar.Rows[rowIndex].Cells["colServisSayisi"].Value = secenek.ServisSayisi;
                                dgvSonuclar.Rows[rowIndex].Cells["colKisiSayisi"].Value = secenek.TasinanKisiSayisi;
                                dgvSonuclar.Rows[rowIndex].Cells["colBirimFiyat"].Value = secenek.BirimFiyat.ToString("C2");
                                dgvSonuclar.Rows[rowIndex].Cells["colToplamFiyat"].Value = secenek.ToplamFiyat.ToString("C2");
                            }

                            double toplamMaliyet = enUygunSecim.Sum(s => s.ToplamFiyat);
                            lblToplamMaliyet.Text = toplamMaliyet.ToString("C2");

                            lblDurum.Text = "Optimizasyon tamamlandı.";
                            tabControl1.SelectedIndex = 2; // Sonuçlar tabına geç
                        });
                    }
                    catch (Exception ex)
                    {
                        this.Invoke((MethodInvoker)delegate
                        {
                            MessageBox.Show($"Servis seçimi sırasında hata oluştu: {ex.Message}", "Hata",
                                MessageBoxButtons.OK, MessageBoxIcon.Error);
                            lblDurum.Text = "Hata oluştu.";
                        });
                    }
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Optimizasyon sırasında hata oluştu: {ex.Message}", "Hata",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                lblDurum.Text = "Hata oluştu.";
            }
        }


        /// Basic Gezgin Satıcı (KM problemi için gerekli)
        private List<int> OptimizeSingleRoute(List<int> subIndices, double[,] mesafeMatrisi)
        {
            // Yerel matrisi oluşturmak için bir eşleme yapar
            Dictionary<int, int> mapToLocal = new Dictionary<int, int>();
            for (int i = 0; i < subIndices.Count; i++)
            {
                mapToLocal[subIndices[i]] = i + 1;
            }

            // Şirket dahil olmak üzere yerel indexleri al
            int k = subIndices.Count;
            double[,] localMatrix = new double[k + 1, k + 1];

            // Yerel matrisi doldur
            for (int i = 0; i <= k; i++)
            {
                for (int j = 0; j <= k; j++)
                {
                    if (i == j)
                    {
                        localMatrix[i, j] = 0;
                        continue;
                    }

                    if (i == 0)
                    {

                        // büyük GSP j => altgrup[j-1]
                        int globalJ = subIndices[j - 1];
                        localMatrix[i, j] = mesafeMatrisi[0, globalJ];
                    }
                    else if (j == 0)
                    {
                        // yerel i => altgrup[i-1]
                        int globalI = subIndices[i - 1];
                        localMatrix[i, j] = mesafeMatrisi[globalI, 0];
                    }
                    else
                    {
                        // yerel i => altgrup[i-1], yerel j => altgrup[j-1]
                        int globalI = subIndices[i - 1];
                        int globalJ = subIndices[j - 1];
                        localMatrix[i, j] = mesafeMatrisi[globalI, globalJ];
                    }
                }
            }

            // Tek vasıtalı gezgin satıcı problemi çöz
            var routeLocal = RunSingleTSP(localMatrix, k);

            // Yerel rotayı global rotaya dönüştür
            // routeLocal yerel indexler olarak döner (1..k)
            // global index olarak dönüştürmek için subIndices[localIndex-1] kullan
            List<int> finalRoute = new List<int>();
            foreach (var localIndex in routeLocal)
            {
                // localIndex=1..k => subIndices[localIndex-1]
                finalRoute.Add(subIndices[localIndex - 1]);
            }

            return finalRoute;
        }


        /// Tekil Gezgin Satıcı Problemi çözümü için çalışan fonksiyon.
        private List<int> RunSingleTSP(double[,] localMatrix, int boyut)
        {
            // Genetik algoritma parametreleri
            int populasyonBuyuklugu = 150;
            int jenerasyonSayisi = 300;
            double caprazlamaSiklik = 1.9;
            double mutasyonSiklik = 1.3;

            // Başlangıç populasyonunu oluştur
            List<List<int>> populasyon = new List<List<int>>();
            Random rnd = new Random();

            for (int i = 0; i < populasyonBuyuklugu; i++)
            {
                // Rastgele bir rota oluştur (1'den boyuta kadar)
                List<int> rota = Enumerable.Range(1, boyut).ToList();

                // Karıştır
                for (int j = 0; j < boyut; j++)
                {
                    int k = rnd.Next(j, boyut);
                    int temp = rota[j];
                    rota[j] = rota[k];
                    rota[k] = temp;
                }

                populasyon.Add(rota);
            }

            // Jenerasyonları çalıştır
            for (int jenerasyon = 0; jenerasyon < jenerasyonSayisi; jenerasyon++)
            {
                // Uygunluk değerlerini hesapla
                Dictionary<int, double> fitnessMap = new Dictionary<int, double>();

                for (int i = 0; i < populasyon.Count; i++)
                {
                    double mesafe = RotaMesafesiHesaplaLocal(populasyon[i], localMatrix);
                    fitnessMap[i] = (mesafe <= 0) ? 0.0 : 1.0 / mesafe;
                }

                // Yeni populasyon oluştur
                List<List<int>> yeniPopulasyon = new List<List<int>>();

                // En iyi çözümü doğrudan aktar
                int enIyiIndeks = fitnessMap.OrderByDescending(x => x.Value).First().Key;
                yeniPopulasyon.Add(new List<int>(populasyon[enIyiIndeks]));

                // Yeni bireyleri oluştur
                while (yeniPopulasyon.Count < populasyonBuyuklugu)
                {
                    // Çaprazlama
                    if (rnd.NextDouble() < caprazlamaSiklik)
                    {
                        // Turnuva seçimi ile iki parent seç
                        int ebeveyn1 = TurnuvaSecimi(fitnessMap, rnd);
                        int ebeveyn2 = TurnuvaSecimi(fitnessMap, rnd);

                        // Çaprazlama yap
                        var cocuk = Caprazla(populasyon[ebeveyn1], populasyon[ebeveyn2], rnd);

                        // Child mutasyon
                        if (rnd.NextDouble() < mutasyonSiklik)
                        {
                            Mutasyon(cocuk, rnd);
                        }

                        yeniPopulasyon.Add(cocuk);
                    }
                    else
                    {
                        int secilen = TurnuvaSecimi(fitnessMap, rnd);
                        var klon = new List<int>(populasyon[secilen]);
                        if (rnd.NextDouble() < mutasyonSiklik)
                        {
                            Mutasyon(klon, rnd);
                        }
                        yeniPopulasyon.Add(klon);
                    }
                }

                // Populasyonu güncelle
                populasyon = yeniPopulasyon;
            }

            // En iyi rotayı bul
            Dictionary<int, double> sonFitnessMap = new Dictionary<int, double>();
            for (int i = 0; i < populasyon.Count; i++)
            {
                double mesafe = RotaMesafesiHesaplaLocal(populasyon[i], localMatrix);
                sonFitnessMap[i] = mesafe;
            }

            int enIyiSonIndeks = sonFitnessMap.OrderBy(x => x.Value).First().Key;
            return populasyon[enIyiSonIndeks];
        }


        /// Yerel oluşturulan 2. matrisi kullanarak mesafeyi hesaplar.
        private double RotaMesafesiHesaplaLocal(List<int> rota, double[,] localMatrix)
        {
            double toplam = 0;
            // Şirketten başla
            toplam += localMatrix[0, rota[0]];
            // Rotayı takip et
            for (int i = 0; i < rota.Count - 1; i++)
            {
                toplam += localMatrix[rota[i], rota[i + 1]];
            }
            return toplam;
        }


        private void btnRaporOlustur_Click(object sender, EventArgs e)
        {
            if (optimumRota == null || calisanlar == null)
            {
                MessageBox.Show("Henüz optimizasyon yapılmadı.", "Uyarı",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            SaveFileDialog saveDialog = new SaveFileDialog();
            saveDialog.Filter = "Word Dosyası|*.docx|Metin Dosyası|*.txt";
            saveDialog.Title = "Raporu Kaydet";

            if (saveDialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    string dosyaYolu = saveDialog.FileName;
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

                    MessageBox.Show($"Rapor başarıyla kaydedildi: {dosyaYolu}", "Bilgi",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Rapor oluşturulurken hata oluştu: {ex.Message}", "Hata",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        #region Veri Okuma İşlemleri

        private List<string> BolgeleriGetir()
        {
            // Kök dizindeki tüm json dosyalarını bul ve bölge adlarını çıkar
            return Directory.GetFiles(".", "*.json")
                .Where(dosya => !Path.GetFileName(dosya).Equals("servisucretleri.json", StringComparison.OrdinalIgnoreCase))
                .Select(dosya => Path.GetFileNameWithoutExtension(dosya))
                .ToList();
        }

        private List<Calisan> CalisanlariOku(string bolge)
        {
            string dosyaYolu = $"{bolge}.json";
            string jsonIcerik = File.ReadAllText(dosyaYolu);

            var calisanlar = JsonSerializer.Deserialize<List<Calisan>>(jsonIcerik,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            return calisanlar;
        }

        private Dictionary<string, List<ServisUcret>> ServisUcretleriniOku()
        {
            string dosyaYolu = "servisucretleri.json";
            string jsonIcerik = File.ReadAllText(dosyaYolu);

            var servisUcretleri = JsonSerializer.Deserialize<List<KilometreGrubu>>(jsonIcerik,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            // Kilometre grubu başlangıç ve bitiş değerlerine göre grupla
            var ucretSozlugu = new Dictionary<string, List<ServisUcret>>();

            foreach (var grup in servisUcretleri)
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

        #region Mesafe Matrisi İşlemleri

        private async Task<double[,]> MesafeMatrisiHesapla(List<Calisan> calisanlar)
        {
            int calisanSayisi = calisanlar.Count;
            double[,] mesafeMatrisi = new double[calisanSayisi + 1, calisanSayisi + 1];

            using (HttpClient client = new HttpClient())
            {
                string apiKey = "API BURAYA";

                // Tüm adresleri bir diziye koy (0: şirket, 1-n: çalışanlar)
                string[] adresler = new string[calisanSayisi + 1];
                adresler[0] = "ŞİRKET ADRESİ"; // Şirket adresi

                for (int i = 0; i < calisanSayisi; i++)
                {
                    adresler[i + 1] = calisanlar[i].YaklasikAdres + ", İl bilgisi Ekle"; // Adres yazmayı bilmeyen çalışanlar için il verisini manüel ekle
                }

                // Süreyi azaltmak için adresleri küçük gruplara bölelim
                int batchSize = 10; // 10 lu gruplar halinde istek yap
                List<Task> tasks = new List<Task>();

                for (int i = 0; i < adresler.Length; i += batchSize)
                {
                    for (int j = 0; j < adresler.Length; j += batchSize)
                    {
                        int startI = i;
                        int endI = Math.Min(i + batchSize, adresler.Length);
                        int startJ = j;
                        int endJ = Math.Min(j + batchSize, adresler.Length);

                        tasks.Add(Task.Run(async () =>
                        {
                            string origins = string.Join("|", adresler[startI..endI].Select(Uri.EscapeDataString));
                            string destinations = string.Join("|", adresler[startJ..endJ].Select(Uri.EscapeDataString));
                            string url = $"https://maps.googleapis.com/maps/api/distancematrix/json?origins={origins}&destinations={destinations}&key={apiKey}";

                            try
                            {
                                HttpResponseMessage response = await client.GetAsync(url);
                                if (response.IsSuccessStatusCode)
                                {
                                    var distanceResponse = await response.Content.ReadFromJsonAsync<GoogleDistanceMatrixResponse>();
                                    if (distanceResponse != null && distanceResponse.status == "OK")
                                    {
                                        for (int x = 0; x < distanceResponse.rows.Count; x++)
                                        {
                                            for (int y = 0; y < distanceResponse.rows[x].elements.Count; y++)
                                            {
                                                var element = distanceResponse.rows[x].elements[y];
                                                if (element.status == "OK")
                                                {
                                                    mesafeMatrisi[startI + x, startJ + y] = element.distance.value / 1000.0; // Google tarafından metre olarak döndürülen mesafeyi kilometreye çevir
                                                }
                                                else
                                                {
                                                    mesafeMatrisi[startI + x, startJ + y] = double.MaxValue; // Yükseltilmiş bir değer kullanarak bir hatayı göster
                                                }
                                            }
                                        }
                                    }
                                    else
                                    {
                                        this.Invoke((MethodInvoker)delegate
                                        {
                                            MessageBox.Show("Mesafe hesaplanırken hata oluştu: Geçersiz yanıt", "Hata",
                                                MessageBoxButtons.OK, MessageBoxIcon.Error);
                                        });
                                    }
                                }
                                else
                                {
                                    this.Invoke((MethodInvoker)delegate
                                    {
                                        MessageBox.Show($"Mesafe hesaplanırken hata oluştu: {response.ReasonPhrase}", "Hata",
                                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                                    });
                                }
                            }
                            catch (Exception ex)
                            {
                                this.Invoke((MethodInvoker)delegate
                                {
                                    MessageBox.Show($"Mesafe hesaplanırken hata oluştu: {ex.Message}", "Hata",
                                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                                });
                            }
                        }));
                    }
                }

                await Task.WhenAll(tasks);
            }

            return mesafeMatrisi;
        }

        #endregion

        #region Genetik Algoritma İşlemleri
        //Kaynaklar: https://www.datacamp.com/tutorial/genetic-algorithm-python
        //Muzaffer Kapanoglu Mutasyon Operatorleri https://www.youtube.com/watch?v=3mLE8TsyWPs

        private List<int> GezginSaticiProblemiCoz(double[,] mesafeMatrisi)
        {
            int boyut = mesafeMatrisi.GetLength(0) - 1; // Şirket hariç nokta sayısı

            // Genetik algoritma parametreleri
            int populasyonBuyuklugu = 150;
            int jenerasyonSayisi = 300;
            double caprazlamaSiklik = 1.9;
            double mutasyonSiklik = 1.3;

            // Başlangıç populasyonunu oluştur
            List<List<int>> populasyon = new List<List<int>>();
            Random rnd = new Random();

            for (int i = 0; i < populasyonBuyuklugu; i++)
            {
                // Rastgele bir rota oluştur (1'den boyuta kadar)
                List<int> rota = Enumerable.Range(1, boyut).ToList();

                // Karıştır
                for (int j = 0; j < boyut; j++)
                {
                    int k = rnd.Next(j, boyut);
                    int temp = rota[j];
                    rota[j] = rota[k];
                    rota[k] = temp;
                }

                populasyon.Add(rota);
            }

            // Jenerasyonları çalıştır
            for (int jenerasyon = 0; jenerasyon < jenerasyonSayisi; jenerasyon++)
            {
                // Uygunluk değerlerini hesapla
                Dictionary<int, double> fitnessMap = new Dictionary<int, double>();

                for (int i = 0; i < populasyon.Count; i++)
                {
                    double mesafe = RotaMesafesiHesapla(populasyon[i], mesafeMatrisi);
                    fitnessMap[i] = 1.0 / mesafe; // Mesafe ne kadar küçükse uygunluk o kadar büyük
                }

                // Yeni populasyon oluştur
                List<List<int>> yeniPopulasyon = new List<List<int>>();

                // En iyi çözümü doğrudan aktar
                int enIyiIndeks = fitnessMap.OrderByDescending(x => x.Value).First().Key;
                yeniPopulasyon.Add(new List<int>(populasyon[enIyiIndeks]));

                // Yeni bireyleri oluştur
                while (yeniPopulasyon.Count < populasyonBuyuklugu)
                {
                    // Çaprazlama yapılacak mı?
                    if (rnd.NextDouble() < caprazlamaSiklik)
                    {
                        // Turnuva seçimi ile iki parent seç
                        int ebeveyn1 = TurnuvaSecimi(fitnessMap, rnd);
                        int ebeveyn2 = TurnuvaSecimi(fitnessMap, rnd);

                        // Çaprazlama yap
                        var cocuk = Caprazla(populasyon[ebeveyn1], populasyon[ebeveyn2], rnd);

                        // Child mutasyon yap
                        if (rnd.NextDouble() < mutasyonSiklik)
                        {
                            Mutasyon(cocuk, rnd);
                        }

                        yeniPopulasyon.Add(cocuk);
                    }
                    else
                    {
                        // Turnuva seçimi ile seçimyap
                        int secilen = TurnuvaSecimi(fitnessMap, rnd);

                        // Klonla
                        var klon = new List<int>(populasyon[secilen]);

                        // Mutasyon yap
                        if (rnd.NextDouble() < mutasyonSiklik)
                        {
                            Mutasyon(klon, rnd);
                        }

                        yeniPopulasyon.Add(klon);
                    }
                }

                // Populasyonu güncelle
                populasyon = yeniPopulasyon;
            }

            // En iyi rotayı bul
            Dictionary<int, double> sonFitnessMap = new Dictionary<int, double>();
            for (int i = 0; i < populasyon.Count; i++)
            {
                double mesafe = RotaMesafesiHesapla(populasyon[i], mesafeMatrisi);
                sonFitnessMap[i] = mesafe;
            }

            int enIyiSonIndeks = sonFitnessMap.OrderBy(x => x.Value).First().Key;
            return populasyon[enIyiSonIndeks];
        }

        private int TurnuvaSecimi(Dictionary<int, double> fitnessMap, Random rnd)
        {
            int turnuvaBoyutu = 3;
            List<int> adaylar = new List<int>();

            // Rastgele adaylar seç
            for (int i = 0; i < turnuvaBoyutu; i++)
            {
                int aday = rnd.Next(fitnessMap.Count);
                adaylar.Add(aday);
            }

            // En yüksek uygunluk değerine sahip adayı döndür
            return adaylar.OrderByDescending(a => fitnessMap[a]).First();
        }

        private List<int> Caprazla(List<int> ebeveyn1, List<int> ebeveyn2, Random rnd)
        {
            int boyut = ebeveyn1.Count;
            List<int> cocuk = new List<int>(new int[boyut]);

            // Ordered Crossover (OX) kullan
            int baslangic = rnd.Next(boyut);
            int bitis = rnd.Next(baslangic, boyut);

            // Parent1'den alt dizi kopyala
            for (int i = baslangic; i <= bitis; i++)
            {
                cocuk[i] = ebeveyn1[i];
            }

            // Parent2'den kalan elemanları kopyala
            int currentPos = (bitis + 1) % boyut;
            int ebeveyn2Pos = (bitis + 1) % boyut;

            while (true)
            {
                if (currentPos == baslangic)
                    break;

                while (cocuk.Contains(ebeveyn2[ebeveyn2Pos]))
                {
                    ebeveyn2Pos = (ebeveyn2Pos + 1) % boyut;
                }

                cocuk[currentPos] = ebeveyn2[ebeveyn2Pos];
                currentPos = (currentPos + 1) % boyut;
                ebeveyn2Pos = (ebeveyn2Pos + 1) % boyut;
            }

            return cocuk;
        }

        private void Mutasyon(List<int> rota, Random rnd)
        {
            int boyut = rota.Count;

            // Rastgele iki index seç
            int indeks1 = rnd.Next(boyut);
            int indeks2 = rnd.Next(boyut);

            // İki elemanın yerini değiştir
            int temp = rota[indeks1];
            rota[indeks1] = rota[indeks2];
            rota[indeks2] = temp;
        }

        private double RotaMesafesiHesapla(List<int> rota, double[,] mesafeMatrisi)
        {
            double toplam = 0;

            // Şirketten ilk çalışana
            toplam += mesafeMatrisi[0, rota[0]];

            // Çalışanlar arası
            for (int i = 0; i < rota.Count - 1; i++)
            {
                toplam += mesafeMatrisi[rota[i], rota[i + 1]];
            }

            return toplam;
        }


        #endregion

        #region Servis Seçimi İşlemleri

        private List<ServisSecimi> EnUygunServisSeciminiYap(int calisanSayisi, List<double> servisMesafeleri, Dictionary<string, List<ServisUcret>> servisUcretleri)
        {
            Console.WriteLine("EnUygunServisSeciminiYap method started.");

            try
            {
                List<ServisSecimi> tumKombinasyonlar = new List<ServisSecimi>();

                for (int i = 0; i < servisMesafeleri.Count; i++)
                {
                    double rotaMesafesi = servisMesafeleri[i];

                    // Kilometre grubunu belirle
                    string kilometreGrubu = "";
                    foreach (var grup in servisUcretleri.Keys)
                    {
                        string[] aralik = grup.Split('-');
                        int baslangic = int.Parse(aralik[0]);
                        int bitis = int.Parse(aralik[1]);

                        if (rotaMesafesi >= baslangic && rotaMesafesi <= bitis)
                        {
                            kilometreGrubu = grup;
                            break;
                        }
                    }

                    if (string.IsNullOrEmpty(kilometreGrubu))
                    {
                        throw new Exception($"Rota mesafesi ({rotaMesafesi} km) için uygun kilometre grubu bulunamadı.");
                    }

                    var ucretler = servisUcretleri[kilometreGrubu];

                    // 19 kişilik servis sayısı
                    int maks19 = (int)Math.Ceiling(calisanSayisi / 19.0);
                    // 27 kişilik servis sayısı
                    int maks27 = (int)Math.Ceiling(calisanSayisi / 27.0);
                    // 46 kişilik servis sayısı
                    int maks46 = (int)Math.Ceiling(calisanSayisi / 46.0);

                    Console.WriteLine($"Max 19: {maks19}, Max 27: {maks27}, Max 46: {maks46}");

                    Parallel.For(0, maks19 + 1, s19 =>
                    {
                        Console.WriteLine($"Servis 19k isleniyor: {s19}");
                        for (int s27 = 0; s27 <= maks27; s27++)
                        {
                            Console.WriteLine($"Servis 27k isleniyor: {s27}");
                            for (int s46 = 0; s46 <= maks46; s46++)
                            {
                                Console.WriteLine($"Servis 46k isleniyor: {s46}");
                                int kapasite = s19 * 19 + s27 * 27 + s46 * 46;

                                if (kapasite >= calisanSayisi && (s19 > 0 || s27 > 0 || s46 > 0))
                                {
                                    double toplam19 = s19 * ucretler.First(u => u.Kapasite == 19).Ucret;
                                    double toplam27 = s27 * ucretler.First(u => u.Kapasite == 27).Ucret;
                                    double toplam46 = s46 * ucretler.First(u => u.Kapasite == 46).Ucret;
                                    double toplamMaliyet = toplam19 + toplam27 + toplam46;

                                    List<ServisSecimi> kombinasyon = new List<ServisSecimi>();

                                    if (s19 > 0)
                                    {
                                        kombinasyon.Add(new ServisSecimi
                                        {
                                            ServisTipi = 19,
                                            ServisSayisi = s19,
                                            TasinanKisiSayisi = Math.Min(s19 * 19, calisanSayisi),
                                            BirimFiyat = toplam19 / rotaMesafesi, //Birim fiyatı toplam km ye göre hesapla
                                            ToplamFiyat = toplam19
                                        });
                                    }

                                    if (s27 > 0)
                                    {
                                        int kalan = Math.Max(0, calisanSayisi - (s19 * 19));
                                        kombinasyon.Add(new ServisSecimi
                                        {
                                            ServisTipi = 27,
                                            ServisSayisi = s27,
                                            TasinanKisiSayisi = Math.Min(s27 * 27, kalan),
                                            BirimFiyat = toplam27 / rotaMesafesi, //Birim fiyatı toplam km ye göre hesapla
                                            ToplamFiyat = toplam27
                                        });
                                    }

                                    if (s46 > 0)
                                    {
                                        int kalan = Math.Max(0, calisanSayisi - (s19 * 19) - (s27 * 27));
                                        kombinasyon.Add(new ServisSecimi
                                        {
                                            ServisTipi = 46,
                                            ServisSayisi = s46,
                                            TasinanKisiSayisi = Math.Min(s46 * 46, kalan),
                                            BirimFiyat = toplam46 / rotaMesafesi, //Birim fiyatı toplam km ye göre hesapla
                                            ToplamFiyat = toplam46
                                        });
                                    }

                                    // Toplam kombinasyon maliyetini ekle
                                    lock (tumKombinasyonlar)
                                    {
                                        tumKombinasyonlar.Add(new ServisSecimi
                                        {
                                            ServisTipi = 0, // Toplam
                                            ServisSayisi = s19 + s27 + s46,
                                            TasinanKisiSayisi = calisanSayisi,
                                            BirimFiyat = 0,
                                            ToplamFiyat = toplamMaliyet,
                                            Detaylar = kombinasyon
                                        });
                                    }
                                }
                            }
                        }
                    });
                }

                // En düşük maliyetli kombinasyonu bul
                var enUygunKombinasyon = tumKombinasyonlar
                    .OrderBy(k => k.ToplamFiyat)
                    .ThenBy(k => k.ServisSayisi)
                    .FirstOrDefault();

                Console.WriteLine("En Uygun Servis Secimi Yapıldı.");

                return enUygunKombinasyon?.Detaylar ?? new List<ServisSecimi>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"En Uygun Servis Secimi sirasinda hata meydana geldi: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
                throw;
            }
        }




        #endregion

        #region Rapor İşlemleri


        private void RaporuWordOlarakKaydet(string dosyaYolu)
        {
            // Dosya oluştur
            using (var doc = DocX.Create(dosyaYolu))
            {
                // Başlık ekle
                var title = doc.InsertParagraph("SERVİS OPTİMİZASYON RAPORU")
                    .FontSize(16)
                    .Bold()
                    .Alignment = Alignment.center;

                // Alt başlık ekle
                var subtitle = doc.InsertParagraph($"Tarih: {DateTime.Now}\nBölge: {cmbBolgeler.SelectedItem.ToString()}\nÇalışan Sayısı: {calisanlar.Count}\nHat Toplam Mesafe: {toplamMesafe:N2} km")
                    .FontSize(12)
                    .Bold(false)
                    .Alignment = Alignment.left;

                // Rotayı ekle
                var routeTitle = doc.InsertParagraph("OPTİMAL ROTA")
                    .FontSize(14)
                    .Bold()
                    .Alignment = Alignment.left;

                var routeDetails = doc.InsertParagraph("Şirket");
                foreach (var index in optimumRota)
                {
                    if (index > 0 && index <= calisanlar.Count)
                    {
                        routeDetails.AppendLine($"{index}. {calisanlar[index - 1].AdSoyad} - {calisanlar[index - 1].YaklasikAdres}");
                    }
                }

                // Çalışan listesini ekle
                var employeeListTitle = doc.InsertParagraph("ÇALIŞAN LİSTESİ")
                    .FontSize(14)
                    .Bold()
                    .Alignment = Alignment.left;

                var employeeDetails = doc.InsertParagraph();
                for (int i = 0; i < calisanlar.Count; i++)
                {
                    employeeDetails.AppendLine($"{i + 1}. {calisanlar[i].AdSoyad} - {calisanlar[i].YaklasikAdres}");
                }

                // Servis seçimini ekle
                var serviceSelectionTitle = doc.InsertParagraph("SERVİS SEÇİMİ")
                    .FontSize(14)
                    .Bold()
                    .Alignment = Alignment.left;

                var serviceDetails = doc.InsertParagraph();
                double toplamMaliyet = 0;
                foreach (DataGridViewRow row in dgvSonuclar.Rows)
                {
                    string servisTipi = row.Cells["colServisTipi"].Value?.ToString();
                    string servisSayisi = row.Cells["colServisSayisi"].Value?.ToString();
                    string kisiSayisi = row.Cells["colKisiSayisi"].Value?.ToString();
                    string birimFiyat = row.Cells["colBirimFiyat"].Value?.ToString();
                    string toplamFiyat = row.Cells["colToplamFiyat"].Value?.ToString();

                    serviceDetails.AppendLine($"{servisTipi} x {servisSayisi} = {toplamFiyat}");

                    if (decimal.TryParse(toplamFiyat.Replace("₺", "").Trim(), out decimal fiyat))
                    {
                        toplamMaliyet += (double)fiyat;
                    }
                }
                serviceDetails.AppendLine($"\nTOPLAM MALİYET: {toplamMaliyet:C2}");

                // Dosya kaydet
                doc.Save();
                MessageBox.Show($"Rapor başarıyla kaydedildi: {dosyaYolu}", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }


        private void RaporuMetinOlarakKaydet(string dosyaYolu)
        {
            StringBuilder sb = new StringBuilder();
            string bolge = cmbBolgeler.SelectedItem.ToString();

            sb.AppendLine("SERVİS OPTİMİZASYON RAPORU");
            sb.AppendLine("==========================");
            sb.AppendLine();

            sb.AppendLine($"Tarih: {DateTime.Now}");
            sb.AppendLine($"Bölge: {bolge}");
            sb.AppendLine($"Çalışan Sayısı: {calisanlar.Count}");
            sb.AppendLine($"Hat Toplam Mesafe: {toplamMesafe:N2} km");
            sb.AppendLine();

            sb.AppendLine("OPTİMAL ROTA");
            sb.AppendLine("------------");
            sb.AppendLine("Şirket");
            foreach (var index in optimumRota)
            {
                if (index > 0 && index <= calisanlar.Count)
                {
                    sb.AppendLine($"{index}. {calisanlar[index - 1].AdSoyad} - {calisanlar[index - 1].YaklasikAdres}");
                }
            }
            sb.AppendLine();

            sb.AppendLine("ÇALIŞAN LİSTESİ");
            sb.AppendLine("--------------");
            for (int i = 0; i < calisanlar.Count; i++)
            {
                sb.AppendLine($"{i + 1}. {calisanlar[i].AdSoyad} - {calisanlar[i].YaklasikAdres}");
            }
            sb.AppendLine();

            sb.AppendLine("SERVİS SEÇİMİ");
            sb.AppendLine("------------");
            double toplamMaliyet = 0;

            foreach (DataGridViewRow row in dgvSonuclar.Rows)
            {
                string servisTipi = row.Cells["colServisTipi"].Value?.ToString();
                string servisSayisi = row.Cells["colServisSayisi"].Value?.ToString();
                string kisiSayisi = row.Cells["colKisiSayisi"].Value?.ToString();
                string birimFiyat = row.Cells["colBirimFiyat"].Value?.ToString();
                string toplamFiyat = row.Cells["colToplamFiyat"].Value?.ToString();

                sb.AppendLine($"{servisTipi} x {servisSayisi} = {toplamFiyat}");

                if (decimal.TryParse(toplamFiyat.Replace("₺", "").Trim(), out decimal fiyat))
                {
                    toplamMaliyet += (double)fiyat;
                }
            }

            sb.AppendLine();
            sb.AppendLine($"TOPLAM MALİYET: {toplamMaliyet:C2}");

            File.WriteAllText(dosyaYolu, sb.ToString());
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
        public ServisUcretleri ServisUcretleri { get; set; }
    }

    public class ServisUcretleri
    {
        [System.Text.Json.Serialization.JsonPropertyName("19")]
        public double _19 { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("27")]
        public double _27 { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("46")]
        public double _46 { get; set; }
    }

    public class ServisUcret
    {
        public int Kapasite { get; set; }
        public double Ucret { get; set; }
    }

    public class ServisSecimi
    {
        public int ServisTipi { get; set; }
        public int ServisSayisi { get; set; }
        public int TasinanKisiSayisi { get; set; }
        public double BirimFiyat { get; set; }
        public double ToplamFiyat { get; set; }
        public List<ServisSecimi> Detaylar { get; set; }
    }

    public class GoogleDistanceMatrixResponse
    {
        public string status { get; set; }
        public List<Row> rows { get; set; }
    }

    public class Row
    {
        public List<Element> elements { get; set; }
    }

    public class Element
    {
        public string status { get; set; }
        public Distance distance { get; set; }
    }

    public class Distance
    {
        public string text { get; set; }
        public int value { get; set; }
    }


    #endregion
}
