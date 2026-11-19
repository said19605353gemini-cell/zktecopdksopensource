# -*- coding: utf-8 -*-
import io, json

transcript_path = r'C:\Users\SaidNurs\.gemini\antigravity\brain\4e8a3bc1-e966-42d6-8ef3-e1fd07bda7e8\.system_generated\logs\transcript_full.jsonl'
with io.open(transcript_path, 'r', encoding='utf-8') as f:
    for line in f:
        if 'LocalizationService.cs' in line and 'Dictionary<string, string>' in line:
            obj = json.loads(line)
            # check tool calls or content
            print('Found step:', obj.get('step_index'), obj.get('type'))
