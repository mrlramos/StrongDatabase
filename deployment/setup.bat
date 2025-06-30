@echo off
setlocal enabledelayedexpansion

echo 🚀 StrongDatabase - Simplified Setup
echo ====================================
echo.

REM Check Docker
echo 🔍 Checking dependencies...
docker --version >nul 2>&1
if errorlevel 1 (
    echo ❌ Docker not found. Please install Docker first.
    pause
    exit /b 1
)

docker-compose --version >nul 2>&1
if errorlevel 1 (
    echo ❌ Docker Compose not found. Please install Docker Compose first.
    pause
    exit /b 1
)

echo ✅ Dependencies OK!
echo.

REM Clean up previous runs
echo 🧹 Cleaning up previous runs...
cd ..
docker-compose down -v --remove-orphans >nul 2>&1
docker system prune -f >nul 2>&1
echo ✅ Cleanup complete!
echo.

REM Start the simplified stack
echo 🚀 Starting StrongDatabase (simplified)...
docker-compose up -d

echo.
echo ⏳ Waiting for services to start...
timeout /t 30 /nobreak >nul

REM Check service health
echo 🔍 Checking service health...

REM Check Primary
set /p temp="Primary DB: " <nul
docker exec primary-db pg_isready -U postgres -d strongdatabase >nul 2>&1
if !errorlevel! equ 0 (
    echo ✅ Healthy
) else (
    echo ❌ Unhealthy
)

REM Check API
set /p temp="API: " <nul
curl -s http://localhost:5000/health >nul 2>&1
if !errorlevel! equ 0 (
    echo ✅ Healthy
) else (
    echo ❌ Unhealthy
)

REM Check Elasticsearch
set /p temp="Elasticsearch: " <nul
curl -s http://localhost:9200 >nul 2>&1
if !errorlevel! equ 0 (
    echo ✅ Healthy
) else (
    echo ❌ Unhealthy
)

echo.
echo 🎉 Setup Complete!
echo ==================
echo.
echo 📋 Available URLs:
echo    🌐 API: http://localhost:5000
echo    📖 Swagger: http://localhost:5000/swagger
echo    🏥 Health: http://localhost:5000/health
echo    📊 Kibana: http://localhost:5601
echo    🔍 Elasticsearch: http://localhost:9200
echo.
echo 📝 Quick Tests:
echo    curl http://localhost:5000/api/customer
echo    curl http://localhost:5000/health
echo.
echo 📦 View containers: docker-compose ps
echo 📜 View logs: docker-compose logs -f

pause 