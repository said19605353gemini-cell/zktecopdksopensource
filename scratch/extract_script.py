# -*- coding: utf-8 -*-
import io, json

with io.open(r'C:\Users\SaidNurs\Desktop\v5\zkteco\scratch\step_843.json', 'r', encoding='utf-8') as f:
    s843 = json.load(f)

for tc in s843.get('tool_calls', []):
    if tc.get('name') == 'write_file':
        args = tc.get('args', {})
        content = args.get('Content', '')
        print('Extracting scratch_localize.js, len %d' % len(content))
        with io.open(r'C:\Users\SaidNurs\Desktop\v5\zkteco\scratch\scratch_localize.js', 'w', encoding='utf-8') as out:
            out.write(content)
        break
