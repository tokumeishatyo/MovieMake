@echo off
echo Installing dependencies...
pip install -r backend/requirements.txt
if %errorlevel% neq 0 (
    echo Failed to install dependencies. Please ensure python and pip are in your PATH.
    pause
    exit /b
)

echo.
echo Starting Backend Server manually...
echo You should see "Uvicorn running on http://127.0.0.1:8000"
echo.
cd backend
python main.py
pause
