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
                MessageBox.Show(LocalizationService.Get("Msg_SelectEmployeeFirst"), LocalizationService.Get("MsgTitle_Warning"), MessageBoxButton.OK, MessageBoxImage.Warning);
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
                MessageBox.Show(LocalizationService.Get("Msg_EndDateBeforeStartDate"), LocalizationService.Get("MsgTitle_Error"), MessageBoxButton.OK, MessageBoxImage.Error);
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
            MessageBox.Show(LocalizationService.Get("Msg_ShiftScheduleSaved"), LocalizationService.Get("MsgTitle_Success"), MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (selectedId <= 0)
            {
                MessageBox.Show(LocalizationService.Get("Msg_SelectScheduleToDelete"), LocalizationService.Get("MsgTitle_Info"), MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var res = MessageBox.Show(LocalizationService.Get("Msg_ScheduleDeleteConfirm"), LocalizationService.Get("MsgTitle_DeleteConfirm"), MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (res == MessageBoxResult.Yes)
            {
                dbService.DeleteShiftSchedule(selectedId);
                LoadSchedules();
                BtnClear_Click(null, null);
                MessageBox.Show(LocalizationService.Get("Msg_ScheduleDeleted"), LocalizationService.Get("MsgTitle_Info"), MessageBoxButton.OK, MessageBoxImage.Information);
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
