const fs = require('fs');

// 1. Update MainWindow.xaml.cs
let mw = fs.readFileSync('C:/Users/SaidNurs/Desktop/v5/zkteco/MainWindow.xaml.cs', 'utf8');

// Replace connect messages:
mw = mw.replace(
    'MessageBox.Show("ZKTeco cihazına başarıyla bağlanıldı!", "Bağlantı Kuruldu", MessageBoxButton.OK, MessageBoxImage.Information);',
    'MessageBox.Show(LocalizationService.Get("Msg_ConnectedSuccess"), LocalizationService.Get("MsgTitle_Connected"), MessageBoxButton.OK, MessageBoxImage.Information);'
);

mw = mw.replace(
    'MessageBox.Show("Cihaza bağlanılamadı. IP ve ağ erişiminizi kontrol edin.", "Bağlantı Hatası", MessageBoxButton.OK, MessageBoxImage.Error);',
    'MessageBox.Show(LocalizationService.Get("Msg_ConnFailed"), LocalizationService.Get("MsgTitle_ConnError"), MessageBoxButton.OK, MessageBoxImage.Error);'
);

mw = mw.replace(
    'MessageBox.Show("Lütfen önce cihaza bağlanın.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);',
    'MessageBox.Show(LocalizationService.Get("Msg_PleaseConnectFirst"), LocalizationService.Get("MsgTitle_Warning"), MessageBoxButton.OK, MessageBoxImage.Warning);'
);

mw = mw.replace(
    'MessageBox.Show("Dışa aktarılacak kayıt bulunmuyor.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);',
    'MessageBox.Show(LocalizationService.Get("Msg_NoDataToExport"), LocalizationService.Get("MsgTitle_Warning"), MessageBoxButton.OK, MessageBoxImage.Warning);'
);

mw = mw.replace(
    'MessageBox.Show("Dışa aktarılacak puantaj verisi bulunmuyor.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);',
    'MessageBox.Show(LocalizationService.Get("Msg_NoDataToExport"), LocalizationService.Get("MsgTitle_Warning"), MessageBoxButton.OK, MessageBoxImage.Warning);'
);

mw = mw.replace(
    'MessageBox.Show("Lütfen önce bir personel seçiniz.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);',
    'MessageBox.Show(LocalizationService.Get("Msg_SelectEmployeeFirst"), LocalizationService.Get("MsgTitle_Warning"), MessageBoxButton.OK, MessageBoxImage.Warning);'
);

mw = mw.replace(
    'MessageBox.Show("Seçilen personele ait gün gün çalışma kaydı bulunmuyor.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);',
    'MessageBox.Show(LocalizationService.Get("Msg_NoDailyLogsForEmp"), LocalizationService.Get("MsgTitle_Warning"), MessageBoxButton.OK, MessageBoxImage.Warning);'
);

mw = mw.replace(
    'MessageBox.Show("Veritabanında henüz kayıtlı bir geçiş logu bulunmuyor.", "Bilgi", MessageBoxButton.OK, MessageBoxImage.Information);',
    'MessageBox.Show(LocalizationService.Get("Msg_NoLogsInDb"), LocalizationService.Get("MsgTitle_Info"), MessageBoxButton.OK, MessageBoxImage.Information);'
);

mw = mw.replace(
    'MessageBox.Show("Gece/Gündüz ayrılmış detaylı mesai raporu Excel olarak kaydedildi!\\n" + sfd.FileName, "Başarılı", MessageBoxButton.OK, MessageBoxImage.Information);',
    'MessageBox.Show(string.Format(LocalizationService.Get("Msg_ExcelExportSuccess"), sfd.FileName), LocalizationService.Get("MsgTitle_Success"), MessageBoxButton.OK, MessageBoxImage.Information);'
);

mw = mw.replace(
    'MessageBox.Show("Excel raporu kaydedildi!\\n" + sfd.FileName, "Başarılı", MessageBoxButton.OK, MessageBoxImage.Information);',
    'MessageBox.Show(string.Format(LocalizationService.Get("Msg_ExcelExportSuccess"), sfd.FileName), LocalizationService.Get("MsgTitle_Success"), MessageBoxButton.OK, MessageBoxImage.Information);'
);

