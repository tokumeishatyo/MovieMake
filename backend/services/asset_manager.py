import os
from typing import List, Dict, Optional

class AssetManager:
    def __init__(self, assets_dir: str = "assets"):
        # Resolve absolute path relative to backend root or current working dir
        # Assuming run from backend dir or root, we try to be robust
        if os.path.exists("backend/assets"):
            self.assets_dir = os.path.abspath("backend/assets")
        elif os.path.exists("assets"):
            self.assets_dir = os.path.abspath("assets")
        else:
            self.assets_dir = os.path.abspath("assets")
            os.makedirs(self.assets_dir, exist_ok=True)
            
        self.characters_dir = os.path.join(self.assets_dir, "characters")
        os.makedirs(self.characters_dir, exist_ok=True)

    def get_characters(self) -> List[Dict[str, str]]:
        """
        Scans the characters directory.
        Each subdirectory is considered a character.
        Returns a list of character info.
        """
        chars = []
        if not os.path.exists(self.characters_dir):
            print(f"AssetManager: Characters dir not found at {self.characters_dir}")
            return chars

        print(f"AssetManager: Scanning {self.characters_dir}")
        for item in os.listdir(self.characters_dir):
            path = os.path.join(self.characters_dir, item)
            if os.path.isdir(path):
                # Check for a preview image or just use folder name
                # For now simple ID and Name based on folder
                chars.append({
                    "id": item,
                    "name": item.capitalize(),
                    "path": path
                })
        return chars

    def get_character_images(self, char_id: str) -> List[str]:
        """Returns list of image filenames for a character."""
        char_path = os.path.join(self.characters_dir, char_id)
        if not os.path.exists(char_path):
            return []
        
        images = []
        valid_exts = {".png", ".jpg", ".jpeg", ".webp"}
        for f in os.listdir(char_path):
            ext = os.path.splitext(f)[1].lower()
            if ext in valid_exts:
                images.append(f)
        return images
