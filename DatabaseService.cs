using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;

namespace ZkPdksApp
{
    public class Employee
    {
        public string Badgenumber { get; set; }
        public string TcNo { get; set; }
        public string Name { get; set; }
        public string Company { get; set; }
        public string Branch { get; set; }
        public string Department { get; set; }
        public string Unit { get; set; }
        public string Title { get; set; }
        public string CardNo { get; set; }
        public string Phone { get; set; }
        public string HireDate { get; set; }
        public string LeaveDate { get; set; }
        public string Status { get; set; } = "Aktif";
        public int ShiftId { get; set; } = 1;
    }

    public class DatabaseService
    {
        private readonly string dbPath;
        private readonly string connectionString;

        public DatabaseService()
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string appFolder = Path.Combine(appData, "ZkPdksApp");
            if (!Directory.Exists(appFolder))
            {
                Directory.CreateDirectory(appFolder);
            }
            dbPath = Path.Combine(appFolder, "pdks.db");
            connectionString = $"Data Source={dbPath};Version=3;";

            InitializeDatabase();
        }

        private void InitializeDatabase()
        {
            using (var conn = new SQLiteConnection(connectionString))
            {
                conn.Open();

                string createShiftsTable = @"
                    CREATE TABLE IF NOT EXISTS Shifts (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Name TEXT NOT NULL,
                        StartTime TEXT NOT NULL,
                        EndTime TEXT NOT NULL,
                        GracePeriodMinutes INTEGER DEFAULT 10,
                        EarlyInToleranceMinutes INTEGER DEFAULT 15,
                        LunchBreakMinutes INTEGER DEFAULT 60,
                        DuplicateFilterSeconds INTEGER DEFAULT 60,
                        IsOvernight INTEGER DEFAULT 0,
                        MaxDailyOvertimeHours REAL DEFAULT 3.0,
                        MinOvertimeThresholdMinutes REAL DEFAULT 30.0,
                        RequiredDailyWorkHours REAL DEFAULT 8.0
                    );";

                string createEmployeesTable = @"
                    CREATE TABLE IF NOT EXISTS Employees (
                        Badgenumber TEXT PRIMARY KEY,
                        TcNo TEXT,
                        Name TEXT NOT NULL,
                        Company TEXT,
                        Branch TEXT,
                        Department TEXT,
                        Unit TEXT,
                        Title TEXT,
                        CardNo TEXT,
                        Phone TEXT,
                        HireDate TEXT,
                        LeaveDate TEXT,
                        Status TEXT DEFAULT 'Aktif',
                        ShiftId INTEGER DEFAULT 1
                    );";

                string createLogsTable = @"
                    CREATE TABLE IF NOT EXISTS AttendanceLogs (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        UserId TEXT,
                        InOutMode TEXT,
                        LogTime TEXT,
                        VerifyMode TEXT,
                        UNIQUE(UserId, LogTime) ON CONFLICT IGNORE
                    );";

                using (var cmd = new SQLiteCommand(createShiftsTable, conn)) cmd.ExecuteNonQuery();
                using (var cmd = new SQLiteCommand(createEmployeesTable, conn)) cmd.ExecuteNonQuery();
                using (var cmd = new SQLiteCommand(createLogsTable, conn)) cmd.ExecuteNonQuery();

                // Migrasyonlar
                MigrateColumn(conn, "Shifts", "MaxDailyOvertimeHours", "REAL DEFAULT 3.0");
                MigrateColumn(conn, "Shifts", "MinOvertimeThresholdMinutes", "REAL DEFAULT 30.0");
                MigrateColumn(conn, "Shifts", "RequiredDailyWorkHours", "REAL DEFAULT 8.0");

                MigrateColumn(conn, "Employees", "TcNo", "TEXT");
                MigrateColumn(conn, "Employees", "Company", "TEXT");
                MigrateColumn(conn, "Employees", "Branch", "TEXT");
                MigrateColumn(conn, "Employees", "Unit", "TEXT");
                MigrateColumn(conn, "Employees", "Title", "TEXT");
                MigrateColumn(conn, "Employees", "HireDate", "TEXT");
                MigrateColumn(conn, "Employees", "LeaveDate", "TEXT");
                MigrateColumn(conn, "Employees", "Status", "TEXT DEFAULT 'Aktif'");
                MigrateColumn(conn, "Employees", "ShiftId", "INTEGER DEFAULT 1");

                // Varsayılan vardiyayı ekle
                string checkShift = "SELECT COUNT(*) FROM Shifts;";
                using (var cmd = new SQLiteCommand(checkShift, conn))
                {
                    long count = (long)cmd.ExecuteScalar();
                    if (count == 0)
                    {
                        string insertDefaultShifts = @"
                            INSERT INTO Shifts (Name, StartTime, EndTime, GracePeriodMinutes, LunchBreakMinutes, DuplicateFilterSeconds, IsOvernight, MaxDailyOvertimeHours, MinOvertimeThresholdMinutes, RequiredDailyWorkHours)
                            VALUES ('Firma Standart Mesai (08:30 - 18:00)', '08:30:00', '18:00:00', 10, 60, 60, 0, 3.0, 30.0, 8.5);
                            INSERT INTO Shifts (Name, StartTime, EndTime, GracePeriodMinutes, LunchBreakMinutes, DuplicateFilterSeconds, IsOvernight, MaxDailyOvertimeHours, MinOvertimeThresholdMinutes, RequiredDailyWorkHours)
                            VALUES ('Gece Vardiyası (22:00 - 06:00)', '22:00:00', '06:00:00', 10, 45, 60, 1, 3.0, 30.0, 7.25);
                        ";
                        using (var insCmd = new SQLiteCommand(insertDefaultShifts, conn)) insCmd.ExecuteNonQuery();
                    }
                }
            }
        }

