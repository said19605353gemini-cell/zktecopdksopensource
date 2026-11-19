using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;

namespace ZkPdksApp
{
    public class HolidayItem
    {
        public string Date { get; set; } // yyyy-MM-dd
        public string LocalName { get; set; }
        public string Name { get; set; }
    }

    public class CountryInfo
    {
        public string Code { get; set; }          // e.g. TR, DE, GB, US, RU, AZ, SA, AE, IQ
        public string Name { get; set; }          // Display name
        public string Flag { get; set; }          // Flag emoji
        public string TimeZoneId { get; set; }    // Windows TimeZone Id
        public int FallbackUtcOffsetHours { get; set; } // Fallback UTC offset if timezone id fails

        // Aktif dile göre yerelleştirilmiş ülke adını döndürür
        public string LocalizedName
        {
            get
            {
                string key = $"Country_{Code}";
                string loc = LocalizationService.Get(key);
                return !string.IsNullOrEmpty(loc) && loc != key ? loc : Name;
            }
        }
    }

    public static class HolidayService
    {
        public static string CurrentCountryCode { get; set; } = "TR";

        public static readonly List<CountryInfo> SupportedCountries = new List<CountryInfo>
        {
            new CountryInfo { Code = "TR", Name = "Türkiye", Flag = "🇹🇷", TimeZoneId = "Turkey Standard Time", FallbackUtcOffsetHours = 3 },
            new CountryInfo { Code = "DE", Name = "Almanya (Deutschland)", Flag = "🇩🇪", TimeZoneId = "W. Europe Standard Time", FallbackUtcOffsetHours = 1 },
            new CountryInfo { Code = "GB", Name = "Birleşik Krallık (UK)", Flag = "🇬🇧", TimeZoneId = "GMT Standard Time", FallbackUtcOffsetHours = 0 },
            new CountryInfo { Code = "US", Name = "ABD (New York - Doğu)", Flag = "🇺🇸", TimeZoneId = "Eastern Standard Time", FallbackUtcOffsetHours = -5 },
            new CountryInfo { Code = "RU", Name = "Rusya (Moskova)", Flag = "🇷🇺", TimeZoneId = "Russian Standard Time", FallbackUtcOffsetHours = 3 },
            new CountryInfo { Code = "AZ", Name = "Azerbaycan (Bakü)", Flag = "🇦🇿", TimeZoneId = "Azerbaijan Standard Time", FallbackUtcOffsetHours = 4 },
            new CountryInfo { Code = "SA", Name = "Suudi Arabistan (Riyad)", Flag = "🇸🇦", TimeZoneId = "Arab Standard Time", FallbackUtcOffsetHours = 3 },
            new CountryInfo { Code = "AE", Name = "Birleşik Arap Emirlikleri (Dubai)", Flag = "🇦🇪", TimeZoneId = "Arabian Standard Time", FallbackUtcOffsetHours = 4 },
            new CountryInfo { Code = "IQ", Name = "Irak (Bağdat / Erbil)", Flag = "🇮🇶", TimeZoneId = "Arabic Standard Time", FallbackUtcOffsetHours = 3 },
            new CountryInfo { Code = "FR", Name = "Fransa (Paris)", Flag = "🇫🇷", TimeZoneId = "Romance Standard Time", FallbackUtcOffsetHours = 1 },
            new CountryInfo { Code = "NL", Name = "Hollanda (Amsterdam)", Flag = "🇳🇱", TimeZoneId = "W. Europe Standard Time", FallbackUtcOffsetHours = 1 },
            new CountryInfo { Code = "IT", Name = "İtalya (Roma)", Flag = "🇮🇹", TimeZoneId = "W. Europe Standard Time", FallbackUtcOffsetHours = 1 },
            new CountryInfo { Code = "KZ", Name = "Kazakistan (Astana)", Flag = "🇰🇿", TimeZoneId = "Central Asia Standard Time", FallbackUtcOffsetHours = 5 },
            new CountryInfo { Code = "UZ", Name = "Özbekistan (Taşkent)", Flag = "🇺🇿", TimeZoneId = "West Asia Standard Time", FallbackUtcOffsetHours = 5 },
            new CountryInfo { Code = "CN", Name = "Çin (Pekin / Şanghay)", Flag = "🇨🇳", TimeZoneId = "China Standard Time", FallbackUtcOffsetHours = 8 },
            new CountryInfo { Code = "ES", Name = "İspanya (Madrid)", Flag = "🇪🇸", TimeZoneId = "Romance Standard Time", FallbackUtcOffsetHours = 1 }
        };

