import os
from gtts import gTTS
import uuid

class TTSService:
    def __init__(self, output_dir: str = "temp_audio"):
        # Ensure temp directory exists
        if os.path.exists("backend"):
            self.output_dir = os.path.abspath(os.path.join("backend", output_dir))
        else:
            self.output_dir = os.path.abspath(output_dir)
            
        os.makedirs(self.output_dir, exist_ok=True)

    def generate_audio_file(self, text: str, lang: str = 'ja') -> str:
        """
        Generates MP3 audio from text using Google TTS.
        Returns the absolute path to the generated file.
        """
        if not text:
            raise ValueError("Text cannot be empty")

        try:
            tts = gTTS(text=text, lang=lang)
            filename = f"{uuid.uuid4()}.mp3"
            filepath = os.path.join(self.output_dir, filename)
            tts.save(filepath)
            return filepath
        except Exception as e:
            print(f"TTS Generation Error: {e}")
            raise

    def cleanup_temp_files(self):
        """Removes all files in temp directory."""
        if not os.path.exists(self.output_dir):
            return
        for f in os.listdir(self.output_dir):
            try:
                os.remove(os.path.join(self.output_dir, f))
            except Exception:
                pass
