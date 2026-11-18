using System;
using System.Collections.ObjectModel;
using System.Windows;

namespace ZkPdksApp
{
    public partial class GroupManagementWindow : Window
    {
        private readonly DatabaseService dbService;
        public ObservableCollection<EmployeeGroup> Groups { get; set; }
        public ObservableCollection<EmploymentTypeModel> EmploymentTypes { get; set; }

        public GroupManagementWindow(DatabaseService db)
        {
            InitializeComponent();
            dbService = db;
            Groups = new ObservableCollection<EmployeeGroup>();
            EmploymentTypes = new ObservableCollection<EmploymentTypeModel>();

            dgGroups.ItemsSource = Groups;
            dgEmploymentTypes.ItemsSource = EmploymentTypes;

            LoadData();
        }

        private void LoadData()
        {
            Groups.Clear();
            var gList = dbService.GetAllGroupDetails();
            foreach (var g in gList)
            {
                Groups.Add(g);
            }

            EmploymentTypes.Clear();
            var tList = dbService.GetAllEmploymentTypeDetails();
            foreach (var t in tList)
            {
                EmploymentTypes.Add(t);
            }
        }

        private void BtnAddGroup_Click(object sender, RoutedEventArgs e)
        {
            string name = txtNewGroupName.Text.Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show("Lütfen bir grup adı giriniz.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            dbService.SaveGroup(name, txtNewGroupDesc.Text.Trim());
            txtNewGroupName.Text = "";
            txtNewGroupDesc.Text = "";
            LoadData();
        }

        private void BtnDeleteGroup_Click(object sender, RoutedEventArgs e)
        {
            if (dgGroups.SelectedItem is EmployeeGroup selected)
            {
                if (MessageBox.Show($"'{selected.Name}' grubunu silmek istediğinize emin misiniz?", "Onay", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    dbService.DeleteGroup(selected.Id);
                    LoadData();
                }
            }
            else
            {
                MessageBox.Show("Lütfen silmek için tablodan bir grup seçiniz.", "Bilgi", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void BtnAddEmploymentType_Click(object sender, RoutedEventArgs e)
        {
            string name = txtNewTypeName.Text.Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show("Lütfen bir istihdam türü / statü adı giriniz.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            dbService.SaveEmploymentType(name, txtNewTypeDesc.Text.Trim());
            txtNewTypeName.Text = "";
            txtNewTypeDesc.Text = "";
            LoadData();
        }

        private void BtnDeleteEmploymentType_Click(object sender, RoutedEventArgs e)
        {
            if (dgEmploymentTypes.SelectedItem is EmploymentTypeModel selected)
            {
                if (MessageBox.Show($"'{selected.Name}' istihdam türünü silmek istediğinize emin misiniz?", "Onay", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    dbService.DeleteEmploymentType(selected.Id);
                    LoadData();
                }
            }
            else
            {
                MessageBox.Show("Lütfen silmek için tablodan bir statü / tür seçiniz.", "Bilgi", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
    }
}
