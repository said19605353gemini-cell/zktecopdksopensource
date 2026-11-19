# -*- coding: utf-8 -*-
import io

with io.open(r'C:\Users\SaidNurs\Desktop\v5\zkteco\LocalizationService.cs', 'r', encoding='utf-8') as f:
    text = f.read()

languages = ['tr', 'en', 'de', 'ru', 'ar', 'ku']
for i, lang in enumerate(languages):
    marker = '["' + lang + '"] = new Dictionary<string, string>'
    start = text.find(marker)
    if start == -1:
        print(lang, 'NOT FOUND')
        continue
    end = -1
    for next_lang in languages[i+1:]:
        m_next = '["' + next_lang + '"] = new Dictionary<string, string>'
        pos = text.find(m_next, start)
        if pos != -1:
            end = pos
            break
    if end == -1:
        end = text.find('public static string Get', start)
    
    sub = text[start:end]
    help_count = sub.count('HelpTitle_')
    device_sync_count = sub.count('BtnDeviceSyncConfig')
    print('Lang: %s, HelpTitle_ count: %d, BtnDeviceSyncConfig: %d' % (lang, help_count, device_sync_count))
