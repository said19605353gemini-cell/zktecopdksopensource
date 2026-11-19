using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using ClosedXML.Excel;
using Microsoft.Win32;

namespace ZkPdksApp
{
    public partial class IrregularPunchesWindow : Window
    {
        public ObservableCollection<ShiftAttendanceResult> IrregularList { get; set; }

        public IrregularPunchesWindow(IEnumerable<ShiftAttendanceResult> allResults)
        {
            InitializeComponent();
            IrregularList = new ObservableCollection<ShiftAttendanceResult>();
            dgIrregular.ItemsSource = IrregularList;

            LoadData(allResults);
        }

        private void LoadData(IEnumerable<ShiftAttendanceResult> allResults)
        {
            if (allResults == null) return;

            var list = allResults.Where(r => 
                !string.IsNullOrEmpty(r.Status) && 
                (r.Status.Contains("Düzensiz") || 
                 r.Status.Contains("Çıkış Yok") || 
                 r.Status.Contains("Tek Basım") || 
                 r.LateMinutes > 0 || 
                 r.EarlyLeaveMinutes > 0 ||
                 r.ActualLastOut == "-")
            ).OrderByDescending(r => r.DateString).ThenBy(r => r.EmployeeName).ToList();

            IrregularList.Clear();
            foreach (var item in list)
            {
                IrregularList.Add(item);
            }

            int missingOutCount = list.Count(r => r.ActualLastOut == "-" || r.Status.Contains("Çıkış Yok") || r.Status.Contains("Tek Basım"));
            int lateEarlyCount = list.Count(r => r.LateMinutes > 0 || r.EarlyLeaveMinutes > 0);

            txtTotalIrregular.Text = $"{list.Count} Kayıt";
            txtMissingOut.Text = $"{missingOutCount} Kayıt";
            txtLateEarly.Text = $"{lateEarlyCount} Kayıt";
        }

        private void BtnExportExcel_Click(object sender, RoutedEventArgs e)
        {
            if (IrregularList.Count == 0)
            {
                MessageBox.Show("Dışa aktarılacak düzensiz basım kaydı bulunmuyor.", "Bilgi", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var sfd = new SaveFileDialog
            {
                Filter = "Excel Dosyası (*.xlsx)|*.xlsx",
                FileName = $"PDKS_Duzensiz_Basim_Listesi_{DateTime.Now:yyyyMMdd_HHmm}.xlsx"
            };

            if (sfd.ShowDialog() == true)
            {
                try
                {
                    using (var workbook = new XLWorkbook())
                    {
                        var worksheet = workbook.Worksheets.Add("Düzensiz Basımlar");
                        worksheet.Cell(1, 1).Value = "PARMAK İZİ VE KART DÜZENSİZ BASIM LİSTESİ";
                        var titleRange = worksheet.Range(1, 1, 1, 10);
                        titleRange.Merge();
                        titleRange.Style.Font.Bold = true;
                        titleRange.Style.Font.FontSize = 14;
                        titleRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#991B1B");
                        titleRange.Style.Font.FontColor = XLColor.White;

                        string[] headers = { "Personel ID", "Ad Soyad", "Grup", "Statü", "Tarih", "Gün", "İlk Giriş", "Son Çıkış", "Geç Kalma (dk)", "İkaz / Açıklama" };
                        for (int col = 0; col < headers.Length; col++)
                        {
                            worksheet.Cell(3, col + 1).Value = headers[col];
                            worksheet.Cell(3, col + 1).Style.Font.Bold = true;
                            worksheet.Cell(3, col + 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#FEE2E2");
                            worksheet.Cell(3, col + 1).Style.Font.FontColor = XLColor.FromHtml("#991B1B");
                        }

                        int row = 4;
                        foreach (var item in IrregularList)
                        {
                            worksheet.Cell(row, 1).Value = item.UserId ?? "";
                            worksheet.Cell(row, 2).Value = item.EmployeeName ?? "";
                            worksheet.Cell(row, 3).Value = item.GroupName ?? "";
                            worksheet.Cell(row, 4).Value = item.EmploymentType ?? "";
                            worksheet.Cell(row, 5).Value = item.DateString ?? "";
                            worksheet.Cell(row, 6).Value = item.DayType ?? "";
                            worksheet.Cell(row, 7).Value = item.ActualFirstIn ?? "";
                            worksheet.Cell(row, 8).Value = item.ActualLastOut ?? "";
                            worksheet.Cell(row, 9).Value = item.LateMinutes;
                            worksheet.Cell(row, 10).Value = item.Status ?? "";

                            worksheet.Cell(row, 10).Style.Font.Bold = true;
                            worksheet.Cell(row, 10).Style.Font.FontColor = XLColor.FromHtml("#DC2626");
                            row++;
                        }

                        worksheet.Columns().AdjustToContents();
                        workbook.SaveAs(sfd.FileName);
                    }

                    MessageBox.Show("Düzensiz basım listesi başarıyla Excel'e aktarıldı!\n" + sfd.FileName, "Başarılı", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Excel oluşturma hatası: " + ex.Message, "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}