mw = mw.replace(
    'MessageBox.Show("Tüm personelin gün gün detaylı puantaj ve mesai raporu Excel olarak kaydedildi!\\n" + sfd.FileName, "Başarılı", MessageBoxButton.OK, MessageBoxImage.Information);',
    'MessageBox.Show(string.Format(LocalizationService.Get("Msg_ExcelExportSuccess"), sfd.FileName), LocalizationService.Get("MsgTitle_Success"), MessageBoxButton.OK, MessageBoxImage.Information);'
);

mw = mw.replace(
    'MessageBox.Show($"\'{emp.Name}\' personeline ait gün gün çalışma ve mesai raporu Excel olarak kaydedildi!\\n{sfd.FileName}", "Başarılı", MessageBoxButton.OK, MessageBoxImage.Information);',
    'MessageBox.Show(string.Format(LocalizationService.Get("Msg_ExcelExportSuccess"), sfd.FileName), LocalizationService.Get("MsgTitle_Success"), MessageBoxButton.OK, MessageBoxImage.Information);'
);

mw = mw.replace(
    'MessageBox.Show($"{fetchedList.Count} adet geçiş logu çekildi, 12 öncesi/sonrası mesai kuralları hesaplandı.", "Başarılı", MessageBoxButton.OK, MessageBoxImage.Information);',
    'MessageBox.Show(string.Format(LocalizationService.Get("Msg_LogsFetchedSuccess"), fetchedList.Count), LocalizationService.Get("MsgTitle_Success"), MessageBoxButton.OK, MessageBoxImage.Information);'
);

fs.writeFileSync('C:/Users/SaidNurs/Desktop/v5/zkteco/MainWindow.xaml.cs', mw, 'utf8');
console.log('MainWindow.xaml.cs updated.');

// 2. Update EmployeeWindow.xaml.cs
let emp = fs.readFileSync('C:/Users/SaidNurs/Desktop/v5/zkteco/EmployeeWindow.xaml.cs', 'utf8');
emp = emp.replace(
    'MessageBox.Show("Cihaz ID ve Ad Soyad alanları zorunludur.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);',
    'MessageBox.Show(LocalizationService.Get("Msg_EmpFieldsRequired"), LocalizationService.Get("MsgTitle_Warning"), MessageBoxButton.OK, MessageBoxImage.Warning);'
);
emp = emp.replace(
    'MessageBox.Show("Personel özlük kartı başarıyla kaydedildi.", "Bilgi", MessageBoxButton.OK, MessageBoxImage.Information);',
    'MessageBox.Show(LocalizationService.Get("Msg_EmpSavedSuccess"), LocalizationService.Get("MsgTitle_Info"), MessageBoxButton.OK, MessageBoxImage.Information);'
);
emp = emp.replace(
    'MessageBox.Show("Lütfen silinecek personeli seçin.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);',
    'MessageBox.Show(LocalizationService.Get("Msg_SelectEmpToDelete"), LocalizationService.Get("MsgTitle_Warning"), MessageBoxButton.OK, MessageBoxImage.Warning);'
);
emp = emp.replace(
    'MessageBox.Show("Cihaz bağlı değil. Lütfen önce ana ekrandan cihaza bağlanın.", "Hata", MessageBoxButton.OK, MessageBoxImage.Warning);',
    'MessageBox.Show(LocalizationService.Get("Msg_DeviceNotConnected"), LocalizationService.Get("MsgTitle_Warning"), MessageBoxButton.OK, MessageBoxImage.Warning);'
);
emp = emp.replace(
    'MessageBox.Show($"{selected.Name} personelini silmek istediğinize emin misiniz?", "Silme Onayı",',
    'MessageBox.Show(string.Format(LocalizationService.Get("Msg_EmpDeleteConfirm"), selected.Name), LocalizationService.Get("MsgTitle_DeleteConfirm"),'
);
emp = emp.replace(
    'MessageBox.Show($"{count} adet personel cihazdan çekilerek özlük kayıtları oluşturuldu.", "Başarılı", MessageBoxButton.OK, MessageBoxImage.Information);',
    'MessageBox.Show(string.Format(LocalizationService.Get("Msg_EmpsFetchedSuccess"), count), LocalizationService.Get("MsgTitle_Success"), MessageBoxButton.OK, MessageBoxImage.Information);'
);
fs.writeFileSync('C:/Users/SaidNurs/Desktop/v5/zkteco/EmployeeWindow.xaml.cs', emp, 'utf8');
console.log('EmployeeWindow.xaml.cs updated.');