        public static CountryInfo GetCurrentCountry()
        {
            var found = SupportedCountries.Find(c => c.Code.Equals(CurrentCountryCode, StringComparison.OrdinalIgnoreCase));
            return found ?? SupportedCountries[0];
        }

        // Cache key: countryCode_year
        private static readonly Dictionary<string, Dictionary<string, string>> CachedHolidays 
            = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);

        public static Dictionary<string, string> GetHolidaysForYear(int year, DatabaseService db = null, bool forceRefresh = false, string countryCode = null)
        {
            string country = string.IsNullOrEmpty(countryCode) ? CurrentCountryCode : countryCode.ToUpperInvariant();
            string cacheKey = $"{country}_{year}";

            if (!forceRefresh && CachedHolidays.ContainsKey(cacheKey) && CachedHolidays[cacheKey].Count > 0)
            {
                return CachedHolidays[cacheKey];
            }

            // 1. Check local DB
            if (!forceRefresh && db != null)
            {
                var dbHolidays = db.GetHolidaysByYear(year, country);
                if (dbHolidays != null && dbHolidays.Count > 0)
                {
                    CachedHolidays[cacheKey] = dbHolidays;
                    return dbHolidays;
                }
            }

            // 2. Fetch from Internet (Nager.Date Public Holidays API)
            var holidays = FetchFromInternet(year, country);

            // 3. Fallback for TR if internet failed
            if ((holidays == null || holidays.Count == 0) && country == "TR")
            {
                holidays = GetFallbackHolidays(year);
            }

            // 4. Save to DB
            if (db != null && holidays != null && holidays.Count > 0)
            {
                db.SaveHolidays(year, holidays, country);
            }

            CachedHolidays[cacheKey] = holidays ?? new Dictionary<string, string>();
            return CachedHolidays[cacheKey];
        }