        private void MigrateColumn(SQLiteConnection conn, string table, string column, string typeDefinition)
        {
            try
            {
                string sql = $"ALTER TABLE {table} ADD COLUMN {column} {typeDefinition};";
                using (var cmd = new SQLiteCommand(sql, conn))
                {
                    cmd.ExecuteNonQuery();
                }
            }
            catch { }
        }

        public void SaveLogsBulk(IEnumerable<AttendanceLogModel> logs)
        {
            using (var conn = new SQLiteConnection(connectionString))
            {
                conn.Open();
                using (var trans = conn.BeginTransaction())
                {
                    string query = @"INSERT OR IGNORE INTO AttendanceLogs (UserId, InOutMode, LogTime, VerifyMode)
                                     VALUES (@userId, @inOut, @time, @verify);";
                    using (var cmd = new SQLiteCommand(query, conn, trans))
                    {
                        var pUser = cmd.Parameters.Add("@userId", System.Data.DbType.String);
                        var pInOut = cmd.Parameters.Add("@inOut", System.Data.DbType.String);
                        var pTime = cmd.Parameters.Add("@time", System.Data.DbType.String);
                        var pVerify = cmd.Parameters.Add("@verify", System.Data.DbType.String);

                        foreach (var log in logs)
                        {
                            pUser.Value = log.UserId;
                            pInOut.Value = log.InOutMode;
                            pTime.Value = log.LogTime;
                            pVerify.Value = log.VerifyMode;
                            cmd.ExecuteNonQuery();
                        }
                    }
                    trans.Commit();
                }
            }
        }

        public List<AttendanceLogModel> GetAllLogs()
        {
            var list = new List<AttendanceLogModel>();
            using (var conn = new SQLiteConnection(connectionString))
            {
                conn.Open();
                string query = @"SELECT l.UserId, COALESCE(e.Name, 'Personel ' || l.UserId) AS UserName, 
                                        l.InOutMode, l.LogTime, l.VerifyMode 
                                 FROM AttendanceLogs l
                                 LEFT JOIN Employees e ON l.UserId = e.Badgenumber
                                 ORDER BY l.LogTime DESC;";
                using (var cmd = new SQLiteCommand(query, conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(new AttendanceLogModel
                        {
                            UserId = reader["UserId"]?.ToString(),
                            UserName = reader["UserName"]?.ToString(),
                            InOutMode = reader["InOutMode"]?.ToString(),
                            LogTime = reader["LogTime"]?.ToString(),
                            VerifyMode = reader["VerifyMode"]?.ToString()
                        });
                    }
                }
            }
            return list;
        }

