# -*- coding: utf-8 -*-
import sys
import asyncio
import edge_tts

text = sys.argv[1]
output_path = sys.argv[2]
# voice = "zh-TW-YunJheNeural"  # 男聲
voice = "zh-TW-HsiaoYuNeural"  # 女聲
rate = "+35%"  # 女聲原始語速較慢，上調 35%

async def tts():
    communicate = edge_tts.Communicate(
        text=text,
        voice=voice,
        rate=rate,
    )
    await communicate.save(output_path)

asyncio.run(tts())
print("TTS finished")
