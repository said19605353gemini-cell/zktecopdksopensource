# -*- coding: utf-8 -*-
import io, json

with io.open(r'C:\Users\SaidNurs\Desktop\v5\zkteco\scratch\step_843.json', 'r', encoding='utf-8') as f:
    s843 = json.load(f)

for tc in s843.get('tool_calls', []):
    print('843 tc function:', tc.get('function', {}).get('name'))
    args = tc.get('function', {}).get('arguments', {})
    for k, v in args.items():
        print('  arg %s len: %d' % (k, len(v) if isinstance(v, unicode) else 0))
        if 'LocalizationService' in v:
            print('    arg %s contains LocalizationService' % k)
            with io.open(r'C:\Users\SaidNurs\Desktop\v5\zkteco\scratch\extracted_843_%s.txt' % k, 'w', encoding='utf-8') as out:
                out.write(v)

with io.open(r'C:\Users\SaidNurs\Desktop\v5\zkteco\scratch\step_888.json', 'r', encoding='utf-8') as f:
    s888 = json.load(f)

for tc in s888.get('tool_calls', []):
    print('888 tc function:', tc.get('function', {}).get('name'))
    args = tc.get('function', {}).get('arguments', {})
    for k, v in args.items():
        print('  arg %s len: %d' % (k, len(v) if isinstance(v, unicode) else 0))
        if 'HelpTitle' in v:
            print('    arg %s contains HelpTitle' % k)
            with io.open(r'C:\Users\SaidNurs\Desktop\v5\zkteco\scratch\extracted_888_%s.txt' % k, 'w', encoding='utf-8') as out:
                out.write(v)