        public List<Employee> GetAllEmployees()
        {
            var list = new List<Employee>();
            using (var conn = new SQLiteConnection(connectionString))
            {
                conn.Open();
                string query = @"SELECT Badgenumber, TcNo, Name, Company, Branch, Department, Unit, Title, 
                                        CardNo, Phone, HireDate, LeaveDate, Status, ShiftId 
                                 FROM Employees ORDER BY Name ASC;";
                using (var cmd = new SQLiteCommand(query, conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(new Employee
                        {
                            Badgenumber = reader["Badgenumber"]?.ToString(),
                            TcNo = reader["TcNo"]?.ToString(),
                            Name = reader["Name"]?.ToString(),
                            Company = reader["Company"]?.ToString(),
                            Branch = reader["Branch"]?.ToString(),
                            Department = reader["Department"]?.ToString(),
                            Unit = reader["Unit"]?.ToString(),
                            Title = reader["Title"]?.ToString(),
                            CardNo = reader["CardNo"]?.ToString(),
                            Phone = reader["Phone"]?.ToString(),
                            HireDate = reader["HireDate"]?.ToString(),
                            LeaveDate = reader["LeaveDate"]?.ToString(),
                            Status = reader["Status"]?.ToString() ?? "Aktif",
                            ShiftId = reader["ShiftId"] != DBNull.Value ? Convert.ToInt32(reader["ShiftId"]) : 1
                        });
                    }
                }
            }
            return list;
        }

