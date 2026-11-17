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
            MessageBox.Show("Firma çalışma ve mesai kuralı kaydedildi.", "Bilgi", MessageBoxButton.OK, MessageBoxImage.Information);
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
    }
}
