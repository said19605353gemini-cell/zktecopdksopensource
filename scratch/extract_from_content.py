# -*- coding: utf-8 -*-
import io, json

transcript_path = r'C:\Users\SaidNurs\.gemini\antigravity\brain\4e8a3bc1-e966-42d6-8ef3-e1fd07bda7e8\.system_generated\logs\transcript_full.jsonl'
with io.open(transcript_path, 'r', encoding='utf-8') as f:
    for line in f:
        if 'LocalizationService' in line:
            obj = json.loads(line)
            idx = obj.get('step_index')
            content = obj.get('content', '')
            if '["ku"]' in content and '["en"]' in content:
                print('Found in step %d, content len %d' % (idx, len(content)))
                with io.open(r'C:\Users\SaidNurs\Desktop\v5\zkteco\scratch\transcript_step_%d.txt' % idx, 'w', encoding='utf-8') as out:
                    out.write(content)
