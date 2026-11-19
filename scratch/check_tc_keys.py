# -*- coding: utf-8 -*-
import io, json

with io.open(r'C:\Users\SaidNurs\Desktop\v5\zkteco\scratch\step_843.json', 'r', encoding='utf-8') as f:
    s843 = json.load(f)

print('843 keys:', s843.keys())
print('tool_calls type:', type(s843.get('tool_calls')))
if s843.get('tool_calls'):
    print('tool_calls[0]:', s843['tool_calls'][0].keys())
    print('name:', s843['tool_calls'][0].get('name'))
    args = s843['tool_calls'][0].get('arguments')
    print('args keys:', args.keys() if isinstance(args, dict) else type(args))
