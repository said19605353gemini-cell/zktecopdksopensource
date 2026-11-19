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
        public string GroupName { get; set; } = "Genel"; // Grup Tanımı (Örn: Üretim, Yönetim, Saha, Vardiya A)
        public string Company { get; set; }
        public string Branch { get; set; }
        public string Department { get; set; }
        public string Unit { get; set; }
        public string Title { get; set; }
        public string CardNo { get; set; }
        public string Phone { get; set; }
        public string CompanyPhone { get; set; } // Şirket / Kurumsal Hat
        public string HireDate { get; set; }
        public string LeaveDate { get; set; }
        public string Status { get; set; } = "Aktif";
        public string EmploymentType { get; set; } = "Kadrolu"; // Kadrolu, Taşeron, Sözleşmeli, Stajyer, Ziyaretçi
        public int ShiftId { get; set; } = 1;
    }

    public class EmployeeGroup
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int DefaultShiftId { get; set; } = 1;
    }

    public class EmploymentTypeModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
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
                        RequiredDailyWorkHours REAL DEFAULT 8.0,
                        AllowEarlyInOvertime INTEGER DEFAULT 0,
                        EarlyInMaxOvertimeHours REAL DEFAULT 1.0,
                        OvertimeWindowStart TEXT DEFAULT '18:00:00',
                        OvertimeWindowEnd TEXT DEFAULT '22:00:00'
                    );";

                string createEmployeesTable = @"
                    CREATE TABLE IF NOT EXISTS Employees (
                        Badgenumber TEXT PRIMARY KEY,
                        TcNo TEXT,
                        Name TEXT NOT NULL,
                        GroupName TEXT DEFAULT 'Genel',
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

                string createGroupsTable = @"
                    CREATE TABLE IF NOT EXISTS EmployeeGroups (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Name TEXT NOT NULL UNIQUE,
                        Description TEXT,
                        DefaultShiftId INTEGER DEFAULT 1
                    );";

                string createEmploymentTypesTable = @"
                    CREATE TABLE IF NOT EXISTS EmploymentTypes (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Name TEXT NOT NULL UNIQUE,
                        Description TEXT
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

                string createLeavesTable = @"
                    CREATE TABLE IF NOT EXISTS LeaveRecords (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        UserId TEXT,
                        LeaveType TEXT,
                        StartDate TEXT,
                        EndDate TEXT,
                        Hours REAL DEFAULT 8.0,
                        IsPaid INTEGER DEFAULT 1,
                        Description TEXT
                    );";

                string createHolidaysTable = @"
                    CREATE TABLE IF NOT EXISTS PublicHolidays (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Year INTEGER NOT NULL,
                        Date TEXT NOT NULL,
                        CountryCode TEXT NOT NULL DEFAULT 'TR',
                        Name TEXT NOT NULL,
                        UNIQUE(Year, Date, CountryCode)
                    );";

                string createSchedulesTable = @"
                    CREATE TABLE IF NOT EXISTS EmployeeShiftSchedules (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        UserId TEXT NOT NULL,
                        ShiftId INTEGER NOT NULL,
                        StartDate TEXT NOT NULL,
                        EndDate TEXT,
                        Description TEXT
                    );";

                using (var cmd = new SQLiteCommand(createShiftsTable, conn)) cmd.ExecuteNonQuery();
                using (var cmd = new SQLiteCommand(createEmployeesTable, conn)) cmd.ExecuteNonQuery();
                using (var cmd = new SQLiteCommand(createGroupsTable, conn)) cmd.ExecuteNonQuery();
                using (var cmd = new SQLiteCommand(createEmploymentTypesTable, conn)) cmd.ExecuteNonQuery();
                using (var cmd = new SQLiteCommand(createLogsTable, conn)) cmd.ExecuteNonQuery();
                using (var cmd = new SQLiteCommand(createLeavesTable, conn)) cmd.ExecuteNonQuery();
                using (var cmd = new SQLiteCommand(createHolidaysTable, conn)) cmd.ExecuteNonQuery();
                using (var cmd = new SQLiteCommand(createSchedulesTable, conn)) cmd.ExecuteNonQuery();
                string createSettingsTable = @"
                    CREATE TABLE IF NOT EXISTS AppSettings (
                        Key TEXT PRIMARY KEY,
                        Value TEXT
                    );";
                using (var cmd = new SQLiteCommand(createSettingsTable, conn)) cmd.ExecuteNonQuery();

                // İlk varsayılan grupları ekle
                string[] defaultGroups = new[] { "Genel", "Üretim", "Yönetim", "Ofis / Muhasebe", "Saha / Sevkiyat", "Güvenlik", "Montaj / Kalite" };
                foreach (var dg in defaultGroups)
                {
                    using (var cmd = new SQLiteCommand("INSERT OR IGNORE INTO EmployeeGroups (Name, Description) VALUES (@name, 'Varsayılan Departman/Grup');", conn))
                    {
                        cmd.Parameters.AddWithValue("@name", dg);
                        cmd.ExecuteNonQuery();
                    }
                }

                // İlk varsayılan istihdam türlerini ekle (Taşeron, Kadrolu, vb.)
                string[] defaultTypes = new[] { "Kadrolu", "Taşeron", "Sözleşmeli", "Stajyer", "Yevmiyeli", "Dış Kaynak", "Ziyaretçi" };
                foreach (var dt in defaultTypes)
                {
                    using (var cmd = new SQLiteCommand("INSERT OR IGNORE INTO EmploymentTypes (Name, Description) VALUES (@name, 'Varsayılan İstihdam/Statü Türü');", conn))
                    {
                        cmd.Parameters.AddWithValue("@name", dt);
                        cmd.ExecuteNonQuery();
                    }
                }

                // Migrasyonlar
                MigrateColumn(conn, "Shifts", "MaxDailyOvertimeHours", "REAL DEFAULT 3.0");
                MigrateColumn(conn, "Shifts", "MinOvertimeThresholdMinutes", "REAL DEFAULT 30.0");
                MigrateColumn(conn, "Shifts", "RequiredDailyWorkHours", "REAL DEFAULT 8.0");
                MigrateColumn(conn, "Shifts", "AllowEarlyInOvertime", "INTEGER DEFAULT 0");
                MigrateColumn(conn, "Shifts", "EarlyInMaxOvertimeHours", "REAL DEFAULT 1.0");
                MigrateColumn(conn, "Shifts", "OvertimeWindowStart", "TEXT DEFAULT '18:00:00'");
                MigrateColumn(conn, "Shifts", "OvertimeWindowEnd", "TEXT DEFAULT '22:00:00'");
                MigrateColumn(conn, "Shifts", "TargetGroup", "TEXT DEFAULT 'Tümü'");
                MigrateColumn(conn, "Shifts", "ApplyToAllMatchingEmployees", "INTEGER DEFAULT 1");
                MigrateColumn(conn, "Shifts", "WeekdayMultiplier", "REAL DEFAULT 1.5");
                MigrateColumn(conn, "Shifts", "WeekendMultiplier", "REAL DEFAULT 2.0");
                MigrateColumn(conn, "Shifts", "HolidayMultiplier", "REAL DEFAULT 2.0");
                MigrateColumn(conn, "Shifts", "RoundingMinutes", "INTEGER DEFAULT 15");
                MigrateColumn(conn, "Shifts", "MaxWeeklyOvertimeHours", "REAL DEFAULT 15.0");
                MigrateColumn(conn, "Shifts", "MaxMonthlyOvertimeHours", "REAL DEFAULT 45.0");
                MigrateColumn(conn, "Shifts", "ExcludeOvernightOvertime", "INTEGER DEFAULT 0");
                MigrateColumn(conn, "Shifts", "DisableOvertimeLimits", "INTEGER DEFAULT 0");

                MigrateColumn(conn, "Employees", "EmploymentType", "TEXT DEFAULT 'Kadrolu'");
                MigrateColumn(conn, "Employees", "GroupName", "TEXT DEFAULT 'Genel'");
                MigrateColumn(conn, "Employees", "TcNo", "TEXT");
                MigrateColumn(conn, "Employees", "Company", "TEXT");
                MigrateColumn(conn, "Employees", "Branch", "TEXT");
                MigrateColumn(conn, "Employees", "Unit", "TEXT");
                MigrateColumn(conn, "Employees", "Title", "TEXT");
                MigrateColumn(conn, "Employees", "HireDate", "TEXT");
                MigrateColumn(conn, "Employees", "LeaveDate", "TEXT");
                MigrateColumn(conn, "Employees", "CompanyPhone", "TEXT");
                MigrateColumn(conn, "Employees", "Status", "TEXT DEFAULT 'Aktif'");
                MigrateColumn(conn, "Employees", "ShiftId", "INTEGER DEFAULT 1");
                MigrateColumn(conn, "PublicHolidays", "CountryCode", "TEXT DEFAULT 'TR'");

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

        public DateTime? GetLatestLogDate()
        {
            try
            {
                using (var conn = new SQLiteConnection(connectionString))
                {
                    conn.Open();
                    string query = @"SELECT MAX(LogTime) FROM AttendanceLogs;";
                    using (var cmd = new SQLiteCommand(query, conn))
                    {
                        var result = cmd.ExecuteScalar();
                        if (result != null && result != DBNull.Value)
                        {
                            string strDate = result.ToString();
                            if (DateTime.TryParse(strDate, out DateTime dt))
                            {
                                return dt;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogException("GetLatestLogDate", ex);
            }
            return null;
        }

        public List<Employee> GetAllEmployees()
        {
            var list = new List<Employee>();
            using (var conn = new SQLiteConnection(connectionString))
            {
                conn.Open();
                string query = @"SELECT Badgenumber, TcNo, Name, GroupName, EmploymentType, Company, Branch, Department, Unit, Title, 
                                        CardNo, Phone, CompanyPhone, HireDate, LeaveDate, Status, ShiftId 
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
                            GroupName = reader["GroupName"] != DBNull.Value && !string.IsNullOrWhiteSpace(reader["GroupName"].ToString()) ? reader["GroupName"].ToString() : "Genel",
                            EmploymentType = reader["EmploymentType"] != DBNull.Value ? reader["EmploymentType"].ToString() : "Kadrolu",
                            Company = reader["Company"]?.ToString(),
                            Branch = reader["Branch"]?.ToString(),
                            Department = reader["Department"]?.ToString(),
                            Unit = reader["Unit"]?.ToString(),
                            Title = reader["Title"]?.ToString(),
                            CardNo = reader["CardNo"]?.ToString(),
                            Phone = reader["Phone"]?.ToString(),
                            CompanyPhone = reader["CompanyPhone"] != DBNull.Value ? reader["CompanyPhone"]?.ToString() : "",
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
                string query = @"INSERT INTO Employees (Badgenumber, TcNo, Name, GroupName, EmploymentType, Company, Branch, Department, Unit, Title, CardNo, Phone, CompanyPhone, HireDate, LeaveDate, Status, ShiftId)
                                 VALUES (@badge, @tc, @name, @grp, @empType, @comp, @branch, @dept, @unit, @title, @card, @phone, @compPhone, @hire, @leave, @status, @shift)
                                 ON CONFLICT(Badgenumber) DO UPDATE SET
                                 TcNo=excluded.TcNo, Name=excluded.Name, GroupName=excluded.GroupName, EmploymentType=excluded.EmploymentType, Company=excluded.Company, Branch=excluded.Branch,
                                 Department=excluded.Department, Unit=excluded.Unit, Title=excluded.Title, CardNo=excluded.CardNo,
                                 Phone=excluded.Phone, CompanyPhone=excluded.CompanyPhone, HireDate=excluded.HireDate, LeaveDate=excluded.LeaveDate,
                                 Status=excluded.Status, ShiftId=excluded.ShiftId;";
                using (var cmd = new SQLiteCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@badge", emp.Badgenumber);
                    cmd.Parameters.AddWithValue("@tc", emp.TcNo ?? "");
                    cmd.Parameters.AddWithValue("@name", emp.Name);
                    cmd.Parameters.AddWithValue("@grp", string.IsNullOrWhiteSpace(emp.GroupName) ? "Genel" : emp.GroupName);
                    cmd.Parameters.AddWithValue("@empType", string.IsNullOrWhiteSpace(emp.EmploymentType) ? "Kadrolu" : emp.EmploymentType);
                    cmd.Parameters.AddWithValue("@comp", emp.Company ?? "");
                    cmd.Parameters.AddWithValue("@branch", emp.Branch ?? "");
                    cmd.Parameters.AddWithValue("@dept", emp.Department ?? "");
                    cmd.Parameters.AddWithValue("@unit", emp.Unit ?? "");
                    cmd.Parameters.AddWithValue("@title", emp.Title ?? "");
                    cmd.Parameters.AddWithValue("@card", emp.CardNo ?? "");
                    cmd.Parameters.AddWithValue("@phone", emp.Phone ?? "");
                    cmd.Parameters.AddWithValue("@compPhone", emp.CompanyPhone ?? "");
                    cmd.Parameters.AddWithValue("@hire", emp.HireDate ?? "");
                    cmd.Parameters.AddWithValue("@leave", emp.LeaveDate ?? "");
                    cmd.Parameters.AddWithValue("@status", emp.Status ?? "Aktif");
                    cmd.Parameters.AddWithValue("@shift", emp.ShiftId);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public List<string> GetAllGroupNames()
        {
            var list = new List<string> { "Genel", "Üretim", "Yönetim", "Ofis / Muhasebe", "Saha / Sevkiyat", "Güvenlik" };
            try
            {
                using (var conn = new SQLiteConnection(connectionString))
                {
                    conn.Open();
                    string query = @"SELECT DISTINCT GroupName FROM Employees WHERE GroupName IS NOT NULL AND TRIM(GroupName) != ''
                                     UNION 
                                     SELECT Name FROM EmployeeGroups;";
                    using (var cmd = new SQLiteCommand(query, conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string g = reader[0]?.ToString();
                            if (!string.IsNullOrWhiteSpace(g) && !list.Contains(g))
                            {
                                list.Add(g);
                            }
                        }
                    }
                }
            }
            catch { }
            return list;
        }

        public void SaveGroup(string groupName, string description = "")
        {
            if (string.IsNullOrWhiteSpace(groupName)) return;
            using (var conn = new SQLiteConnection(connectionString))
            {
                conn.Open();
                string query = "INSERT OR IGNORE INTO EmployeeGroups (Name, Description) VALUES (@name, @desc);";
                using (var cmd = new SQLiteCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@name", groupName.Trim());
                    cmd.Parameters.AddWithValue("@desc", description ?? "");
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public List<EmployeeGroup> GetAllGroupDetails()
        {
            var list = new List<EmployeeGroup>();
            try
            {
                using (var conn = new SQLiteConnection(connectionString))
                {
                    conn.Open();
                    string query = "SELECT Id, Name, Description FROM EmployeeGroups ORDER BY Id ASC;";
                    using (var cmd = new SQLiteCommand(query, conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            list.Add(new EmployeeGroup
                            {
                                Id = Convert.ToInt32(reader["Id"]),
                                Name = reader["Name"]?.ToString(),
                                Description = reader["Description"]?.ToString()
                            });
                        }
                    }
                }
            }
            catch { }
            return list;
        }

        public void DeleteGroup(int id)
        {
            try
            {
                using (var conn = new SQLiteConnection(connectionString))
                {
                    conn.Open();
                    using (var cmd = new SQLiteCommand("DELETE FROM EmployeeGroups WHERE Id = @id;", conn))
                    {
                        cmd.Parameters.AddWithValue("@id", id);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch { }
        }

        public List<EmploymentTypeModel> GetAllEmploymentTypeDetails()
        {
            var list = new List<EmploymentTypeModel>();
            try
            {
                using (var conn = new SQLiteConnection(connectionString))
                {
                    conn.Open();
                    string query = "SELECT Id, Name, Description FROM EmploymentTypes ORDER BY Id ASC;";
                    using (var cmd = new SQLiteCommand(query, conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            list.Add(new EmploymentTypeModel
                            {
                                Id = Convert.ToInt32(reader["Id"]),
                                Name = reader["Name"]?.ToString(),
                                Description = reader["Description"]?.ToString()
                            });
                        }
                    }
                }
            }
            catch { }
            return list;
        }

        public void SaveEmploymentType(string name, string description = "")
        {
            if (string.IsNullOrWhiteSpace(name)) return;
            try
            {
                using (var conn = new SQLiteConnection(connectionString))
                {
                    conn.Open();
                    string query = "INSERT OR IGNORE INTO EmploymentTypes (Name, Description) VALUES (@name, @desc);";
                    using (var cmd = new SQLiteCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@name", name.Trim());
                        cmd.Parameters.AddWithValue("@desc", description ?? "");
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch { }
        }

        public void DeleteShift(int id)
        {
            try
            {
                using (var conn = new SQLiteConnection(connectionString))
                {
                    conn.Open();
                    using (var cmd = new SQLiteCommand("DELETE FROM Shifts WHERE Id = @id;", conn))
                    {
                        cmd.Parameters.AddWithValue("@id", id);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch { }
        }

        public void DeleteEmploymentType(int id)
        {
            try
            {
                using (var conn = new SQLiteConnection(connectionString))
                {
                    conn.Open();
                    using (var cmd = new SQLiteCommand("DELETE FROM EmploymentTypes WHERE Id = @id;", conn))
                    {
                        cmd.Parameters.AddWithValue("@id", id);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch { }
        }

        public List<string> GetAllEmploymentTypes()
        {
            var list = new List<string>();
            try
            {
                using (var conn = new SQLiteConnection(connectionString))
                {
                    conn.Open();
                    string query = @"SELECT Name FROM EmploymentTypes
                                     UNION
                                     SELECT DISTINCT EmploymentType FROM Employees WHERE EmploymentType IS NOT NULL AND TRIM(EmploymentType) != '';";
                    using (var cmd = new SQLiteCommand(query, conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string t = reader[0]?.ToString();
                            if (!string.IsNullOrWhiteSpace(t) && !list.Contains(t))
                            {
                                list.Add(t);
                            }
                        }
                    }
                }
            }
            catch { }
            if (list.Count == 0)
            {
                list.AddRange(new[] { "Kadrolu", "Taşeron", "Sözleşmeli", "Stajyer", "Yevmiyeli", "Dış Kaynak", "Ziyaretçi" });
            }
            return list;
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
                        TimeSpan otStart = reader["OvertimeWindowStart"] != DBNull.Value && TimeSpan.TryParse(reader["OvertimeWindowStart"].ToString(), out var parsedOtStart)
                            ? parsedOtStart : et;
                        TimeSpan otEnd = reader["OvertimeWindowEnd"] != DBNull.Value && TimeSpan.TryParse(reader["OvertimeWindowEnd"].ToString(), out var parsedOtEnd)
                            ? parsedOtEnd : et.Add(TimeSpan.FromHours(4));

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
                            RequiredDailyWorkHours = reader["RequiredDailyWorkHours"] != DBNull.Value ? Convert.ToDouble(reader["RequiredDailyWorkHours"]) : 8.0,
                            AllowEarlyInOvertime = reader["AllowEarlyInOvertime"] != DBNull.Value && Convert.ToInt32(reader["AllowEarlyInOvertime"]) == 1,
                            EarlyInMaxOvertimeHours = reader["EarlyInMaxOvertimeHours"] != DBNull.Value ? Convert.ToDouble(reader["EarlyInMaxOvertimeHours"]) : 1.0,
                            OvertimeWindowStart = otStart,
                            OvertimeWindowEnd = otEnd,
                            TargetGroup = reader["TargetGroup"] != DBNull.Value ? reader["TargetGroup"].ToString() : "Tümü",
                            ApplyToAllMatchingEmployees = reader["ApplyToAllMatchingEmployees"] == DBNull.Value || Convert.ToInt32(reader["ApplyToAllMatchingEmployees"]) == 1,
                            WeekdayMultiplier = reader["WeekdayMultiplier"] != DBNull.Value ? Convert.ToDouble(reader["WeekdayMultiplier"]) : 1.5,
                            WeekendMultiplier = reader["WeekendMultiplier"] != DBNull.Value ? Convert.ToDouble(reader["WeekendMultiplier"]) : 2.0,
                            HolidayMultiplier = reader["HolidayMultiplier"] != DBNull.Value ? Convert.ToDouble(reader["HolidayMultiplier"]) : 2.0,
                            RoundingMinutes = reader["RoundingMinutes"] != DBNull.Value ? Convert.ToInt32(reader["RoundingMinutes"]) : 15,
                            MaxWeeklyOvertimeHours = reader["MaxWeeklyOvertimeHours"] != DBNull.Value ? Convert.ToDouble(reader["MaxWeeklyOvertimeHours"]) : 15.0,
                            MaxMonthlyOvertimeHours = reader["MaxMonthlyOvertimeHours"] != DBNull.Value ? Convert.ToDouble(reader["MaxMonthlyOvertimeHours"]) : 45.0,
                            ExcludeOvernightOvertime = reader["ExcludeOvernightOvertime"] != DBNull.Value && Convert.ToInt32(reader["ExcludeOvernightOvertime"]) == 1,
                            DisableOvertimeLimits = reader["DisableOvertimeLimits"] != DBNull.Value && Convert.ToInt32(reader["DisableOvertimeLimits"]) == 1
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
                    query = @"INSERT INTO Shifts (Name, StartTime, EndTime, GracePeriodMinutes, EarlyInToleranceMinutes, LunchBreakMinutes, DuplicateFilterSeconds, IsOvernight, MaxDailyOvertimeHours, MinOvertimeThresholdMinutes, RequiredDailyWorkHours, AllowEarlyInOvertime, EarlyInMaxOvertimeHours, OvertimeWindowStart, OvertimeWindowEnd, TargetGroup, ApplyToAllMatchingEmployees, WeekdayMultiplier, WeekendMultiplier, HolidayMultiplier, RoundingMinutes, MaxWeeklyOvertimeHours, MaxMonthlyOvertimeHours, ExcludeOvernightOvertime, DisableOvertimeLimits)
                             VALUES (@name, @st, @et, @grace, @early, @lunch, @dup, @night, @maxOt, @minOt, @reqWork, @allowEarly, @earlyMaxOt, @otStart, @otEnd, @tGroup, @applyAll, @wkdMult, @wknMult, @holMult, @roundMin, @maxWeeklyOt, @maxMonthlyOt, @excludeOvernight, @disableOtLimits);";
                }
                else
                {
                    query = @"UPDATE Shifts SET Name=@name, StartTime=@st, EndTime=@et, GracePeriodMinutes=@grace,
                             EarlyInToleranceMinutes=@early, LunchBreakMinutes=@lunch, DuplicateFilterSeconds=@dup, IsOvernight=@night,
                             MaxDailyOvertimeHours=@maxOt, MinOvertimeThresholdMinutes=@minOt, RequiredDailyWorkHours=@reqWork,
                             AllowEarlyInOvertime=@allowEarly, EarlyInMaxOvertimeHours=@earlyMaxOt, OvertimeWindowStart=@otStart, OvertimeWindowEnd=@otEnd,
                             TargetGroup=@tGroup, ApplyToAllMatchingEmployees=@applyAll, WeekdayMultiplier=@wkdMult, WeekendMultiplier=@wknMult, HolidayMultiplier=@holMult, RoundingMinutes=@roundMin,
                             MaxWeeklyOvertimeHours=@maxWeeklyOt, MaxMonthlyOvertimeHours=@maxMonthlyOt, ExcludeOvernightOvertime=@excludeOvernight, DisableOvertimeLimits=@disableOtLimits
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
                    cmd.Parameters.AddWithValue("@allowEarly", shift.AllowEarlyInOvertime ? 1 : 0);
                    cmd.Parameters.AddWithValue("@earlyMaxOt", shift.EarlyInMaxOvertimeHours);
                    cmd.Parameters.AddWithValue("@otStart", shift.OvertimeWindowStart.ToString(@"hh\:mm\:ss"));
                    cmd.Parameters.AddWithValue("@otEnd", shift.OvertimeWindowEnd.ToString(@"hh\:mm\:ss"));
                    cmd.Parameters.AddWithValue("@tGroup", string.IsNullOrWhiteSpace(shift.TargetGroup) ? "Tümü" : shift.TargetGroup);
                    cmd.Parameters.AddWithValue("@applyAll", shift.ApplyToAllMatchingEmployees ? 1 : 0);
                    cmd.Parameters.AddWithValue("@wkdMult", shift.WeekdayMultiplier > 0 ? shift.WeekdayMultiplier : 1.5);
                    cmd.Parameters.AddWithValue("@wknMult", shift.WeekendMultiplier > 0 ? shift.WeekendMultiplier : 2.0);
                    cmd.Parameters.AddWithValue("@holMult", shift.HolidayMultiplier > 0 ? shift.HolidayMultiplier : 2.0);
                    cmd.Parameters.AddWithValue("@roundMin", shift.RoundingMinutes >= 0 ? shift.RoundingMinutes : 15);
                    cmd.Parameters.AddWithValue("@maxWeeklyOt", shift.MaxWeeklyOvertimeHours);
                    cmd.Parameters.AddWithValue("@maxMonthlyOt", shift.MaxMonthlyOvertimeHours);
                    cmd.Parameters.AddWithValue("@excludeOvernight", shift.ExcludeOvernightOvertime ? 1 : 0);
                    cmd.Parameters.AddWithValue("@disableOtLimits", shift.DisableOvertimeLimits ? 1 : 0);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public Dictionary<string, string> GetHolidaysByYear(int year, string countryCode = "TR")
        {
            var dict = new Dictionary<string, string>();
            try
            {
                using (var conn = new SQLiteConnection(connectionString))
                {
                    conn.Open();
                    string query = "SELECT Date, Name FROM PublicHolidays WHERE Year = @year AND (CountryCode = @country OR (@country = 'TR' AND (CountryCode IS NULL OR CountryCode = ''))) ORDER BY Date ASC;";
                    using (var cmd = new SQLiteCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@year", year);
                        cmd.Parameters.AddWithValue("@country", string.IsNullOrEmpty(countryCode) ? "TR" : countryCode.ToUpperInvariant());
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                string d = reader["Date"]?.ToString();
                                string n = reader["Name"]?.ToString();
                                if (!string.IsNullOrEmpty(d))
                                {
                                    dict[d] = n;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogException("GetHolidaysByYear", ex);
            }
            return dict;
        }

        public void SaveHolidays(int year, Dictionary<string, string> holidays, string countryCode = "TR")
        {
            if (holidays == null || holidays.Count == 0) return;
            try
            {
                using (var conn = new SQLiteConnection(connectionString))
                {
                    conn.Open();
                    using (var trans = conn.BeginTransaction())
                    {
                        string query = @"INSERT OR REPLACE INTO PublicHolidays (Year, Date, Name, CountryCode) 
                                         VALUES (@year, @date, @name, @country);";
                        using (var cmd = new SQLiteCommand(query, conn, trans))
                        {
                            var pYear = cmd.Parameters.Add("@year", System.Data.DbType.Int32);
                            var pDate = cmd.Parameters.Add("@date", System.Data.DbType.String);
                            var pName = cmd.Parameters.Add("@name", System.Data.DbType.String);
                            var pCountry = cmd.Parameters.Add("@country", System.Data.DbType.String);
                            pCountry.Value = string.IsNullOrEmpty(countryCode) ? "TR" : countryCode.ToUpperInvariant();

                            pYear.Value = year;
                            foreach (var kvp in holidays)
                            {
                                pDate.Value = kvp.Key;
                                pName.Value = kvp.Value;
                                cmd.ExecuteNonQuery();
                            }
                        }
                        trans.Commit();
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogException("SaveHolidays", ex);
            }
        }

        public List<EmployeeShiftSchedule> GetAllShiftSchedules()
        {
            var list = new List<EmployeeShiftSchedule>();
            try
            {
                using (var conn = new SQLiteConnection(connectionString))
                {
                    conn.Open();
                    string query = @"SELECT es.Id, es.UserId, es.ShiftId, es.StartDate, es.EndDate, es.Description, s.Name as ShiftName
                                     FROM EmployeeShiftSchedules es
                                     LEFT JOIN Shifts s ON es.ShiftId = s.Id
                                     ORDER BY es.StartDate ASC;";
                    using (var cmd = new SQLiteCommand(query, conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            DateTime.TryParse(reader["StartDate"]?.ToString(), out DateTime st);
                            DateTime? et = null;
                            if (reader["EndDate"] != DBNull.Value && DateTime.TryParse(reader["EndDate"]?.ToString(), out DateTime parsedEt))
                            {
                                et = parsedEt;
                            }

                            list.Add(new EmployeeShiftSchedule
                            {
                                Id = Convert.ToInt32(reader["Id"]),
                                UserId = reader["UserId"]?.ToString(),
                                ShiftId = Convert.ToInt32(reader["ShiftId"]),
                                ShiftName = reader["ShiftName"]?.ToString() ?? $"Vardiya {reader["ShiftId"]}",
                                StartDate = st,
                                EndDate = et,
                                Description = reader["Description"]?.ToString()
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogException("GetAllShiftSchedules", ex);
            }
            return list;
        }

        public void SaveShiftSchedule(EmployeeShiftSchedule item)
        {
            try
            {
                using (var conn = new SQLiteConnection(connectionString))
                {
                    conn.Open();
                    string query;
                    if (item.Id == 0)
                    {
                        query = @"INSERT INTO EmployeeShiftSchedules (UserId, ShiftId, StartDate, EndDate, Description)
                                 VALUES (@uid, @sid, @sdate, @edate, @desc);";
                    }
                    else
                    {
                        query = @"UPDATE EmployeeShiftSchedules 
                                 SET UserId=@uid, ShiftId=@sid, StartDate=@sdate, EndDate=@edate, Description=@desc
                                 WHERE Id=@id;";
                    }

                    using (var cmd = new SQLiteCommand(query, conn))
                    {
                        if (item.Id != 0) cmd.Parameters.AddWithValue("@id", item.Id);
                        cmd.Parameters.AddWithValue("@uid", item.UserId);
                        cmd.Parameters.AddWithValue("@sid", item.ShiftId);
                        cmd.Parameters.AddWithValue("@sdate", item.StartDate.ToString("yyyy-MM-dd"));
                        cmd.Parameters.AddWithValue("@edate", item.EndDate.HasValue ? (object)item.EndDate.Value.ToString("yyyy-MM-dd") : DBNull.Value);
                        cmd.Parameters.AddWithValue("@desc", item.Description ?? "");
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogException("SaveShiftSchedule", ex);
            }
        }

        public void DeleteShiftSchedule(int id)
        {
            try
            {
                using (var conn = new SQLiteConnection(connectionString))
                {
                    conn.Open();
                    using (var cmd = new SQLiteCommand("DELETE FROM EmployeeShiftSchedules WHERE Id = @id;", conn))
                    {
                        cmd.Parameters.AddWithValue("@id", id);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogException("DeleteShiftSchedule", ex);
            }
        }

        public string GetSetting(string key, string defaultValue = null)
        {
            try
            {
                using (var conn = new SQLiteConnection(connectionString))
                {
                    conn.Open();
                    using (var cmd = new SQLiteCommand("SELECT Value FROM AppSettings WHERE Key = @key LIMIT 1;", conn))
                    {
                        cmd.Parameters.AddWithValue("@key", key);
                        var obj = cmd.ExecuteScalar();
                        if (obj != null && obj != DBNull.Value)
                        {
                            return obj.ToString();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogException("GetSetting: " + key, ex);
            }
            return defaultValue;
        }

        public void SetSetting(string key, string value)
        {
            try
            {
                using (var conn = new SQLiteConnection(connectionString))
                {
                    conn.Open();
                    using (var cmd = new SQLiteCommand("INSERT OR REPLACE INTO AppSettings (Key, Value) VALUES (@key, @val);", conn))
                    {
                        cmd.Parameters.AddWithValue("@key", key);
                        cmd.Parameters.AddWithValue("@val", value ?? "");
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogException("SetSetting: " + key, ex);
            }
        }
    }
}