// 3. Update GroupManagementWindow.xaml.cs
let grp = fs.readFileSync('C:/Users/SaidNurs/Desktop/v5/zkteco/GroupManagementWindow.xaml.cs', 'utf8');
grp = grp.replace(
    'MessageBox.Show("Lütfen bir grup adı giriniz.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);',
    'MessageBox.Show(LocalizationService.Get("Msg_GroupNameRequired"), LocalizationService.Get("MsgTitle_Warning"), MessageBoxButton.OK, MessageBoxImage.Warning);'
);
grp = grp.replace(
    'MessageBox.Show("Lütfen silmek için tablodan bir grup seçiniz.", "Bilgi", MessageBoxButton.OK, MessageBoxImage.Information);',
    'MessageBox.Show(LocalizationService.Get("Msg_SelectGroupToDelete"), LocalizationService.Get("MsgTitle_Info"), MessageBoxButton.OK, MessageBoxImage.Information);'
);
grp = grp.replace(
    'MessageBox.Show("Lütfen bir istihdam türü / statü adı giriniz.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);',
    'MessageBox.Show(LocalizationService.Get("Msg_EmploymentTypeNameRequired"), LocalizationService.Get("MsgTitle_Warning"), MessageBoxButton.OK, MessageBoxImage.Warning);'
);
grp = grp.replace(
    'MessageBox.Show("Lütfen silmek için tablodan bir statü / tür seçiniz.", "Bilgi", MessageBoxButton.OK, MessageBoxImage.Information);',
    'MessageBox.Show(LocalizationService.Get("Msg_SelectGroupToDelete"), LocalizationService.Get("MsgTitle_Info"), MessageBoxButton.OK, MessageBoxImage.Information);'
);
grp = grp.replace(
    'MessageBox.Show($"\'{selected.Name}\' grubunu silmek istediğinize emin misiniz?", "Onay", MessageBoxButton.YesNo, MessageBoxImage.Question)',
    'MessageBox.Show(string.Format(LocalizationService.Get("Msg_GroupDeleteConfirm"), selected.Name), LocalizationService.Get("MsgTitle_Confirm"), MessageBoxButton.YesNo, MessageBoxImage.Question)'
);
grp = grp.replace(
    'MessageBox.Show($"\'{selected.Name}\' istihdam türünü silmek istediğinize emin misiniz?", "Onay", MessageBoxButton.YesNo, MessageBoxImage.Question)',
    'MessageBox.Show(string.Format(LocalizationService.Get("Msg_EmploymentTypeDeleteConfirm"), selected.Name), LocalizationService.Get("MsgTitle_Confirm"), MessageBoxButton.YesNo, MessageBoxImage.Question)'
);
fs.writeFileSync('C:/Users/SaidNurs/Desktop/v5/zkteco/GroupManagementWindow.xaml.cs', grp, 'utf8');
console.log('GroupManagementWindow.xaml.cs updated.');

