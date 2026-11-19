const fs = require('fs');
const path = require('path');

const base = 'C:/Users/SaidNurs/Desktop/v5/zkteco';
const files = [
    'MainWindow.xaml.cs',
    'EmployeeWindow.xaml.cs',
    'GroupManagementWindow.xaml.cs',
    'ShiftWindow.xaml.cs',
    'ShiftScheduleWindow.xaml.cs',
    'IrregularPunchesWindow.xaml.cs',
    'DeviceSyncConfigWindow.xaml.cs'
];

const list = [];
files.forEach(f => {
    const full = path.join(base, f);
    const content = fs.readFileSync(full, 'utf8');
    const regex = /MessageBox\.Show\s*\(([^;]+)\)/g;
    let m;
    while ((m = regex.exec(content)) !== null) {
        list.push({ file: f, call: m[1].trim() });
    }
});

fs.writeFileSync('C:/Users/SaidNurs/Desktop/v5/zkteco/scratch/msgbox_list.json', JSON.stringify(list, null, 2), 'utf8');
console.log('Total MessageBox calls found:', list.length);
