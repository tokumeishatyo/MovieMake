import os
import uuid
from typing import List, Dict, Any
from moviepy.editor import ImageClip, AudioFileClip, concatenate_videoclips, CompositeVideoClip, TextClip
from services.tts_service import TTSService
from services.asset_manager import AssetManager

class VideoGenerator:
    def __init__(self, tts_service: TTSService, asset_manager: AssetManager, output_dir: str = "output"):
        self.tts_service = tts_service
        self.asset_manager = asset_manager
        
        if os.path.exists("backend"):
            self.output_dir = os.path.abspath(os.path.join("backend", output_dir))
        else:
             self.output_dir = os.path.abspath(output_dir)
        os.makedirs(self.output_dir, exist_ok=True)

    def generate_video(self, script: Dict[str, Any]) -> str:
        """
        Generates a video from the script.
        Returns the absolute path to the generated MP4 file.
        """
        clips = []
        lines = script.get("lines", [])
        characters = {c["id"]: c for c in script.get("characters", [])}
        
        # Default fallback image if char not found or has no images
        # For now, we just skip image or use a color block? 
        # Better: AssetManager should provide a default image path for a character.
        
        for line in lines:
            text = line.get("text", "")
            char_id = line.get("characterId", "")
            
            if not text:
                continue

            # 1. Generate Audio
            audio_path = self.tts_service.generate_audio_file(text)
            audio_clip = AudioFileClip(audio_path)
            
            # 2. Get Character Image
            # Simplification: Use the first image found for the character
            image_path = None
            if char_id in characters:
                # We need to resolve the full path. AssetManager gives us relative or absolute?
                # AssetManager currently returns list of images via get_character_images(id)
                # We need the full path to that image.
                imgs = self.asset_manager.get_character_images(char_id)
                if imgs:
                    # Construct full path
                    # This logic relies on AssetManager internal structure, maybe add a helper in AssetManager?
                    # But for now:
                   image_path = os.path.join(self.asset_manager.characters_dir, char_id, imgs[0])
            
            if image_path and os.path.exists(image_path):
                video_clip = ImageClip(image_path).set_duration(audio_clip.duration)
            else:
                # Fallback: Black screen or Text only
                # For MVP let's assume valid image or crash/empty
                # Let's make a simple ColorClip equivalent or just an empty ImageClip if possible? 
                # MoviePy needs a visual. Let's use TextClip if no image.
                # Note: TextClip requires ImageMagick. To avoid deps, let's use a default placeholder if possible.
                # Or just skip visual? No, audio needs video track.
                # Let's hope for an image. If not, maybe just 100x100 black.
                from moviepy.editor import ColorClip
                video_clip = ColorClip(size=(1280, 720), color=(0,0,0), duration=audio_clip.duration)

            video_clip = video_clip.set_audio(audio_clip)
            video_clip.fps = 24
            clips.append(video_clip)

        if not clips:
            raise ValueError("No lines to generate video from.")

        final_clip = concatenate_videoclips(clips, method="compose")
        
        output_filename = f"{uuid.uuid4()}.mp4"
        output_path = os.path.join(self.output_dir, output_filename)
        
        final_clip.write_videofile(output_path, codec="libx264", audio_codec="aac", fps=24)
        
        return output_path
