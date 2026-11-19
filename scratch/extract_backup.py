# -*- coding: utf-8 -*-
import io, json

transcript_path = r'C:\Users\SaidNurs\.gemini\antigravity\brain\4e8a3bc1-e966-42d6-8ef3-e1fd07bda7e8\.system_generated\logs\transcript_full.jsonl'
with io.open(transcript_path, 'r', encoding='utf-8') as f:
    for line in f:
        obj = json.loads(line)
        idx = obj.get('step_index')
        if idx in [151, 843, 888]:
            tool_calls = obj.get('tool_calls', [])
            for tc in tool_calls:
                fn = tc.get('function', {})
                args = fn.get('arguments', {})
                content = args.get('Content', '')
                if 'Translations' in content:
                    print('Step %d has full LocalizationService! Writing to backup_%d.cs' % (idx, idx))
                    with io.open(r'C:\Users\SaidNurs\Desktop\v5\zkteco\scratch\backup_%d.cs' % idx, 'w', encoding='utf-8') as out:
                        out.write(content)
