# -*- coding: utf-8 -*-
import io, json

transcript_path = r'C:\Users\SaidNurs\.gemini\antigravity\brain\4e8a3bc1-e966-42d6-8ef3-e1fd07bda7e8\.system_generated\logs\transcript_full.jsonl'
with io.open(transcript_path, 'r', encoding='utf-8') as f:
    for line in f:
        obj = json.loads(line)
        idx = obj.get('step_index')
        if idx in [843, 888]:
            print('Dumping step', idx)
            with io.open(r'C:\Users\SaidNurs\Desktop\v5\zkteco\scratch\step_%d.json' % idx, 'w', encoding='utf-8') as out:
                out.write(json.dumps(obj, indent=2, ensure_ascii=False))
