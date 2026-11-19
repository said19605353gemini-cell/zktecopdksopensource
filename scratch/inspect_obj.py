# -*- coding: utf-8 -*-
import io, json

transcript_path = r'C:\Users\SaidNurs\.gemini\antigravity\brain\4e8a3bc1-e966-42d6-8ef3-e1fd07bda7e8\.system_generated\logs\transcript_full.jsonl'
with io.open(transcript_path, 'r', encoding='utf-8') as f:
    for line in f:
        if 'LocalizationService' in line:
            obj = json.loads(line)
            idx = obj.get('step_index')
            print('Line matches LocalizationService, step_index:', idx, 'keys:', obj.keys())
            if 'tool_calls' in obj:
                print('  tool_calls:', len(obj['tool_calls']))
                for tc in obj['tool_calls']:
                    print('    tc:', tc.keys())
            break
