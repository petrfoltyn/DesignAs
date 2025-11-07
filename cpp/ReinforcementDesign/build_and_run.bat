@echo off
echo ======================================================
echo Building ReinforcementDesign with Analytical Integration
echo ======================================================
echo.

REM Try to find Visual Studio
set "VSWHERE=%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere.exe"

if not exist "%VSWHERE%" (
    echo ERROR: Visual Studio not found
    echo Please build manually in Visual Studio
    pause
    exit /b 1
)

REM Get VS installation path
for /f "usebackq tokens=*" %%i in (`"%VSWHERE%" -latest -products * -requires Microsoft.Component.MSBuild -property installationPath`) do (
    set "VSINSTALLDIR=%%i"
)

if not defined VSINSTALLDIR (
    echo ERROR: Could not find Visual Studio installation
    pause
    exit /b 1
)

echo Found Visual Studio at: %VSINSTALLDIR%
echo.

REM Setup build environment
call "%VSINSTALLDIR%\VC\Auxiliary\Build\vcvars64.bat"

echo.
echo Building Release configuration...
echo.

REM Build the solution
msbuild ReinforcementDesign.sln /p:Configuration=Release /p:Platform=x64 /t:rebuild /v:minimal

if %ERRORLEVEL% NEQ 0 (
    echo.
    echo ERROR: Build failed
    pause
    exit /b 1
)

echo.
echo ======================================================
echo Build successful! Running benchmark...
echo ======================================================
echo.

REM Run the executable
x64\Release\ReinforcementDesign.exe

pause
