using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;

namespace ZkPdksApp
{
    public partial class ShiftWindow : Window
    {
        private readonly DatabaseService dbService;
        public ObservableCollection<ShiftRule> Shifts { get; set; }
        private int selectedShiftId = 0;

        public ShiftWindow(DatabaseService db)
        {
            InitializeComponent();
            dbService = db;
            Shifts = new ObservableCollection<ShiftRule>();
            dgShifts.ItemsSource = Shifts;

            LoadGroups();
            LoadShifts();
        }

        private void LoadGroups()
        {
            var groups = dbService.GetAllGroupNames();
            var empTypes = dbService.GetAllEmploymentTypes();
            cbTargetGroup.Items.Clear();
            cbTargetGroup.Items.Add("Tümü");
            
            // İstihdam türleri (Taşeron, Kadrolu vb.)
            foreach (var t in empTypes)
            {
                if (!cbTargetGroup.Items.Contains(t))
                    cbTargetGroup.Items.Add(t);
            }

            // Çalışma grupları (Üretim, Yönetim, Montaj vb.)
            foreach (var g in groups)
            {
                if (!cbTargetGroup.Items.Contains(g))
                    cbTargetGroup.Items.Add(g);
            }
            cbTargetGroup.SelectedIndex = 0;
        }

        private void LoadShifts()
        {
            Shifts.Clear();
            var list = dbService.GetAllShifts();
            foreach (var item in list)
            {
                Shifts.Add(item);
            }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            string name = txtShiftName.Text.Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show("Vardiya / kural adı zorunludur.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!TimeSpan.TryParse(txtStartTime.Text.Trim(), out TimeSpan st) ||
                !TimeSpan.TryParse(txtEndTime.Text.Trim(), out TimeSpan et))
            {
                MessageBox.Show("Başlangıç ve bitiş saatleri geçerli formatta olmalıdır (Örn: 08:30:00).", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            int.TryParse(txtGraceMinutes.Text.Trim(), out int grace);
            int.TryParse(txtLunchMinutes.Text.Trim(), out int lunch);
            int.TryParse(txtDuplicateSeconds.Text.Trim(), out int dup);
            double.TryParse(txtMaxDailyOt.Text.Trim(), out double maxOt);
            double.TryParse(txtMaxWeeklyOt.Text.Trim(), out double maxWeeklyOt);
            double.TryParse(txtMaxMonthlyOt.Text.Trim(), out double maxMonthlyOt);
            double.TryParse(txtMinOtThreshold.Text.Trim(), out double minOt);
            double.TryParse(txtReqWork.Text.Trim(), out double reqWork);
            double.TryParse(txtEarlyInMaxOt.Text.Trim(), out double earlyMaxOt);
            double.TryParse(txtWeekdayMult.Text.Trim(), out double wkdMult);
            double.TryParse(txtWeekendMult.Text.Trim(), out double wknMult);
            double.TryParse(txtHolidayMult.Text.Trim(), out double holMult);

            string targetGrp = !string.IsNullOrWhiteSpace(cbTargetGroup.Text) ? cbTargetGroup.Text.Trim() : (cbTargetGroup.SelectedItem?.ToString() ?? "Tümü");
            int rounding = 15;
            if (cbRounding.SelectedIndex == 0) rounding = 0;
            else if (cbRounding.SelectedIndex == 1) rounding = 15;
            else if (cbRounding.SelectedIndex == 2) rounding = 30;
            else if (cbRounding.SelectedIndex == 3) rounding = 60;

            TimeSpan.TryParse(txtOtWindowStart.Text.Trim(), out TimeSpan otStart);
            TimeSpan.TryParse(txtOtWindowEnd.Text.Trim(), out TimeSpan otEnd);
            if (otStart == TimeSpan.Zero) otStart = et;
            if (otEnd == TimeSpan.Zero) otEnd = et.Add(TimeSpan.FromHours(4));

            // Kullanıcı tercihi: Eskisi korunsun mu yoksa geçmiş de güncellensin mi?
            if (selectedShiftId > 0 && rbKeepPastAsIs.IsChecked == true)
            {
                // Kullanıcı eski kayıtlar korunsun istedi:
                // Mevcut kuralın ID'si yerine yeni bir ID ile yeni vardiya kuralı oluşturulur.
                // Böylece eski dönemlerdeki çizelgeler/personel kartları eski ShiftId'ye bağlı kalır,
                // yeni kural bugünden/ileriye yönelik olarak geçerli olur.
                var shift = new ShiftRule
                {
                    Id = 0, // Yeni kural olarak ekle
                    Name = name + " (Güncel " + DateTime.Now.ToString("dd.MM.yyyy") + ")",
                    StartTime = st,
                    EndTime = et,
                    GracePeriodMinutes = grace,
                    LunchBreakMinutes = lunch,
                    DuplicateFilterSeconds = dup,
                    IsOvernight = chkOvernight.IsChecked == true,
                    MaxDailyOvertimeHours = maxOt > 0 ? maxOt : 3.0,
                    MaxWeeklyOvertimeHours = maxWeeklyOt > 0 ? maxWeeklyOt : 15.0,
                    MaxMonthlyOvertimeHours = maxMonthlyOt > 0 ? maxMonthlyOt : 45.0,
                    ExcludeOvernightOvertime = chkExcludeOvernightOt.IsChecked == true,
                    DisableOvertimeLimits = chkDisableOtLimits.IsChecked == true,
                    MinOvertimeThresholdMinutes = minOt >= 0 ? minOt : 30.0,
                    RequiredDailyWorkHours = reqWork > 0 ? reqWork : 8.0,
                    AllowEarlyInOvertime = chkAllowEarlyInOt.IsChecked == true,
                    EarlyInMaxOvertimeHours = earlyMaxOt > 0 ? earlyMaxOt : 1.0,
                    OvertimeWindowStart = otStart,
                    OvertimeWindowEnd = otEnd,
                    TargetGroup = targetGrp,
                    ApplyToAllMatchingEmployees = chkApplyToAllMatching.IsChecked == true,
                    WeekdayMultiplier = wkdMult > 0 ? wkdMult : 1.5,
                    WeekendMultiplier = wknMult > 0 ? wknMult : 2.0,
                    HolidayMultiplier = holMult > 0 ? holMult : 2.0,
                    RoundingMinutes = rounding
                };

                dbService.SaveShift(shift);
                LoadShifts();
                BtnClear_Click(null, null);
                MessageBox.Show("Eski mesai programı geçmiş veriler için aynen korundu!\n\nYeni mesai programı bugünden/bundan sonraki dönemler için yeni bir kural olarak başarıyla eklendi.", "Yeni Mesai Programı Oluşturuldu", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                // Geçmişi de güncelle (Doğrudan mevcut kuralı güncelle veya yeni kural ekle)
                var shift = new ShiftRule
                {
                    Id = selectedShiftId,
                    Name = name,
                    StartTime = st,
                    EndTime = et,
                    GracePeriodMinutes = grace,
                    LunchBreakMinutes = lunch,
                    DuplicateFilterSeconds = dup,
                    IsOvernight = chkOvernight.IsChecked == true,
                    MaxDailyOvertimeHours = maxOt > 0 ? maxOt : 3.0,
                    MaxWeeklyOvertimeHours = maxWeeklyOt > 0 ? maxWeeklyOt : 15.0,
                    MaxMonthlyOvertimeHours = maxMonthlyOt > 0 ? maxMonthlyOt : 45.0,
                    ExcludeOvernightOvertime = chkExcludeOvernightOt.IsChecked == true,
                    DisableOvertimeLimits = chkDisableOtLimits.IsChecked == true,
                    MinOvertimeThresholdMinutes = minOt >= 0 ? minOt : 30.0,
                    RequiredDailyWorkHours = reqWork > 0 ? reqWork : 8.0,
                    AllowEarlyInOvertime = chkAllowEarlyInOt.IsChecked == true,
                    EarlyInMaxOvertimeHours = earlyMaxOt > 0 ? earlyMaxOt : 1.0,
                    OvertimeWindowStart = otStart,
                    OvertimeWindowEnd = otEnd,
                    TargetGroup = targetGrp,
                    ApplyToAllMatchingEmployees = chkApplyToAllMatching.IsChecked == true,
                    WeekdayMultiplier = wkdMult > 0 ? wkdMult : 1.5,
                    WeekendMultiplier = wknMult > 0 ? wknMult : 2.0,
                    HolidayMultiplier = holMult > 0 ? holMult : 2.0,
                    RoundingMinutes = rounding
                };

                dbService.SaveShift(shift);
                LoadShifts();
                BtnClear_Click(null, null);
                MessageBox.Show("Mesai programı güncellendi ve geçmiş/yeni tüm puantaj hesaplamalarına yansıtıldı.", "Bilgi", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void DgShifts_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgShifts.SelectedItem is ShiftRule selected)
            {
                selectedShiftId = selected.Id;
                txtShiftName.Text = selected.Name;
                txtStartTime.Text = selected.StartTime.ToString(@"hh\:mm\:ss");
                txtEndTime.Text = selected.EndTime.ToString(@"hh\:mm\:ss");
                txtGraceMinutes.Text = selected.GracePeriodMinutes.ToString();
                txtLunchMinutes.Text = selected.LunchBreakMinutes.ToString();
                txtDuplicateSeconds.Text = selected.DuplicateFilterSeconds.ToString();
                chkOvernight.IsChecked = selected.IsOvernight;
                txtMaxDailyOt.Text = selected.MaxDailyOvertimeHours.ToString("0.0");
                txtMaxWeeklyOt.Text = selected.MaxWeeklyOvertimeHours.ToString("0.0");
                txtMaxMonthlyOt.Text = selected.MaxMonthlyOvertimeHours.ToString("0.0");
                chkExcludeOvernightOt.IsChecked = selected.ExcludeOvernightOvertime;
                chkDisableOtLimits.IsChecked = selected.DisableOvertimeLimits;
                txtMinOtThreshold.Text = selected.MinOvertimeThresholdMinutes.ToString("0");
                txtReqWork.Text = selected.RequiredDailyWorkHours.ToString("0.0");
                chkAllowEarlyInOt.IsChecked = selected.AllowEarlyInOvertime;
                txtEarlyInMaxOt.Text = selected.EarlyInMaxOvertimeHours.ToString("0.0");
                txtOtWindowStart.Text = selected.OvertimeWindowStart.ToString(@"hh\:mm\:ss");
                txtOtWindowEnd.Text = selected.OvertimeWindowEnd.ToString(@"hh\:mm\:ss");
                cbTargetGroup.Text = selected.TargetGroup ?? "Tümü";
                chkApplyToAllMatching.IsChecked = selected.ApplyToAllMatchingEmployees;
                txtWeekdayMult.Text = selected.WeekdayMultiplier.ToString("0.0");
                txtWeekendMult.Text = selected.WeekendMultiplier.ToString("0.0");
                txtHolidayMult.Text = selected.HolidayMultiplier.ToString("0.0");

                if (selected.RoundingMinutes == 0) cbRounding.SelectedIndex = 0;
                else if (selected.RoundingMinutes == 15) cbRounding.SelectedIndex = 1;
                else if (selected.RoundingMinutes == 30) cbRounding.SelectedIndex = 2;
                else if (selected.RoundingMinutes == 60) cbRounding.SelectedIndex = 3;
            }
        }

        private void BtnClear_Click(object sender, RoutedEventArgs e)
        {
            selectedShiftId = 0;
            txtShiftName.Clear();
            txtStartTime.Text = "08:30:00";
            txtEndTime.Text = "18:00:00";
            txtGraceMinutes.Text = "10";
            txtLunchMinutes.Text = "60";
            txtDuplicateSeconds.Text = "60";
            chkOvernight.IsChecked = false;
            txtMaxDailyOt.Text = "3.0";
            txtMaxWeeklyOt.Text = "15.0";
            txtMaxMonthlyOt.Text = "45.0";
            chkExcludeOvernightOt.IsChecked = false;
            chkDisableOtLimits.IsChecked = false;
            txtMinOtThreshold.Text = "30";
            txtReqWork.Text = "8.0";
            chkAllowEarlyInOt.IsChecked = false;
            txtEarlyInMaxOt.Text = "1.0";
            txtOtWindowStart.Text = "18:00:00";
            txtOtWindowEnd.Text = "22:00:00";
            cbTargetGroup.SelectedIndex = 0;
            chkApplyToAllMatching.IsChecked = true;
            txtWeekdayMult.Text = "1.5";
            txtWeekendMult.Text = "2.0";
            txtHolidayMult.Text = "2.0";
            cbRounding.SelectedIndex = 1;
            dgShifts.SelectedItem = null;
        }

        private void BtnDeleteShift_Click(object sender, RoutedEventArgs e)
        {
            if (selectedShiftId <= 0)
            {
                MessageBox.Show("Lütfen tablodan silmek istediğiniz vardiyayı seçiniz.", "Bilgi", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var res = MessageBox.Show("Bu vardiya kuralını silmek istediğinize emin misiniz?", "Vardiya Silme Onayı", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (res == MessageBoxResult.Yes)
            {
                dbService.DeleteShift(selectedShiftId);
                LoadShifts();
                BtnClear_Click(null, null);
                MessageBox.Show("Vardiya silindi.", "Bilgi", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        // HIZLI ŞABLON BUTONLARI (Gündüz Standart, Gece, Part-Time, Erken Vardiya)
        private void BtnPresetDayStandard_Click(object sender, RoutedEventArgs e)
        {
            txtShiftName.Text = "Gündüz Standart (08:30 - 18:00)";
            txtStartTime.Text = "08:30:00";
            txtEndTime.Text = "18:00:00";
            txtReqWork.Text = "8.5";
            txtLunchMinutes.Text = "60";
            txtGraceMinutes.Text = "10";
            chkOvernight.IsChecked = false;
            txtOtWindowStart.Text = "18:00:00";
            txtOtWindowEnd.Text = "22:00:00";
            chkAllowEarlyInOt.IsChecked = false;
            txtEarlyInMaxOt.Text = "1.0";
            txtMaxDailyOt.Text = "3.0";
        }

        private void BtnPresetNight_Click(object sender, RoutedEventArgs e)
        {
            txtShiftName.Text = "Gece Vardiyası (22:00 - 06:00)";
            txtStartTime.Text = "22:00:00";
            txtEndTime.Text = "06:00:00";
            txtReqWork.Text = "7.5";
            txtLunchMinutes.Text = "45";
            txtGraceMinutes.Text = "10";
            chkOvernight.IsChecked = true;
            txtOtWindowStart.Text = "06:00:00";
            txtOtWindowEnd.Text = "09:00:00";
            chkAllowEarlyInOt.IsChecked = true;
            txtEarlyInMaxOt.Text = "1.0";
            txtMaxDailyOt.Text = "3.0";
        }

        private void BtnPresetPartTime_Click(object sender, RoutedEventArgs e)
        {
            txtShiftName.Text = "Part-Time / Yarı Zamanlı (13:00 - 18:00)";
            txtStartTime.Text = "13:00:00";
            txtEndTime.Text = "18:00:00";
            txtReqWork.Text = "5.0";
            txtLunchMinutes.Text = "15";
            txtGraceMinutes.Text = "10";
            chkOvernight.IsChecked = false;
            txtOtWindowStart.Text = "18:00:00";
            txtOtWindowEnd.Text = "20:00:00";
            chkAllowEarlyInOt.IsChecked = false;
            txtEarlyInMaxOt.Text = "0.5";
            txtMaxDailyOt.Text = "2.0";
        }

        private void BtnPresetEarly_Click(object sender, RoutedEventArgs e)
        {
            txtShiftName.Text = "Erken Vardiya (07:00 - 15:30)";
            txtStartTime.Text = "07:00:00";
            txtEndTime.Text = "15:30:00";
            txtReqWork.Text = "8.0";
            txtLunchMinutes.Text = "45";
            txtGraceMinutes.Text = "10";
            chkOvernight.IsChecked = false;
            txtOtWindowStart.Text = "15:30:00";
            txtOtWindowEnd.Text = "19:30:00";
            chkAllowEarlyInOt.IsChecked = true;
            txtEarlyInMaxOt.Text = "1.0";
            txtMaxDailyOt.Text = "3.0";
        }

        private void BtnHelp_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.FrameworkElement elem && elem.Tag is string tag)
            {
                ShowShiftHelp(tag);
            }
        }

        private void ShowShiftHelp(string tag)
        {
            string title = "Vardiya & Mesai Kuralı";
            string desc = "";
            string tip = "";

            switch (tag)
            {
                case "ShiftName":
                    title = "Vardiya Adı";
                    desc = "Tanımladığınız çalışma planının veya vardiyanın sistemdeki tanıtıcı adıdır. (Örn: Gündüz Standart, 1. Vardiya, Gece Ekibi, Taşeron Vardiyası vb.).";
                    break;

                case "ShiftHours":
                    title = "İşe Giriş ve Çıkış Saatleri";
                    desc = "Personelin mesaiye başlaması ve bitirmesi gereken resmi saatlerdir. Örneğin '08:30:00' iş başlangıcı, '18:00:00' iş çıkışıdır.";
                    tip = "Giriş ve çıkış saatleri arasındaki farktan yemek molası düşülerek personelin günlük standart süresi bulunur.";
                    break;

                case "ReqWorkLunch":
                    title = "Zorunlu Çalışma (Saat) ve Yemek Molası (dk)";
                    desc = "Zorunlu Çalışma: Personelin tam gün ücretini hak etmesi için tamamlaması gereken net çalışma süresidir (Örn: 8.0 veya 8.5 saat).\n\nYemek Molası: Gün ortasında personelin hakkı olan mola süresidir (Örn: 60 dk). Net çalışma hesaplanırken bu süre toplam süreden otomatik olarak düşülür.";
                    break;

                case "Overnight":
                    title = "Gece Vardiyası (Ertesi Güne Sarkan)";
                    desc = "Vardiya başlangıç saati akşam veya gece olup, bitiş saati ertesi günün sabahına denk geliyorsa (örneğin 22:00 - 06:00) bu kutucuk işaretlenmelidir. Sistem tarih geçişini otomatik yönetir.";
                    break;

                case "OtLimits":
                    title = "Tolerans ve Fazla Mesai Limitleri (Günlük, Haftalık, Aylık)";
                    desc = "Geç Kalma Toleransı (dk): İşe bu süre kadar geç gelen personelin süresi 'Geç Kaldı' sayılmaz (Örn: 10 dk).\n\nAsgari Mesai Eşiği (dk): Personelin çıkıştan sonra en az bu kadar süre fazladan kalması gerekir, aksi halde mesaiye sayılmaz (Örn: 30 dk).\n\nGünlük/Haftalık/Aylık Max Mesai: Yasal veya şirket içi belirlenen azami fazla mesai sınırlarıdır. Bu sınırı aşan mesailer otomatik olarak 'İptal / Aşan' hanesine yazılır.";
                    break;

                case "TargetGroup":
                    title = "Uygulanacak Grup / Statü";
                    desc = "Bu kuralın şirketteki hangi çalışan grubuna (Taşeron, Kadrolu, Üretim, Yönetim veya Tümü) otomatik olarak uygulanacağını belirler. Böylece taşeron personele farklı, fabrika personeline farklı kurallar çalıştırabilirsiniz.";
                    break;

                case "MorningOt":
                    title = "Sabah Erken Gelmeler Mesaiye Sayılsın";
                    desc = "İş başlangıç saatinden (örneğin 08:30) önce gelen personelin erken gelme süresinin mesaiye dahil edilip edilmeyeceğini belirler. 'Sabah Max Erken Mesai' alanı ile sabah erken gelmeden en fazla kaç saat mesai yazılabileceği sınırlandırılabilir (Örn: En fazla 1 saat).";
                    break;

                case "EveningWindow":
                    title = "Akşam Mesai Kabul Penceresi";
                    desc = "Personelin iş çıkışından sonra yaptığı fazla mesailerin hangi saat aralığında kabul edileceğini belirler (Örn: Başlangıç 18:00, Bitiş 22:00). Personel 22:00'den sonra çıksa dahi 22:00 sonrası süre mesaiye dahil edilmez (kırpılır).";
                    break;

                case "ExcludeOvernight":
                    title = "Akşamdan Sabaha Olan Mesailer Silinsin (Gece Sayılmasın)";
                    desc = "Gece 24:00 (00:00) sonrasına sarkan çalışmaların fazla mesai olarak yazılmasını engeller, 24:00'ten sonraki süreler otomatik olarak silinir / iptal edilir.";
                    break;

                case "DisableOtLimits":
                    title = "⚡ Fazla Mesai Silinmesin (Limit Yok / Tamamı Mesaiye Sayılsın)";
                    desc = "Bu seçenek işaretlendiğinde günlük, haftalık ve aylık tavan sınırları ile gece kırpmaları devre dışı kalır. Personel ne kadar fazla çalıştıysa tamamı onaylanan mesaiye eklenir ve hakedişe yazılır.";
                    tip = "Özel yoğun dönemler, şantiyeler veya limitsiz mesai hakkı verilen taşeron grupları için bu seçeneği kullanabilirsiniz.";
                    break;

                case "RetroactiveChoice":
                    title = "Mesai Programı Güncelleme & Geçmiş Veri Koruma";
                    desc = "Eğer mevcut bir mesai programında yanlışlık yapılmışsa 'Geçmişi de Güncelle' seçilerek eski aylar dahil tüm puantajlar yeni kurala göre düzeltilebilir.\n\nEğer eski taşeron/kadro kuralı eski aylarda aynen kalsın, yeni kural bugünden itibaren başlasın isteniyorsa 'Eski Veriler Korunsun' seçilmelidir.";
                    tip = "Böylece geçmiş bordrolar bozulmadan yeni mesai programına geçiş yapabilirsiniz.";
                    break;

                case "Multipliers":
                    title = "Mesai Çarpanları ve Yuvarlama";
                    desc = "Hafta içi (örn: 1.5 - %50 zamlı), Hafta sonu (örn: 2.0 - %100 zamlı) ve Resmi tatillerdeki çalışma katsayılarını belirler.\n\nMesai Yuvarlama: Personelin hak ettiği fazla mesainin 15 dk, 30 dk veya 60 dk katlarına yuvarlanarak bordrolaştırılmasını sağlar.";
                    break;

                default:
                    title = "Vardiya Kuralı Açıklaması";
                    desc = "Bu parametre PDKS hesaplama motorunun çalışma kurallarından biridir.";
                    break;
            }

            HelpInfoWindow.ShowHelp(this, title, desc, tip);
        }
    }
}
