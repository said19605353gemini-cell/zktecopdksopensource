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

            LoadShifts();
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
                RequiredDailyWorkHours = reqWork > 0 ? reqWork : 8.0
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
            txtMaxDailyOt.Text = "3.0";
            txtMinOtThreshold.Text = "30";
            txtReqWork.Text = "8.0";
            chkOvernight.IsChecked = false;
            dgShifts.SelectedItem = null;
        }
    }
}
