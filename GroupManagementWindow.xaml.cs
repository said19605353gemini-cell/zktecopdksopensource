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
                MessageBox.Show(LocalizationService.Get("Msg_GroupNameRequired"), LocalizationService.Get("MsgTitle_Warning"), MessageBoxButton.OK, MessageBoxImage.Warning);
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
                if (MessageBox.Show(string.Format(LocalizationService.Get("Msg_GroupDeleteConfirm"), selected.Name), LocalizationService.Get("MsgTitle_Confirm"), MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    dbService.DeleteGroup(selected.Id);
                    LoadData();
                }
            }
            else
            {
                MessageBox.Show(LocalizationService.Get("Msg_SelectGroupToDelete"), LocalizationService.Get("MsgTitle_Info"), MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void BtnAddEmploymentType_Click(object sender, RoutedEventArgs e)
        {
            string name = txtNewTypeName.Text.Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show(LocalizationService.Get("Msg_EmploymentTypeNameRequired"), LocalizationService.Get("MsgTitle_Warning"), MessageBoxButton.OK, MessageBoxImage.Warning);
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
                if (MessageBox.Show(string.Format(LocalizationService.Get("Msg_EmploymentTypeDeleteConfirm"), selected.Name), LocalizationService.Get("MsgTitle_Confirm"), MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    dbService.DeleteEmploymentType(selected.Id);
                    LoadData();
                }
            }
            else
            {
                MessageBox.Show(LocalizationService.Get("Msg_SelectGroupToDelete"), LocalizationService.Get("MsgTitle_Info"), MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
    }
}