        public void SaveEmployee(Employee emp)
        {
            using (var conn = new SQLiteConnection(connectionString))
            {
                conn.Open();
                string query = @"INSERT INTO Employees (Badgenumber, TcNo, Name, Company, Branch, Department, Unit, Title, CardNo, Phone, HireDate, LeaveDate, Status, ShiftId)
                                 VALUES (@badge, @tc, @name, @comp, @branch, @dept, @unit, @title, @card, @phone, @hire, @leave, @status, @shift)
                                 ON CONFLICT(Badgenumber) DO UPDATE SET
                                 TcNo=excluded.TcNo, Name=excluded.Name, Company=excluded.Company, Branch=excluded.Branch,
                                 Department=excluded.Department, Unit=excluded.Unit, Title=excluded.Title, CardNo=excluded.CardNo,
                                 Phone=excluded.Phone, HireDate=excluded.HireDate, LeaveDate=excluded.LeaveDate,
                                 Status=excluded.Status, ShiftId=excluded.ShiftId;";
                using (var cmd = new SQLiteCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@badge", emp.Badgenumber);
                    cmd.Parameters.AddWithValue("@tc", emp.TcNo ?? "");
                    cmd.Parameters.AddWithValue("@name", emp.Name);
                    cmd.Parameters.AddWithValue("@comp", emp.Company ?? "");
                    cmd.Parameters.AddWithValue("@branch", emp.Branch ?? "");
                    cmd.Parameters.AddWithValue("@dept", emp.Department ?? "");
                    cmd.Parameters.AddWithValue("@unit", emp.Unit ?? "");
                    cmd.Parameters.AddWithValue("@title", emp.Title ?? "");
                    cmd.Parameters.AddWithValue("@card", emp.CardNo ?? "");
                    cmd.Parameters.AddWithValue("@phone", emp.Phone ?? "");
                    cmd.Parameters.AddWithValue("@hire", emp.HireDate ?? "");
                    cmd.Parameters.AddWithValue("@leave", emp.LeaveDate ?? "");
                    cmd.Parameters.AddWithValue("@status", emp.Status ?? "Aktif");
                    cmd.Parameters.AddWithValue("@shift", emp.ShiftId);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public void DeleteEmployee(string badgeNumber)
        {
            using (var conn = new SQLiteConnection(connectionString))
            {
                conn.Open();
                string query = "DELETE FROM Employees WHERE Badgenumber = @badge;";
                using (var cmd = new SQLiteCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@badge", badgeNumber);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public List<ShiftRule> GetAllShifts()
        {
            var list = new List<ShiftRule>();
            using (var conn = new SQLiteConnection(connectionString))
            {
                conn.Open();
                string query = "SELECT * FROM Shifts ORDER BY Id ASC;";
                using (var cmd = new SQLiteCommand(query, conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        TimeSpan st = TimeSpan.Parse(reader["StartTime"].ToString());
                        TimeSpan et = TimeSpan.Parse(reader["EndTime"].ToString());
                        list.Add(new ShiftRule
                        {
                            Id = Convert.ToInt32(reader["Id"]),
                            Name = reader["Name"]?.ToString(),
                            StartTime = st,
                            EndTime = et,
                            GracePeriodMinutes = Convert.ToInt32(reader["GracePeriodMinutes"]),
                            EarlyInToleranceMinutes = Convert.ToInt32(reader["EarlyInToleranceMinutes"]),
                            LunchBreakMinutes = Convert.ToInt32(reader["LunchBreakMinutes"]),
                            DuplicateFilterSeconds = Convert.ToInt32(reader["DuplicateFilterSeconds"]),
                            IsOvernight = Convert.ToInt32(reader["IsOvernight"]) == 1,
                            MaxDailyOvertimeHours = reader["MaxDailyOvertimeHours"] != DBNull.Value ? Convert.ToDouble(reader["MaxDailyOvertimeHours"]) : 3.0,
                            MinOvertimeThresholdMinutes = reader["MinOvertimeThresholdMinutes"] != DBNull.Value ? Convert.ToDouble(reader["MinOvertimeThresholdMinutes"]) : 30.0,
                            RequiredDailyWorkHours = reader["RequiredDailyWorkHours"] != DBNull.Value ? Convert.ToDouble(reader["RequiredDailyWorkHours"]) : 8.0
                        });
                    }
                }
            }
            return list;
        }

        public void SaveShift(ShiftRule shift)
        {
            using (var conn = new SQLiteConnection(connectionString))
            {
                conn.Open();
                string query;
                if (shift.Id == 0)
                {
                    query = @"INSERT INTO Shifts (Name, StartTime, EndTime, GracePeriodMinutes, EarlyInToleranceMinutes, LunchBreakMinutes, DuplicateFilterSeconds, IsOvernight, MaxDailyOvertimeHours, MinOvertimeThresholdMinutes, RequiredDailyWorkHours)
                             VALUES (@name, @st, @et, @grace, @early, @lunch, @dup, @night, @maxOt, @minOt, @reqWork);";
                }
                else
                {
                    query = @"UPDATE Shifts SET Name=@name, StartTime=@st, EndTime=@et, GracePeriodMinutes=@grace,
                             EarlyInToleranceMinutes=@early, LunchBreakMinutes=@lunch, DuplicateFilterSeconds=@dup, IsOvernight=@night,
                             MaxDailyOvertimeHours=@maxOt, MinOvertimeThresholdMinutes=@minOt, RequiredDailyWorkHours=@reqWork
                             WHERE Id=@id;";
                }

                using (var cmd = new SQLiteCommand(query, conn))
                {
                    if (shift.Id != 0) cmd.Parameters.AddWithValue("@id", shift.Id);
                    cmd.Parameters.AddWithValue("@name", shift.Name);
                    cmd.Parameters.AddWithValue("@st", shift.StartTime.ToString(@"hh\:mm\:ss"));
                    cmd.Parameters.AddWithValue("@et", shift.EndTime.ToString(@"hh\:mm\:ss"));
                    cmd.Parameters.AddWithValue("@grace", shift.GracePeriodMinutes);
                    cmd.Parameters.AddWithValue("@early", shift.EarlyInToleranceMinutes);
                    cmd.Parameters.AddWithValue("@lunch", shift.LunchBreakMinutes);
                    cmd.Parameters.AddWithValue("@dup", shift.DuplicateFilterSeconds);
                    cmd.Parameters.AddWithValue("@night", shift.IsOvernight ? 1 : 0);
                    cmd.Parameters.AddWithValue("@maxOt", shift.MaxDailyOvertimeHours);
                    cmd.Parameters.AddWithValue("@minOt", shift.MinOvertimeThresholdMinutes);
                    cmd.Parameters.AddWithValue("@reqWork", shift.RequiredDailyWorkHours);
                    cmd.ExecuteNonQuery();
                }
            }
        }
    }
}
