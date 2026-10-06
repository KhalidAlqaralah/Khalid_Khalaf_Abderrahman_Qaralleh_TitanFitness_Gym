@echo off
REM ============================================================================
REM  Titan Fitness - runs every automated check, start to finish.
REM  Run from the repository folder:   run-all-tests.cmd
REM  WARNING: drops and recreates the TitanFitness database (demo data only).
REM ============================================================================
setlocal
cd /d "%~dp0"
if /i "%~1"=="stop" (call :stop & echo Stopped the API and the frontend. & exit /b 0)
set "PATH=%PATH%;%USERPROFILE%\.dotnet\tools"
set "RESULT_BUILD=FAIL"
set "RESULT_WEB=FAIL"
set "RESULT_UI=FAIL"
set "RESULT_API=FAIL"

echo.
echo === 1/8  Tools ===
where dotnet >nul 2>&1 || (echo .NET SDK not found. Install .NET 10 SDK. & goto :end)
where node >nul 2>&1 || (echo Node.js not found. Install Node 24. & goto :end)
dotnet ef --version >nul 2>&1 || dotnet tool install --global dotnet-ef
call :stop

echo.
echo === 2/8  Build the backend (must say 0 Warning, 0 Error) ===
dotnet build TitanFitness.slnx
if errorlevel 1 (echo BACKEND BUILD FAILED & goto :end)
set "RESULT_BUILD=OK"

echo.
echo === 3/8  Install and build the frontend ===
pushd titan-fitness-web
call npm install --no-audit --no-fund
call npx ng build
if not errorlevel 1 set "RESULT_WEB=OK"
popd

echo.
echo === 4/8  Install the test runner (Playwright + Chromium, first time only) ===
pushd tests
call npm install --no-audit --no-fund
call npx playwright install chromium
popd

echo.
echo === 5/8  Fresh database + start API and frontend ===
call :freshapi || goto :end
start "TF-WEB" /min cmd /c "cd /d "%~dp0titan-fitness-web" && npm start"
echo Waiting for the frontend on http://localhost:4200 ...
call :wait http://localhost:4200 90 || (echo Frontend did not start. & goto :end)

echo.
echo === 6/8  UI tests (Angular + API end to end) ===
pushd tests
node e2e.mjs
if not errorlevel 1 set "RESULT_UI=OK"
popd

echo.
echo === 7/8  API tests (on a second fresh database) ===
call :stopapi
call :freshapi || goto :end
pushd tests
node api-test.mjs
if not errorlevel 1 set "RESULT_API=OK"
popd

echo.
echo === 8/8  Leave a fresh database for manual testing ===
call :stopapi
call :freshapi

:end
echo.
echo ================== SUMMARY ==================
echo  Backend build ............ %RESULT_BUILD%
echo  Frontend build ........... %RESULT_WEB%
echo  UI tests ................. %RESULT_UI%
echo  API tests ................ %RESULT_API%
echo =============================================
echo  The API (window TF-API) and the frontend (window TF-WEB) are still running:
echo    http://localhost:4200        http://localhost:5162/swagger
echo  To stop them:  run-all-tests.cmd stop     (or close the two windows)
echo  Failure screenshots, if any: tests\screenshots
endlocal
exit /b

REM ---------------------------------------------------------------------------
:freshapi
echo Dropping the TitanFitness database ...
dotnet ef database drop --force --project TitanFitness.Infrastructure --startup-project TitanFitness.Api
start "TF-API" /min cmd /c "cd /d "%~dp0" && dotnet run --no-build --project TitanFitness.Api --launch-profile http"
echo Waiting for the API on http://localhost:5162 (creates and seeds the database) ...
call :wait http://localhost:5162/api/auth/me 120 || (echo API did not start - is SQL Server running? Check the TF-API window. & exit /b 1)
exit /b 0

:wait
REM %1 = url, %2 = seconds
set /a "_n=0"
:waitloop
curl -s -o nul %1 && exit /b 0
set /a "_n+=2"
if %_n% geq %2 exit /b 1
timeout /t 2 /nobreak >nul
goto waitloop

:stopapi
taskkill /F /IM TitanFitness.Api.exe >nul 2>&1
taskkill /F /FI "WINDOWTITLE eq TF-API*" /T >nul 2>&1
timeout /t 2 /nobreak >nul
exit /b 0

:stop
call :stopapi
taskkill /F /FI "WINDOWTITLE eq TF-WEB*" /T >nul 2>&1
exit /b 0
