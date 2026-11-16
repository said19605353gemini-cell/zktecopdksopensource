using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace ZkPdksApp
{
    public partial class EmployeeWindow : Window
    {
        private readonly DatabaseService dbService;
        private readonly ZkService zkService;
        public ObservableCollection<Employee> Employees { get; set; }

        public EmployeeWindow(DatabaseService db, ZkService zk)
        {
            InitializeComponent();
            dbService = db;
            zkService = zk;
            Employees = new ObservableCollection<Employee>();
            dgEmployees.ItemsSource = Employees;

            LoadShifts();
            LoadEmployees();
        }

        private void LoadShifts()
        {
            var shifts = dbService.GetAllShifts();
            cbShifts.ItemsSource = shifts;
            if (shifts.Count > 0)
            {
                cbShifts.SelectedIndex = 0;
            }
        }

        private void LoadEmployees()
        {
            Employees.Clear();
            var list = dbService.GetAllEmployees();
            foreach (var item in list)
            {
                Employees.Add(item);
            }
            txtEmployeeCount.Text = $"({Employees.Count} Personel)";
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            string id = txtBadge.Text.Trim();
            string name = txtName.Text.Trim();

            if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show("Cihaz ID ve Ad Soyad alanları zorunludur.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string status = (cbStatus.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Aktif";
            int shiftId = (cbShifts.SelectedValue != null) ? Convert.ToInt32(cbShifts.SelectedValue) : 1;

            var emp = new Employee
            {
                Badgenumber = id,
                Name = name,
                TcNo = txtTcNo.Text.Trim(),
                CardNo = txtCard.Text.Trim(),
                Phone = txtPhone.Text.Trim(),
                Company = txtCompany.Text.Trim(),
                Branch = txtBranch.Text.Trim(),
                Department = txtDept.Text.Trim(),
                Unit = txtUnit.Text.Trim(),
                Title = txtTitle.Text.Trim(),
                Status = status,
                ShiftId = shiftId
            };

            dbService.SaveEmployee(emp);
            LoadEmployees();
            BtnClear_Click(null, null);
            MessageBox.Show("Personel özlük kartı başarıyla kaydedildi.", "Bilgi", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (dgEmployees.SelectedItem is Employee selected)
            {
                if (MessageBox.Show($"{selected.Name} personelini silmek istediğinize emin misiniz?", "Silme Onayı",
                    MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    dbService.DeleteEmployee(selected.Badgenumber);
                    LoadEmployees();
                    BtnClear_Click(null, null);
                }
            }
            else
            {
                MessageBox.Show("Lütfen silinecek personeli seçin.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void BtnFetchFromDevice_Click(object sender, RoutedEventArgs e)
        {
            if (!zkService.IsConnected)
            {
                MessageBox.Show("Cihaz bağlı değil. Lütfen önce ana ekrandan cihaza bağlanın.", "Hata", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            int count = 0;
            try
            {
                zkService.GetAllUserInfo((badge, name, card) =>
                {
                    var emp = new Employee
                    {
                        Badgenumber = badge,
                        Name = string.IsNullOrWhiteSpace(name) ? $"Personel {badge}" : name,
                        CardNo = card,
                        Company = "Merkez A.Ş.",
                        Branch = "Ana Şube",
                        Department = "Genel",
                        Status = "Aktif",
                        ShiftId = 1
                    };
                    dbService.SaveEmployee(emp);
                    count++;
                });

                LoadEmployees();
                MessageBox.Show($"{count} adet personel cihazdan çekilerek özlük kayıtları oluşturuldu.", "Başarılı", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Personeller çekilirken hata oluştu: " + ex.Message, "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DgEmployees_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgEmployees.SelectedItem is Employee selected)
            {
                txtBadge.Text = selected.Badgenumber;
                txtName.Text = selected.Name;
                txtTcNo.Text = selected.TcNo;
                txtCard.Text = selected.CardNo;
                txtPhone.Text = selected.Phone;
                txtCompany.Text = selected.Company;
                txtBranch.Text = selected.Branch;
                txtDept.Text = selected.Department;
                txtUnit.Text = selected.Unit;
                txtTitle.Text = selected.Title;
                cbShifts.SelectedValue = selected.ShiftId;

                foreach (ComboBoxItem item in cbStatus.Items)
                {
                    if (item.Content.ToString() == selected.Status)
                    {
                        cbStatus.SelectedItem = item;
                        break;
                    }
                }
            }
        }

        private void BtnClear_Click(object sender, RoutedEventArgs e)
        {
            txtBadge.Clear();
            txtName.Clear();
            txtTcNo.Clear();
            txtCard.Clear();
            txtPhone.Clear();
            txtUnit.Clear();
            txtTitle.Clear();
            cbStatus.SelectedIndex = 0;
            if (cbShifts.Items.Count > 0) cbShifts.SelectedIndex = 0;
            dgEmployees.SelectedItem = null;
        }
    }
}
