using System;
using System.Collections.ObjectModel;
using System.Windows;

namespace ZkPdksApp
{
    public partial class ShiftScheduleWindow : Window
    {
        private readonly DatabaseService dbService;
        public ObservableCollection<EmployeeShiftSchedule> Schedules { get; set; }
        private int selectedId = 0;

        public ShiftScheduleWindow(DatabaseService db)
        {
            InitializeComponent();
            dbService = db;
            Schedules = new ObservableCollection<EmployeeShiftSchedule>();
            dgSchedules.ItemsSource = Schedules;

            LoadData();
        }

        private void LoadData()
        {
            var employees = dbService.GetAllEmployees();
            cbEmployee.ItemsSource = employees;
            if (employees.Count > 0) cbEmployee.SelectedIndex = 0;

            var shifts = dbService.GetAllShifts();
            cbShift.ItemsSource = shifts;
            if (shifts.Count > 0) cbShift.SelectedIndex = 0;

            dpStartDate.SelectedDate = DateTime.Today;

            LoadSchedules();
        }

        private void LoadSchedules()
        {
            Schedules.Clear();
            var list = dbService.GetAllShiftSchedules();
            foreach (var item in list)
            {
                Schedules.Add(item);
            }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (cbEmployee.SelectedValue == null)
            {
                MessageBox.Show("Lütfen bir personel seçiniz.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (cbShift.SelectedValue == null)
            {
                MessageBox.Show("Lütfen uygulanacak vardiyayı seçiniz.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!dpStartDate.SelectedDate.HasValue)
            {
                MessageBox.Show("Lütfen başlangıç tarihini seçiniz.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string uid = cbEmployee.SelectedValue.ToString();
            int sid = Convert.ToInt32(cbShift.SelectedValue);
            DateTime sDate = dpStartDate.SelectedDate.Value;
            DateTime? eDate = dpEndDate.SelectedDate;

            if (eDate.HasValue && eDate.Value < sDate)
            {
                MessageBox.Show("Bitiş tarihi başlangıç tarihinden önce olamaz.", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var schedule = new EmployeeShiftSchedule
            {
                Id = selectedId,
                UserId = uid,
                ShiftId = sid,
                StartDate = sDate,
                EndDate = eDate,
                Description = txtDesc.Text?.Trim()
            };

            dbService.SaveShiftSchedule(schedule);
            LoadSchedules();
            BtnClear_Click(null, null);
            MessageBox.Show("Tarih bazlı vardiya çizelgesi kaydedildi!\nPuantaj hesaplamalarında bu tarihlerde otomatik olarak seçilen vardiya programı geçerli olacaktır.", "Başarılı", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (selectedId <= 0)
            {
                MessageBox.Show("Lütfen silinecek kaydı tablodan seçiniz.", "Bilgi", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var res = MessageBox.Show("Bu vardiya çizelge kaydını silmek istediğinize emin misiniz?", "Silme Onayı", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (res == MessageBoxResult.Yes)
            {
                dbService.DeleteShiftSchedule(selectedId);
                LoadSchedules();
                BtnClear_Click(null, null);
                MessageBox.Show("Vardiya çizelgesi silindi.", "Bilgi", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void BtnClear_Click(object sender, RoutedEventArgs e)
        {
            selectedId = 0;
            dpStartDate.SelectedDate = DateTime.Today;
            dpEndDate.SelectedDate = null;
            txtDesc.Clear();
            dgSchedules.SelectedItem = null;
        }

        private void DgSchedules_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (dgSchedules.SelectedItem is EmployeeShiftSchedule selected)
            {
                selectedId = selected.Id;
                cbEmployee.SelectedValue = selected.UserId;
                cbShift.SelectedValue = selected.ShiftId;
                dpStartDate.SelectedDate = selected.StartDate;
                dpEndDate.SelectedDate = selected.EndDate;
                txtDesc.Text = selected.Description;
            }
        }
    }
}
