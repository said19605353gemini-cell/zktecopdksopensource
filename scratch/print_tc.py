# -*- coding: utf-8 -*-
import io, json

with io.open(r'C:\Users\SaidNurs\Desktop\v5\zkteco\scratch\step_843.json', 'r', encoding='utf-8') as f:
    s843 = json.load(f)

for tc in s843.get('tool_calls', []):
    print(tc)

with io.open(r'C:\Users\SaidNurs\Desktop\v5\zkteco\scratch\step_888.json', 'r', encoding='utf-8') as f:
    s888 = json.load(f)

for tc in s888.get('tool_calls', []):
    print(tc)
