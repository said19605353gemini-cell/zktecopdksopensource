const fs = require('fs');
const path = require('path');
const cp = require('child_process');

const appData = process.env.LOCALAPPDATA;
const dbPath = path.join(appData, 'ZkPdksApp', 'pdks.db');
console.log('Database path:', dbPath);
console.log('Exists:', fs.existsSync(dbPath));

// Let's create a quick C# script or runner to query SQLite or read setting
const runnerCs = `
using System;
using System.IO;
using System.Data.SQLite;

class Program {
    static void Main() {
        string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        string dbPath = Path.Combine(appData, "ZkPdksApp", "pdks.db");
        using (var conn = new SQLiteConnection("Data Source=" + dbPath + ";Version=3;")) {
            conn.Open();
            using (var cmd = new SQLiteCommand("SELECT name FROM sqlite_master WHERE type='table';", conn)) {
                using (var r = cmd.ExecuteReader()) {
                    while (r.Read()) Console.WriteLine("Table: " + r.GetString(0));
                }
            }
            try {
                using (var cmd = new SQLiteCommand("SELECT Key, Value FROM AppSettings;", conn)) {
                    using (var r = cmd.ExecuteReader()) {
                        while (r.Read()) Console.WriteLine("Setting: " + r.GetString(0) + " = " + r.GetString(1));
                    }
                }
            } catch (Exception ex) {
                Console.WriteLine("AppSettings error: " + ex.Message);
            }
        }
    }
}
`;

fs.writeFileSync('C:/Users/SaidNurs/Desktop/v5/zkteco/scratch/check_db.cs', runnerCs, 'utf8');

const csc = 'C:/Windows/Microsoft.NET/Framework/v4.0.30319/csc.exe';
const dller = 'C:/Users/SaidNurs/Desktop/v5/zkteco/bin/Release/net48/dller/System.Data.SQLite.dll';
cp.execSync(`"${csc}" /r:"${dller}" /out:C:/Users/SaidNurs/Desktop/v5/zkteco/scratch/check_db.exe C:/Users/SaidNurs/Desktop/v5/zkteco/scratch/check_db.cs`);

// Copy SQLite.Interop.dll next to check_db.exe
const interopSrc = 'C:/Users/SaidNurs/Desktop/v5/zkteco/bin/Release/net48/dller/x86/SQLite.Interop.dll';
const interopDest = 'C:/Users/SaidNurs/Desktop/v5/zkteco/scratch/SQLite.Interop.dll';
const sqliteDll = 'C:/Users/SaidNurs/Desktop/v5/zkteco/scratch/System.Data.SQLite.dll';
fs.copyFileSync(interopSrc, interopDest);
fs.copyFileSync(dller, sqliteDll);

const out = cp.execSync('C:/Users/SaidNurs/Desktop/v5/zkteco/scratch/check_db.exe').toString();
console.log(out);