// 4. Update ShiftWindow.xaml.cs
let shf = fs.readFileSync('C:/Users/SaidNurs/Desktop/v5/zkteco/ShiftWindow.xaml.cs', 'utf8');
shf = shf.replace(
    'MessageBox.Show("Vardiya / kural adı zorunludur.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);',
    'MessageBox.Show(LocalizationService.Get("Msg_ShiftNameRequired"), LocalizationService.Get("MsgTitle_Warning"), MessageBoxButton.OK, MessageBoxImage.Warning);'
);
shf = shf.replace(
    'MessageBox.Show("Başlangıç ve bitiş saatleri geçerli formatta olmalıdır (Örn: 08:30:00).", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);',
    'MessageBox.Show(LocalizationService.Get("Msg_ShiftTimeFormatInvalid"), LocalizationService.Get("MsgTitle_Warning"), MessageBoxButton.OK, MessageBoxImage.Warning);'
);
shf = shf.replace(
    'MessageBox.Show("Bu vardiya kuralını silmek istediğinize emin misiniz?", "Vardiya Silme Onayı", MessageBoxButton.YesNo, MessageBoxImage.Question)',
    'MessageBox.Show(LocalizationService.Get("Msg_ShiftDeleteConfirm"), LocalizationService.Get("MsgTitle_DeleteConfirm"), MessageBoxButton.YesNo, MessageBoxImage.Question)'
);
shf = shf.replace(
    'MessageBox.Show("Vardiya silindi.", "Bilgi", MessageBoxButton.OK, MessageBoxImage.Information);',
    'MessageBox.Show(LocalizationService.Get("Msg_ShiftDeletedSuccess"), LocalizationService.Get("MsgTitle_Info"), MessageBoxButton.OK, MessageBoxImage.Information);'
);
fs.writeFileSync('C:/Users/SaidNurs/Desktop/v5/zkteco/ShiftWindow.xaml.cs', shf, 'utf8');
console.log('ShiftWindow.xaml.cs updated.');

// 5. Update ShiftScheduleWindow.xaml.cs
let ssch = fs.readFileSync('C:/Users/SaidNurs/Desktop/v5/zkteco/ShiftScheduleWindow.xaml.cs', 'utf8');
ssch = ssch.replace(
    'MessageBox.Show("Lütfen bir personel seçiniz.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);',
    'MessageBox.Show(LocalizationService.Get("Msg_SelectEmployeeFirst"), LocalizationService.Get("MsgTitle_Warning"), MessageBoxButton.OK, MessageBoxImage.Warning);'
);
ssch = ssch.replace(
    'MessageBox.Show("Bitiş tarihi başlangıç tarihinden önce olamaz.", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);',
    'MessageBox.Show(LocalizationService.Get("Msg_EndDateBeforeStartDate"), LocalizationService.Get("MsgTitle_Error"), MessageBoxButton.OK, MessageBoxImage.Error);'
);
ssch = ssch.replace(
    'MessageBox.Show("Tarih bazlı vardiya çizelgesi kaydedildi!\\nPuantaj hesaplamalarında bu tarihlerde otomatik olarak seçilen vardiya programı geçerli olacaktır.", "Başarılı", MessageBoxButton.OK, MessageBoxImage.Information);',
    'MessageBox.Show(LocalizationService.Get("Msg_ShiftScheduleSaved"), LocalizationService.Get("MsgTitle_Success"), MessageBoxButton.OK, MessageBoxImage.Information);'
);
ssch = ssch.replace(
    'MessageBox.Show("Lütfen silinecek kaydı tablodan seçiniz.", "Bilgi", MessageBoxButton.OK, MessageBoxImage.Information);',
    'MessageBox.Show(LocalizationService.Get("Msg_SelectScheduleToDelete"), LocalizationService.Get("MsgTitle_Info"), MessageBoxButton.OK, MessageBoxImage.Information);'
);
ssch = ssch.replace(
    'MessageBox.Show("Bu vardiya çizelge kaydını silmek istediğinize emin misiniz?", "Silme Onayı", MessageBoxButton.YesNo, MessageBoxImage.Question)',
    'MessageBox.Show(LocalizationService.Get("Msg_ScheduleDeleteConfirm"), LocalizationService.Get("MsgTitle_DeleteConfirm"), MessageBoxButton.YesNo, MessageBoxImage.Question)'
);
ssch = ssch.replace(
    'MessageBox.Show("Vardiya çizelgesi silindi.", "Bilgi", MessageBoxButton.OK, MessageBoxImage.Information);',
    'MessageBox.Show(LocalizationService.Get("Msg_ScheduleDeleted"), LocalizationService.Get("MsgTitle_Info"), MessageBoxButton.OK, MessageBoxImage.Information);'
);
fs.writeFileSync('C:/Users/SaidNurs/Desktop/v5/zkteco/ShiftScheduleWindow.xaml.cs', ssch, 'utf8');
console.log('ShiftScheduleWindow.xaml.cs updated.');
