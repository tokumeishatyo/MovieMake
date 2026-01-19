from fastapi import FastAPI, Request, HTTPException
from fastapi.middleware.cors import CORSMiddleware
from typing import Optional
import os
import sys
import traceback

app = FastAPI()

# CORS settings
app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)

# In-memory storage for API key
# using Optional for compatibility with python < 3.10
_api_key: Optional[str] = None

from fastapi.staticfiles import StaticFiles
from pydantic import BaseModel
from services.asset_manager import AssetManager
from services.tts_service import TTSService

from services.video_generator import VideoGenerator

# Initialize Services
asset_manager = AssetManager()
tts_service = TTSService()
video_generator = VideoGenerator(tts_service, asset_manager)

# Mount static files
app.mount("/static/assets", StaticFiles(directory=asset_manager.assets_dir), name="assets")
app.mount("/static/audio", StaticFiles(directory=tts_service.output_dir), name="audio")
app.mount("/static/output", StaticFiles(directory=video_generator.output_dir), name="output")

# ... existing health/api-key ...

# Video API
@app.post("/video/render")
async def render_video(request: Request):
    try:
        script = await request.json()
        print("Starting video render...")
        output_path = video_generator.generate_video(script)
        
        filename = os.path.basename(output_path)
        url = f"/static/output/{filename}"
        print(f"Video rendered to: {output_path}")
        return {"url": url, "path": output_path}
    except Exception as e:
        print(f"Render Error: {e}")
        traceback.print_exc()
        raise HTTPException(status_code=500, detail=str(e))

@app.get("/health")
async def health_check():
    """Health check endpoint for WinUI to verify connection."""
    return {"status": "ok", "api_key_set": _api_key is not None}

@app.post("/config/api-key")
async def set_api_key(request: Request):
    """
    Receive API key securely via POST body.
    Store in memory only.
    """
    global _api_key
    try:
        data = await request.json()
        print(f"DEBUG: Received config data: {data}")
        
        api_key = data.get("api_key")
        
        if not api_key:
            raise HTTPException(status_code=400, detail="API key is missing")
        
        _api_key = api_key
        print("API Key set successfully (in-memory).")
        return {"status": "success"}
    except Exception as e:
        print(f"ERROR in set_api_key: {e}")
        traceback.print_exc()
        raise HTTPException(status_code=500, detail=str(e))

# Asset APIs
@app.get("/assets/characters")
async def get_characters():
    return asset_manager.get_characters()

@app.get("/assets/characters/{char_id}/images")
async def get_character_images(char_id: str):
    return asset_manager.get_character_images(char_id)

# TTS API
class TTSRequest(BaseModel):
    text: str
    lang: str = "ja"

@app.post("/tts/generate")
async def generate_tts(req: TTSRequest):
    try:
        path = tts_service.generate_audio_file(req.text, req.lang)
        # Return URL relative to static mount
        filename = os.path.basename(path)
        url = f"/static/audio/{filename}"
        return {"url": url, "path": path}
    except Exception as e:
        raise HTTPException(status_code=500, detail=str(e))

@app.get("/")
async def root():
    return {"message": "MovieMake Backend is running"}

if __name__ == "__main__":
    import uvicorn
    port = int(os.environ.get("PORT", 8000))
    # log_level="debug" to see more details in console
    uvicorn.run(app, host="127.0.0.1", port=port, log_level="debug")
