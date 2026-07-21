# -*- coding: utf-8 -*-

import sys
import whisper


sys.stdout.reconfigure(encoding='utf-8')

model = whisper.load_model("small", device="cuda")
# model = whisper.load_model("small")


result = model.transcribe(sys.argv[1])

print(result["text"])
