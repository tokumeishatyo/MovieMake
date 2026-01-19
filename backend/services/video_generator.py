import os
import uuid
import random
import numpy as np
from typing import List, Dict, Any
from moviepy.editor import ImageClip, AudioFileClip, concatenate_videoclips, CompositeVideoClip, VideoClip, ColorClip
from services.tts_service import TTSService
from services.asset_manager import AssetManager

# Fix for moviepy 1.0.3 using removed PIL.Image.ANTIALIAS
import PIL.Image
if not hasattr(PIL.Image, 'ANTIALIAS'):
    PIL.Image.ANTIALIAS = PIL.Image.LANCZOS

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
        clips = []
        lines = script.get("lines", [])
        
        for line in lines:
            text = line.get("text", "")
            char_id = line.get("characterId", "")
            
            if not text:
                continue

            # 1. Generate Audio
            audio_path = self.tts_service.generate_audio_file(text)
            audio_clip = AudioFileClip(audio_path)
            duration = audio_clip.duration

            # 2. Get Character Images
            char_images = {}
            if char_id:
                # Use helper to get full paths map
                char_images = self._load_character_images(char_id)
            
            if not char_images:
                # Fallback to black screen if no character
                video_clip = ColorClip(size=(1280, 720), color=(0,0,0), duration=duration)
            else:
                # 3. Create Animated Video Clip
                print(f"Generating animation for {char_id} ({duration}s)")
                
                # Analyze audio for lip-sync
                # Get volume array (resampled to 10 FPS for simpler logic or per frame?)
                # make_frame is called for every frame (e.g. 24 fps).
                # We need audio volume at time t.
                
                # Pre-calculate blink times
                blink_events = []
                t = 0
                while t < duration:
                    # Blink every 3-5 seconds
                    interval = random.uniform(3.0, 5.0)
                    t += interval
                    if t < duration:
                        blink_events.append((t, t + 0.15)) # Blink duration 0.15s

                def make_frame(t):
                    # 1. Determine Mouth State
                    # Get audio chunk around t
                    # t is seconds.
                    # Safety check
                    if t < 0: t = 0
                    if t > duration: t = duration
                    
                    # Simple volume check
                    # Get a small window of audio
                    try:
                        # audio_clip.subclip might be slow if called every frame?
                        # Better to access direct array if possible, but AudioFileClip handles it.
                        # window: 0.1s
                        chunk = audio_clip.get_frame(t) # This returns single frame amplitude? No, returns stereo frame?
                        # Actually audio_clip.get_frame(t) returns numpy array of shape (2,) for stereo
                        # We need RMS amplitude
                        vol = np.sqrt(np.mean(chunk**2))
                    except:
                        vol = 0

                    is_mouth_open = vol > 0.01 # Threshold. Tune this.

                    # 2. Determine Eye State
                    is_eye_closed = False
                    for start, end in blink_events:
                        if start <= t <= end:
                            is_eye_closed = True
                            break
                    
                    # 3. Select Image
                    # 00: Open/Closed (Normal)
                    # 01: Open/Open
                    # 02: Closed/Closed
                    # 03: Closed/Open
                    
                    img_key = "00.png"
                    if not is_eye_closed and not is_mouth_open:
                        img_key = "00.png"
                    elif not is_eye_closed and is_mouth_open:
                        img_key = "01.png"
                    elif is_eye_closed and not is_mouth_open:
                        img_key = "02.png"
                    elif is_eye_closed and is_mouth_open:
                        img_key = "03.png"
                    
                    # Fallback to 00 if key missing
                    if img_key not in char_images:
                        img_key = "00.png"
                        if img_key not in char_images:
                             # Ultimate fallback: return first available or black?
                             # Assuming at least one image exists from _load_character_images check
                             img_key = list(char_images.keys())[0]

                    return char_images[img_key]

                # Create VideoClip
                # NOTE: VideoClip(make_frame) expects make_frame to return a numpy array (H,W,3).
                # But we are selecting existing images.
                # Loading image from disk every frame is SLOW.
                # Pre-load images as numpy arrays.
                # FIX: Resize images to standard height (e.g. 720) and ensure EVEN dimensions to avoid codec issues.
                target_height = 720
                
                limit_images = {}
                first_size = None
                
                for k, v in char_images.items():
                    clip = ImageClip(v)
                    # Resize to target height
                    if clip.h != target_height:
                        clip = clip.resize(height=target_height)
                    
                    # Ensure even dimensions (required by libx264)
                    w, h = clip.size
                    if w % 2 != 0: w -= 1
                    if h % 2 != 0: h -= 1
                    
                    if (w, h) != clip.size:
                        clip = clip.resize(newsize=(w, h)) # Crucial for some moviepy versions or just crop? Resize is safer.
                        # Actually standard resize might keep aspect ratio and result in odd again if not careful.
                        # Force exact size:
                        clip = clip.resize(newsize=(w, h))

                    limit_images[k] = clip.img
                    if first_size is None:
                        first_size = (w, h)
                        
                # Optimized make_frame
                def frame_generator(t):
                    if t < 0: t = 0
                    if t >= duration: t = duration - 0.001
                    
                    # Volume
                    try:
                        chunk = audio_clip.get_frame(t)
                        vol = np.sqrt(np.mean(chunk**2))
                    except:
                        vol = 0
                    is_mouth_open = vol > 0.01

                    # Blink
                    is_eye_closed = False
                    for start, end in blink_events:
                        if start <= t <= end:
                            is_eye_closed = True
                            break
                    
                    key = "00.png"
                    if not is_eye_closed and not is_mouth_open: key = "00.png" # Explicit fix for logic
                    elif not is_eye_closed and is_mouth_open: key = "01.png"
                    elif is_eye_closed and not is_mouth_open: key = "02.png"
                    elif is_eye_closed and is_mouth_open: key = "03.png"
                    
                    if key not in limit_images: key = "00.png"
                    if key not in limit_images: key = list(limit_images.keys())[0]
                    
                    return limit_images[key]

                video_clip = VideoClip(frame_generator, duration=duration)

            video_clip = video_clip.set_audio(audio_clip)
            video_clip.fps = 24
            clips.append(video_clip)

        if not clips:
            raise ValueError("No lines to generate video from.")

        final_clip = concatenate_videoclips(clips, method="compose")
        
        output_filename = f"{uuid.uuid4()}.mp4"
        output_path = os.path.join(self.output_dir, output_filename)
        
        final_clip.write_videofile(output_path, codec="libx264", audio_codec="aac", fps=24, logger=None) # logger=None to reduce verbosity?
        
        return output_path

    def _load_character_images(self, char_id: str) -> Dict[str, str]:
        """
        Returns a dict of filename -> full_path
        e.g. {'00.png': '/path/to/00.png', ...}
        """
        # AssetManager.get_character_images returns list of filenames.
        # We need to construct full paths.
        # AssetManager has internal structure, but we used a trick to check internal/user dirs.
        # Let's use AssetManager.character_paths cache if available, or just re-resolve.
        
        # We need the ROOT path of the character to join filenames.
        # AssetManager.get_characters() populates self.asset_manager.character_paths
        
        # Ensure path is resolved
        if char_id not in self.asset_manager.character_paths:
             # Trigger a scan if missing?
             self.asset_manager.get_characters()
        
        char_root = self.asset_manager.character_paths.get(char_id)
        if not char_root:
            return {}
            
        images = {}
        for f in ["00.png", "01.png", "02.png", "03.png"]:
            full = os.path.join(char_root, f)
            if os.path.exists(full):
                images[f] = full
        
        # Also include any other images just in case fallback is needed
        # Or just stick to the 4 spec.
        if not images:
             # Try to find ANY png
             for f in os.listdir(char_root):
                 if f.endswith(".png"):
                     images[f] = os.path.join(char_root, f)
                     # Treat first one as 00.png default if 00 missing
                     if "00.png" not in images:
                         images["00.png"] = os.path.join(char_root, f)
        
        return images
