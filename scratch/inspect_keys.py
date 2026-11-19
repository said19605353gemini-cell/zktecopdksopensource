# -*- coding: utf-8 -*-
import io, re

with io.open(r'C:\Users\SaidNurs\Desktop\v5\zkteco\LocalizationService.cs', 'r', encoding='utf-8') as f:
    text = f.read()

# Let's find every key in each dictionary
# Translations is Dictionary<string, Dictionary<string, string>>
langs = ['tr', 'en', 'de', 'ru', 'ar', 'ku']

# Let's see what keys each language actually has right now
for lang in langs:
    pattern = r'\["' + lang + r'"\]\s*=\s*new\s+Dictionary<string,\s*string>\s*\{(.*?)\}\s*,\s*\[|\}\s*;\s*public'
    m = re.search(pattern, text, re.DOTALL)
    if m:
        content = m.group(1) if m.group(1) is not None else ''
        keys = re.findall(r'\["([^"]+)"\]\s*=', content)
        print('Lang %s has %d keys' % (lang, len(keys)))
    else:
        print('Lang %s not matched' % lang)
