import os
from typing import List, Dict, Optional, Any

class AssetManager:
    def __init__(self, assets_dir: str = "assets", user_data_dir: Optional[str] = None):
        self.character_paths: Dict[str, str] = {} # Map ID to full path

        # 1. Internal Assets
        if os.path.exists("backend/assets"):
            self.internal_assets_dir = os.path.abspath("backend/assets")
        elif os.path.exists("assets"):
            self.internal_assets_dir = os.path.abspath("assets")
        else:
             self.internal_assets_dir = os.path.abspath("assets")
             os.makedirs(self.internal_assets_dir, exist_ok=True)
            
        self.internal_chars_dir = os.path.join(self.internal_assets_dir, "characters")
        os.makedirs(self.internal_chars_dir, exist_ok=True)

        # 2. User Data Assets
        self.user_chars_dir = None
        if user_data_dir:
            self.user_chars_dir = os.path.join(user_data_dir, "characters")
            os.makedirs(self.user_chars_dir, exist_ok=True)
            print(f"AssetManager: User characters dir set to {self.user_chars_dir}")

    def get_characters(self) -> List[Dict[str, str]]:
        """
        Scans both internal and user characters directories.
        """
        chars_map = {} # Use dict to dedupe by ID
        
        # Scan Internal
        self._scan_dir(self.internal_chars_dir, chars_map, "internal")
        
        # Scan User
        if self.user_chars_dir:
            self._scan_dir(self.user_chars_dir, chars_map, "user")
            
        self.character_paths = {k: v["path"] for k, v in chars_map.items()}
        return list(chars_map.values())

    def _scan_dir(self, directory: str, result_map: Dict[str, Any], source_type: str):
        if not os.path.exists(directory):
            return

        print(f"AssetManager: Scanning {directory}")
        for item in os.listdir(directory):
            path = os.path.join(directory, item)
            if os.path.isdir(path):
                # ID is folder name
                char_id = item
                if char_id not in result_map:
                    result_map[char_id] = {
                        "id": char_id,
                        "name": item.capitalize(),
                        "path": path,
                        "source": source_type
                    }

    def get_character_images(self, char_id: str) -> List[str]:
        """Returns list of image filenames for a character."""
        # Resolve path from cached map or check both
        char_path = self.character_paths.get(char_id)
        
        if not char_path:
            # Fallback check
            p1 = os.path.join(self.internal_chars_dir, char_id)
            if os.path.exists(p1):
                 char_path = p1
            elif self.user_chars_dir:
                p2 = os.path.join(self.user_chars_dir, char_id)
                if os.path.exists(p2):
                    char_path = p2
        
        if not char_path or not os.path.exists(char_path):
            return []
        
        images = []
        valid_exts = {".png", ".jpg", ".jpeg", ".webp"}
        for f in os.listdir(char_path):
            ext = os.path.splitext(f)[1].lower()
            if ext in valid_exts:
                images.append(f)
        return images
