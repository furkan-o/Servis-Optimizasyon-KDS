namespace ServisOptimizasyonSistemi
{
    partial class MainForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            tabControl1 = new TabControl();
            tabBolgeSecimi = new TabPage();
            btnOptimizasyonYap = new Button();
            label2 = new Label();
            cmbBolgeler = new ComboBox();
            label1 = new Label();
            tabCalisanlar = new TabPage();
            lstCalisanlar = new ListBox();
            label4 = new Label();
            lblCalisanSayisi = new Label();
            label3 = new Label();
            tabSonuclar = new TabPage();
            btnRaporOlustur = new Button();
            lblToplamMaliyet = new Label();
            label9 = new Label();
            dgvSonuclar = new DataGridView();
            colServisTipi = new DataGridViewTextBoxColumn();
            colServisSayisi = new DataGridViewTextBoxColumn();
            colKisiSayisi = new DataGridViewTextBoxColumn();
            colBirimFiyat = new DataGridViewTextBoxColumn();
            colToplamFiyat = new DataGridViewTextBoxColumn();
            label8 = new Label();
            lstOptimumRota = new ListBox();
            label7 = new Label();
            lblToplamMesafe = new Label();
            label5 = new Label();
            statusStrip1 = new StatusStrip();
            lblDurum = new ToolStripStatusLabel();
            tabControl1.SuspendLayout();
            tabBolgeSecimi.SuspendLayout();
            tabCalisanlar.SuspendLayout();
            tabSonuclar.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dgvSonuclar).BeginInit();
            statusStrip1.SuspendLayout();
            SuspendLayout();
            // 
            // tabControl1
            // 
            tabControl1.Controls.Add(tabBolgeSecimi);
            tabControl1.Controls.Add(tabCalisanlar);
            tabControl1.Controls.Add(tabSonuclar);
            tabControl1.Dock = DockStyle.Fill;
            tabControl1.Location = new Point(0, 0);
            tabControl1.Margin = new Padding(4, 5, 4, 5);
            tabControl1.Name = "tabControl1";
            tabControl1.SelectedIndex = 0;
            tabControl1.Size = new Size(1067, 692);
            tabControl1.TabIndex = 0;
            // 
            // tabBolgeSecimi
            // 
            tabBolgeSecimi.Controls.Add(btnOptimizasyonYap);
            tabBolgeSecimi.Controls.Add(label2);
            tabBolgeSecimi.Controls.Add(cmbBolgeler);
            tabBolgeSecimi.Controls.Add(label1);
            tabBolgeSecimi.Location = new Point(4, 29);
            tabBolgeSecimi.Margin = new Padding(4, 5, 4, 5);
            tabBolgeSecimi.Name = "tabBolgeSecimi";
            tabBolgeSecimi.Padding = new Padding(4, 5, 4, 5);
            tabBolgeSecimi.Size = new Size(1059, 659);
            tabBolgeSecimi.TabIndex = 0;
            tabBolgeSecimi.Text = "Bölge Seçimi";
            tabBolgeSecimi.UseVisualStyleBackColor = true;
            // 
            // btnOptimizasyonYap
            // 
            btnOptimizasyonYap.Font = new Font("Microsoft Sans Serif", 12F, FontStyle.Bold, GraphicsUnit.Point, 162);
            btnOptimizasyonYap.Location = new Point(392, 377);
            btnOptimizasyonYap.Margin = new Padding(4, 5, 4, 5);
            btnOptimizasyonYap.Name = "btnOptimizasyonYap";
            btnOptimizasyonYap.Size = new Size(284, 78);
            btnOptimizasyonYap.TabIndex = 3;
            btnOptimizasyonYap.Text = "Optimizasyon Yap";
            btnOptimizasyonYap.UseVisualStyleBackColor = true;
            btnOptimizasyonYap.Click += btnOptimizasyonYap_Click;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(277, 262);
            label2.Margin = new Padding(4, 0, 4, 0);
            label2.Name = "label2";
            label2.Size = new Size(101, 20);
            label2.TabIndex = 2;
            label2.Text = "Bölge Seçiniz:";
            // 
            // cmbBolgeler
            // 
            cmbBolgeler.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbBolgeler.Font = new Font("Microsoft Sans Serif", 12F, FontStyle.Regular, GraphicsUnit.Point, 162);
            cmbBolgeler.FormattingEnabled = true;
            cmbBolgeler.Location = new Point(392, 255);
            cmbBolgeler.Margin = new Padding(4, 5, 4, 5);
            cmbBolgeler.Name = "cmbBolgeler";
            cmbBolgeler.Size = new Size(283, 33);
            cmbBolgeler.TabIndex = 1;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Font = new Font("Microsoft Sans Serif", 14F, FontStyle.Bold, GraphicsUnit.Point, 162);
            label1.Location = new Point(272, 117);
            label1.Margin = new Padding(4, 0, 4, 0);
            label1.Name = "label1";
            label1.Size = new Size(502, 29);
            label1.TabIndex = 0;
            label1.Text = "Servis Optimizasyon Karar Destek Sistemi";
            // 
            // tabCalisanlar
            // 
            tabCalisanlar.Controls.Add(lstCalisanlar);
            tabCalisanlar.Controls.Add(label4);
            tabCalisanlar.Controls.Add(lblCalisanSayisi);
            tabCalisanlar.Controls.Add(label3);
            tabCalisanlar.Location = new Point(4, 29);
            tabCalisanlar.Margin = new Padding(4, 5, 4, 5);
            tabCalisanlar.Name = "tabCalisanlar";
            tabCalisanlar.Padding = new Padding(4, 5, 4, 5);
            tabCalisanlar.Size = new Size(1059, 659);
            tabCalisanlar.TabIndex = 1;
            tabCalisanlar.Text = "Çalışanlar";
            tabCalisanlar.UseVisualStyleBackColor = true;
            // 
            // lstCalisanlar
            // 
            lstCalisanlar.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            lstCalisanlar.FormattingEnabled = true;
            lstCalisanlar.Location = new Point(31, 117);
            lstCalisanlar.Margin = new Padding(4, 5, 4, 5);
            lstCalisanlar.Name = "lstCalisanlar";
            lstCalisanlar.Size = new Size(993, 504);
            lstCalisanlar.TabIndex = 3;
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Font = new Font("Microsoft Sans Serif", 10F, FontStyle.Bold, GraphicsUnit.Point, 162);
            label4.Location = new Point(27, 86);
            label4.Margin = new Padding(4, 0, 4, 0);
            label4.Name = "label4";
            label4.Size = new Size(141, 20);
            label4.TabIndex = 2;
            label4.Text = "Çalışan Listesi:";
            // 
            // lblCalisanSayisi
            // 
            lblCalisanSayisi.AutoSize = true;
            lblCalisanSayisi.Font = new Font("Microsoft Sans Serif", 10F, FontStyle.Bold, GraphicsUnit.Point, 162);
            lblCalisanSayisi.Location = new Point(200, 23);
            lblCalisanSayisi.Margin = new Padding(4, 0, 4, 0);
            lblCalisanSayisi.Name = "lblCalisanSayisi";
            lblCalisanSayisi.Size = new Size(19, 20);
            lblCalisanSayisi.TabIndex = 1;
            lblCalisanSayisi.Text = "0";
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Font = new Font("Microsoft Sans Serif", 10F, FontStyle.Regular, GraphicsUnit.Point, 162);
            label3.Location = new Point(27, 23);
            label3.Margin = new Padding(4, 0, 4, 0);
            label3.Name = "label3";
            label3.Size = new Size(130, 20);
            label3.TabIndex = 0;
            label3.Text = "Toplam Çalışan:";
            // 
            // tabSonuclar
            // 
            tabSonuclar.Controls.Add(btnRaporOlustur);
            tabSonuclar.Controls.Add(lblToplamMaliyet);
            tabSonuclar.Controls.Add(label9);
            tabSonuclar.Controls.Add(dgvSonuclar);
            tabSonuclar.Controls.Add(label8);
            tabSonuclar.Controls.Add(lstOptimumRota);
            tabSonuclar.Controls.Add(label7);
            tabSonuclar.Controls.Add(lblToplamMesafe);
            tabSonuclar.Controls.Add(label5);
            tabSonuclar.Location = new Point(4, 29);
            tabSonuclar.Margin = new Padding(4, 5, 4, 5);
            tabSonuclar.Name = "tabSonuclar";
            tabSonuclar.Size = new Size(1059, 659);
            tabSonuclar.TabIndex = 2;
            tabSonuclar.Text = "Sonuçlar";
            tabSonuclar.UseVisualStyleBackColor = true;
            // 
            // btnRaporOlustur
            // 
            btnRaporOlustur.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            btnRaporOlustur.Font = new Font("Microsoft Sans Serif", 10F, FontStyle.Bold, GraphicsUnit.Point, 162);
            btnRaporOlustur.Location = new Point(824, 567);
            btnRaporOlustur.Margin = new Padding(4, 5, 4, 5);
            btnRaporOlustur.Name = "btnRaporOlustur";
            btnRaporOlustur.Size = new Size(221, 55);
            btnRaporOlustur.TabIndex = 8;
            btnRaporOlustur.Text = "Rapor Oluştur";
            btnRaporOlustur.UseVisualStyleBackColor = true;
            btnRaporOlustur.Click += btnRaporOlustur_Click;
            // 
            // lblToplamMaliyet
            // 
            lblToplamMaliyet.AutoSize = true;
            lblToplamMaliyet.Font = new Font("Microsoft Sans Serif", 12F, FontStyle.Bold, GraphicsUnit.Point, 162);
            lblToplamMaliyet.ForeColor = Color.Green;
            lblToplamMaliyet.Location = new Point(620, 577);
            lblToplamMaliyet.Margin = new Padding(4, 0, 4, 0);
            lblToplamMaliyet.Name = "lblToplamMaliyet";
            lblToplamMaliyet.Size = new Size(66, 25);
            lblToplamMaliyet.TabIndex = 7;
            lblToplamMaliyet.Text = "₺0,00";
            // 
            // label9
            // 
            label9.AutoSize = true;
            label9.Font = new Font("Microsoft Sans Serif", 10F, FontStyle.Regular, GraphicsUnit.Point, 162);
            label9.Location = new Point(452, 580);
            label9.Margin = new Padding(4, 0, 4, 0);
            label9.Name = "label9";
            label9.Size = new Size(127, 20);
            label9.TabIndex = 6;
            label9.Text = "Toplam Maliyet:";
            // 
            // dgvSonuclar
            // 
            dgvSonuclar.AllowUserToAddRows = false;
            dgvSonuclar.AllowUserToDeleteRows = false;
            dgvSonuclar.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            dgvSonuclar.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvSonuclar.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvSonuclar.Columns.AddRange(new DataGridViewColumn[] { colServisTipi, colServisSayisi, colKisiSayisi, colBirimFiyat, colToplamFiyat });
            dgvSonuclar.Location = new Point(437, 373);
            dgvSonuclar.Margin = new Padding(4, 5, 4, 5);
            dgvSonuclar.Name = "dgvSonuclar";
            dgvSonuclar.ReadOnly = true;
            dgvSonuclar.RowHeadersWidth = 51;
            dgvSonuclar.Size = new Size(608, 189);
            dgvSonuclar.TabIndex = 5;
            // 
            // colServisTipi
            // 
            colServisTipi.HeaderText = "Servis Tipi";
            colServisTipi.MinimumWidth = 6;
            colServisTipi.Name = "colServisTipi";
            colServisTipi.ReadOnly = true;
            // 
            // colServisSayisi
            // 
            colServisSayisi.HeaderText = "Servis Sayısı";
            colServisSayisi.MinimumWidth = 6;
            colServisSayisi.Name = "colServisSayisi";
            colServisSayisi.ReadOnly = true;
            // 
            // colKisiSayisi
            // 
            colKisiSayisi.HeaderText = "Kişi Sayısı";
            colKisiSayisi.MinimumWidth = 6;
            colKisiSayisi.Name = "colKisiSayisi";
            colKisiSayisi.ReadOnly = true;
            // 
            // colBirimFiyat
            // 
            colBirimFiyat.HeaderText = "Birim Fiyat (₺/km)";
            colBirimFiyat.MinimumWidth = 6;
            colBirimFiyat.Name = "colBirimFiyat";
            colBirimFiyat.ReadOnly = true;
            // 
            // colToplamFiyat
            // 
            colToplamFiyat.HeaderText = "Toplam Fiyat";
            colToplamFiyat.MinimumWidth = 6;
            colToplamFiyat.Name = "colToplamFiyat";
            colToplamFiyat.ReadOnly = true;
            // 
            // label8
            // 
            label8.AutoSize = true;
            label8.Font = new Font("Microsoft Sans Serif", 10F, FontStyle.Bold, GraphicsUnit.Point, 162);
            label8.Location = new Point(433, 337);
            label8.Margin = new Padding(4, 0, 4, 0);
            label8.Name = "label8";
            label8.Size = new Size(308, 20);
            label8.TabIndex = 4;
            label8.Text = "En Uygun Servis Seçimi ve Maliyet:";
            // 
            // lstOptimumRota
            // 
            lstOptimumRota.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left;
            lstOptimumRota.FormattingEnabled = true;
            lstOptimumRota.Location = new Point(11, 80);
            lstOptimumRota.Margin = new Padding(4, 5, 4, 5);
            lstOptimumRota.Name = "lstOptimumRota";
            lstOptimumRota.Size = new Size(408, 544);
            lstOptimumRota.TabIndex = 3;
            // 
            // label7
            // 
            label7.AutoSize = true;
            label7.Font = new Font("Microsoft Sans Serif", 10F, FontStyle.Bold, GraphicsUnit.Point, 162);
            label7.Location = new Point(11, 49);
            label7.Margin = new Padding(4, 0, 4, 0);
            label7.Name = "label7";
            label7.Size = new Size(135, 20);
            label7.TabIndex = 2;
            label7.Text = "Optimum Rota:";
            // 
            // lblToplamMesafe
            // 
            lblToplamMesafe.AutoSize = true;
            lblToplamMesafe.Font = new Font("Microsoft Sans Serif", 10F, FontStyle.Bold, GraphicsUnit.Point, 162);
            lblToplamMesafe.Location = new Point(620, 86);
            lblToplamMesafe.Margin = new Padding(4, 0, 4, 0);
            lblToplamMesafe.Name = "lblToplamMesafe";
            lblToplamMesafe.Size = new Size(49, 20);
            lblToplamMesafe.TabIndex = 1;
            lblToplamMesafe.Text = "0 km";
            lblToplamMesafe.Click += lblToplamMesafe_Click;
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Font = new Font("Microsoft Sans Serif", 10F, FontStyle.Regular, GraphicsUnit.Point, 162);
            label5.Location = new Point(433, 86);
            label5.Margin = new Padding(4, 0, 4, 0);
            label5.Name = "label5";
            label5.Size = new Size(161, 20);
            label5.TabIndex = 0;
            label5.Text = "Hat Toplam Mesafe:";
            //label5.Click += this.label5_Click;
            // 
            // statusStrip1
            // 
            statusStrip1.ImageScalingSize = new Size(20, 20);
            statusStrip1.Items.AddRange(new ToolStripItem[] { lblDurum });
            statusStrip1.Location = new Point(0, 666);
            statusStrip1.Name = "statusStrip1";
            statusStrip1.Padding = new Padding(1, 0, 19, 0);
            statusStrip1.Size = new Size(1067, 26);
            statusStrip1.TabIndex = 1;
            statusStrip1.Text = "statusStrip1";
            // 
            // lblDurum
            // 
            lblDurum.Name = "lblDurum";
            lblDurum.Size = new Size(53, 20);
            lblDurum.Text = "Hazır...";
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1067, 692);
            Controls.Add(statusStrip1);
            Controls.Add(tabControl1);
            Margin = new Padding(4, 5, 4, 5);
            Name = "MainForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Servis Optimizasyon Karar Destek Sistemi";
            Load += MainForm_Load;
            tabControl1.ResumeLayout(false);
            tabBolgeSecimi.ResumeLayout(false);
            tabBolgeSecimi.PerformLayout();
            tabCalisanlar.ResumeLayout(false);
            tabCalisanlar.PerformLayout();
            tabSonuclar.ResumeLayout(false);
            tabSonuclar.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)dgvSonuclar).EndInit();
            statusStrip1.ResumeLayout(false);
            statusStrip1.PerformLayout();
            ResumeLayout(false);
            PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.TabPage tabBolgeSecimi;
        private System.Windows.Forms.TabPage tabCalisanlar;
        private System.Windows.Forms.TabPage tabSonuclar;
        private System.Windows.Forms.StatusStrip statusStrip1;
        private System.Windows.Forms.ToolStripStatusLabel lblDurum;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button btnOptimizasyonYap;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.ComboBox cmbBolgeler;
        private System.Windows.Forms.Label lblCalisanSayisi;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.ListBox lstCalisanlar;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label lblToplamMesafe;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.ListBox lstOptimumRota;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.DataGridView dgvSonuclar;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.Button btnRaporOlustur;
        private System.Windows.Forms.Label lblToplamMaliyet;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.DataGridViewTextBoxColumn colServisTipi;
        private System.Windows.Forms.DataGridViewTextBoxColumn colServisSayisi;
        private System.Windows.Forms.DataGridViewTextBoxColumn colKisiSayisi;
        private System.Windows.Forms.DataGridViewTextBoxColumn colBirimFiyat;
        private System.Windows.Forms.DataGridViewTextBoxColumn colToplamFiyat;
    }
}
