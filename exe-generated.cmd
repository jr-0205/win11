@echo off
setlocal
cd /d "%~dp0"
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0exe-generated.ps1" %*
set EXITCODE=%ERRORLEVEL%
echo.
if not "%EXITCODE%"=="0" (
  echo El generador termino con error %EXITCODE%.
) else (
  echo Proceso terminado.
)
pause
exit /b %EXITCODE%
