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

@app.get("/")
async def root():
    return {"message": "MovieMake Backend is running"}

if __name__ == "__main__":
    import uvicorn
    port = int(os.environ.get("PORT", 8000))
    # log_level="debug" to see more details in console
    uvicorn.run(app, host="127.0.0.1", port=port, log_level="debug")
