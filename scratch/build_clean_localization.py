# -*- coding: utf-8 -*-
import io, re

with io.open(r'C:\Users\SaidNurs\Desktop\v5\zkteco\LocalizationService.cs', 'r', encoding='utf-8') as f:
    text = f.read()

# Let's extract entries for each language cleanly:
# Let's find ranges:
# tr: 10 to 258
# en: 261 to 507
# de: 510 to 756
# ru: 759 to 1005
# ar: 1008 to 1254
# ku: 1257 to 1503
lines = text.splitlines(True)

def extract_entries(start_line, end_line):
    # grab all lines inside
    res = []
    for line in lines[start_line:end_line]:
        # only keep lines that are key-value or comments
        stripped = line.strip()
        if stripped.startswith('["') or stripped.startswith('//'):
            res.append(line)
    # Ensure all key-value lines end with comma
    fixed = []
    for line in res:
        stripped = line.strip()
        if stripped.startswith('["') and not stripped.endswith(','):
            fixed.append(line.rstrip('\r\n') + u',\n')
        else:
            fixed.append(line)
    return fixed

tr_entries = extract_entries(12, 258)
en_entries = extract_entries(263, 508)
de_entries = extract_entries(512, 757)
ru_entries = extract_entries(761, 1006)
ar_entries = extract_entries(1010, 1255)
ku_entries = extract_entries(1259, 1504)

print('Counts: tr=%d, en=%d, de=%d, ru=%d, ar=%d, ku=%d' % (len(tr_entries), len(en_entries), len(de_entries), len(ru_entries), len(ar_entries), len(ku_entries)))

out = []
out.append(u'''using System.Collections.Generic;

namespace ZkPdksApp
{
    public static class LocalizationService
    {
        public static string CurrentLanguage { get; set; } = "tr";

        private static readonly Dictionary<string, Dictionary<string, string>> Translations = new Dictionary<string, Dictionary<string, string>>
        {
''')

langs = [
    ('tr', tr_entries),
    ('en', en_entries),
    ('de', de_entries),
    ('ru', ru_entries),
    ('ar', ar_entries),
    ('ku', ku_entries),
]

for idx, (lang, entries) in enumerate(langs):
    out.append(u'            ["%s"] = new Dictionary<string, string>\n' % lang)
    out.append(u'            {\n')
    for e in entries:
        out.append(e)
    if idx == len(langs) - 1:
        out.append(u'            }\n')
    else:
        out.append(u'            },\n')

out.append(u'''        };

        public static string Get(string key)
        {
            if (Translations.ContainsKey(CurrentLanguage) && Translations[CurrentLanguage].ContainsKey(key))
            {
                return Translations[CurrentLanguage][key];
            }
            if (Translations["tr"].ContainsKey(key))
            {
                return Translations["tr"][key];
            }
            return key;
        }
    }
}
''')

with io.open(r'C:\Users\SaidNurs\Desktop\v5\zkteco\LocalizationService.cs', 'w', encoding='utf-8') as f:
    f.write(u''.join(out))

print('LocalizationService.cs cleanly generated!')
