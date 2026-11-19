# -*- coding: utf-8 -*-
import io, glob, os

brains = glob.glob(r'C:\Users\SaidNurs\.gemini\antigravity\brain\*\.system_generated\logs\transcript_full.jsonl')
for b in brains:
    print('Checking:', b)
    with io.open(b, 'r', encoding='utf-8') as f:
        for idx, line in enumerate(f):
            if 'LocalizationService.cs' in line and '["AppTitle"]' in line:
                print('  Found match in %s line %d' % (b, idx))
                # write out line
                with io.open(r'C:\Users\SaidNurs\Desktop\v5\zkteco\scratch\matched_line.txt', 'w', encoding='utf-8') as out:
                    out.write(line)
                break
