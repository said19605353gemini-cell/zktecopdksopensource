# -*- coding: utf-8 -*-
import io, json

transcript_path = r'C:\Users\SaidNurs\.gemini\antigravity\brain\4e8a3bc1-e966-42d6-8ef3-e1fd07bda7e8\.system_generated\logs\transcript_full.jsonl'
with io.open(transcript_path, 'r', encoding='utf-8') as f:
    for line in f:
        if 'LocalizationService.cs' in line:
            obj = json.loads(line)
            idx = obj.get('step_index')
            # Look at all string fields in obj
            for k, v in obj.items():
                if isinstance(v, unicode) and '["ku"]' in v:
                    print('Key %s in step %d has ["ku"]! len %d' % (k, idx, len(v)))
                    with io.open(r'C:\Users\SaidNurs\Desktop\v5\zkteco\scratch\found_%s_%d.txt' % (k, idx), 'w', encoding='utf-8') as out:
                        out.write(v)
