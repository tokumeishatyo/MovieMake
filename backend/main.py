from fastapi import FastAPI, Request, HTTPException, Depends
from fastapi.middleware.cors import CORSMiddleware
import os
import sys

app = FastAPI()

# CORS settings - allow localhost for WinUI
app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"], # Strictly should be restricted, but * is fine for local tool
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)

# In-memory storage for API key
_api_key: str | None = None

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
    data = await request.json()
    api_key = data.get("api_key")
    
    if not api_key:
        raise HTTPException(status_code=400, detail="API key is missing")
    
    _api_key = api_key
    print("API Key set successfully (in-memory).")
    return {"status": "success"}

@app.get("/")
async def root():
    return {"message": "MovieMake Backend is running"}

if __name__ == "__main__":
    import uvicorn
    # Allow port to be set via environment variable or default to 8000
    port = int(os.environ.get("PORT", 8000))
    uvicorn.run(app, host="127.0.0.1", port=port)