        private static Dictionary<string, string> FetchFromInternet(int year, string countryCode)
        {
            var result = new Dictionary<string, string>();
            try
            {
                string url = $"https://date.nager.at/api/v3/PublicHolidays/{year}/{countryCode}";
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;

                var request = (HttpWebRequest)WebRequest.Create(url);
                request.Method = "GET";
                request.UserAgent = "ZkPdksApp-HolidayClient/1.0";
                request.Timeout = 7000;

                using (var response = (HttpWebResponse)request.GetResponse())
                using (var reader = new StreamReader(response.GetResponseStream(), System.Text.Encoding.UTF8))
                {
                    string json = reader.ReadToEnd();
                    var matches = Regex.Matches(json, "\"date\"\\s*:\\s*\"(?<date>[^\"]+)\"[^}]*?\"localName\"\\s*:\\s*\"(?<name>[^\"]+)\"");
                    foreach (Match m in matches)
                    {
                        string d = m.Groups["date"].Value;
                        string rawName = m.Groups["name"].Value;
                        string name = Regex.Unescape(rawName);
                        if (!string.IsNullOrEmpty(d) && !result.ContainsKey(d))
                        {
                            result[d] = name;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"İnternetten tatil verisi çekilirken hata (Yıl {year}, Ülke {countryCode}): {ex.Message}");
            }
            return result;
        }

        public static Dictionary<string, string> GetFallbackHolidays(int year)
        {
            var map = new Dictionary<string, string>
            {
                [$"{year}-01-01"] = "Yılbaşı",
                [$"{year}-04-23"] = "Ulusal Egemenlik ve Çocuk Bayramı",
                [$"{year}-05-01"] = "Emek ve Dayanışma Günü",
                [$"{year}-05-19"] = "Atatürk'ü Anma, Gençlik ve Spor Bayramı",
                [$"{year}-07-15"] = "Demokrasi ve Millî Birlik Günü",
                [$"{year}-08-30"] = "Zafer Bayramı",
                [$"{year}-10-29"] = "Cumhuriyet Bayramı"
            };

            if (year == 2024)
            {
                map["2024-04-10"] = "Ramazan Bayramı 1. Gün";
                map["2024-04-11"] = "Ramazan Bayramı 2. Gün";
                map["2024-04-12"] = "Ramazan Bayramı 3. Gün";
                map["2024-06-16"] = "Kurban Bayramı 1. Gün";
                map["2024-06-17"] = "Kurban Bayramı 2. Gün";
                map["2024-06-18"] = "Kurban Bayramı 3. Gün";
                map["2024-06-19"] = "Kurban Bayramı 4. Gün";
            }
            else if (year == 2025)
            {
                map["2025-03-30"] = "Ramazan Bayramı 1. Gün";
                map["2025-03-31"] = "Ramazan Bayramı 2. Gün";
                map["2025-04-01"] = "Ramazan Bayramı 3. Gün";
                map["2025-06-06"] = "Kurban Bayramı 1. Gün";
                map["2025-06-07"] = "Kurban Bayramı 2. Gün";
                map["2025-06-08"] = "Kurban Bayramı 3. Gün";
                map["2025-06-09"] = "Kurban Bayramı 4. Gün";
            }
            else if (year == 2026)
            {
                map["2026-03-20"] = "Ramazan Bayramı 1. Gün";
                map["2026-03-21"] = "Ramazan Bayramı 2. Gün";
                map["2026-03-22"] = "Ramazan Bayramı 3. Gün";
                map["2026-05-27"] = "Kurban Bayramı 1. Gün";
                map["2026-05-28"] = "Kurban Bayramı 2. Gün";
                map["2026-05-29"] = "Kurban Bayramı 3. Gün";
                map["2026-05-30"] = "Kurban Bayramı 4. Gün";
            }
            else if (year == 2027)
            {
                map["2027-03-10"] = "Ramazan Bayramı 1. Gün";
                map["2027-03-11"] = "Ramazan Bayramı 2. Gün";
                map["2027-03-12"] = "Ramazan Bayramı 3. Gün";
                map["2027-05-17"] = "Kurban Bayramı 1. Gün";
                map["2027-05-18"] = "Kurban Bayramı 2. Gün";
                map["2027-05-19"] = "Kurban Bayramı 3. Gün";
                map["2027-05-20"] = "Kurban Bayramı 4. Gün";
            }

            return map;
        }

        public static (DateTime Time, bool IsFromInternet, string ErrorMessage) GetCurrentCountryDateTimeSafe(string countryCode = null)
        {
            var country = !string.IsNullOrEmpty(countryCode) 
                ? SupportedCountries.Find(c => c.Code.Equals(countryCode, StringComparison.OrdinalIgnoreCase)) ?? GetCurrentCountry()
                : GetCurrentCountry();

            try
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
                string[] checkUrls = new[]
                {
                    "https://date.nager.at",
                    "https://www.cloudflare.com",
                    "https://www.microsoft.com",
                    "https://www.google.com"
                };

                foreach (var url in checkUrls)
                {
                    try
                    {
                        var req = (HttpWebRequest)WebRequest.Create(url);
                        req.Method = "HEAD";
                        req.Timeout = 4000;
                        req.UserAgent = "ZkPdksApp/1.0";
                        using (var resp = (HttpWebResponse)req.GetResponse())
                        {
                            string dateHeader = resp.Headers["Date"];
                            if (!string.IsNullOrEmpty(dateHeader) && DateTime.TryParse(dateHeader, out DateTime parsedUtc))
                            {
                                if (parsedUtc.Year < 2020 || parsedUtc.Year > 2040) continue;

                                DateTime utc = parsedUtc.ToUniversalTime();
                                try
                                {
                                    var tz = TimeZoneInfo.FindSystemTimeZoneById(country.TimeZoneId);
                                    return (TimeZoneInfo.ConvertTimeFromUtc(utc, tz), true, null);
                                }
                                catch
                                {
                                    return (utc.AddHours(country.FallbackUtcOffsetHours), true, null);
                                }
                            }
                        }
                    }
                    catch { }
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"GetCurrentCountryDateTimeSafe internet hatası: {ex.Message}");
            }

            try
            {
                DateTime utcLocal = DateTime.UtcNow;
                var tz = TimeZoneInfo.FindSystemTimeZoneById(country.TimeZoneId);
                return (TimeZoneInfo.ConvertTimeFromUtc(utcLocal, tz), false, "İnternet zaman sunucusuna ulaşılamadı. Yerel saat baz alınarak dönüştürüldü.");
            }
            catch
            {
                return (DateTime.UtcNow.AddHours(country.FallbackUtcOffsetHours), false, "İnternet zaman sunucusuna ulaşılamadı.");
            }
        }

        public static (DateTime Time, bool IsFromInternet, string ErrorMessage) GetCurrentTurkeyDateTimeSafe()
        {
            return GetCurrentCountryDateTimeSafe("TR");
        }

        public static DateTime GetCurrentCountryDateTime(string countryCode = null)
        {
            return GetCurrentCountryDateTimeSafe(countryCode).Time;
        }

        public static DateTime GetCurrentTurkeyDateTime()
        {
            return GetCurrentCountryDateTimeSafe("TR").Time;
        }
    }
}
