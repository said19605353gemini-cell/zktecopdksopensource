const fs = require('fs');

const dbFile = 'C:/Users/SaidNurs/Desktop/v5/zkteco/DatabaseService.cs';
let content = fs.readFileSync(dbFile, 'utf8');

// 1. Add createSettingsTable
const schedLine = 'using (var cmd = new SQLiteCommand(createSchedulesTable, conn)) cmd.ExecuteNonQuery();';
const settingsCreate = `
                string createSettingsTable = @"
                    CREATE TABLE IF NOT EXISTS AppSettings (
                        Key TEXT PRIMARY KEY,
                        Value TEXT
                    );";
                using (var cmd = new SQLiteCommand(createSettingsTable, conn)) cmd.ExecuteNonQuery();`;

if (content.includes(schedLine) && !content.includes('AppSettings')) {
    content = content.replace(schedLine, schedLine + settingsCreate);
}

// 2. Add GetSetting and SetSetting methods before the last closing brace
const settingsMethods = `
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
`;

const lastEnd = content.lastIndexOf('    }\r\n}');
const lastEndLF = content.lastIndexOf('    }\n}');

if (lastEnd !== -1) {
    content = content.slice(0, lastEnd) + settingsMethods;
} else if (lastEndLF !== -1) {
    content = content.slice(0, lastEndLF) + settingsMethods;
}

fs.writeFileSync(dbFile, content, 'utf8');
console.log('DatabaseService updated with AppSettings table and methods!');
