@echo off
chcp 65001 >nul
echo ========================================
echo   Modern TRCC - 发布（框架依赖）
echo ========================================
echo.

where dotnet >nul 2>&1
if %ERRORLEVEL% NEQ 0 (
    echo [错误] 未检测到 .NET SDK
    pause
    exit /b 1
)

set OUTPUT_DIR=%~dp0publish

echo 正在发布（单文件模式）...
dotnet publish "%~dp0src\ModernTRCC.App\ModernTRCC.App.csproj" ^
    -c Release ^
    -r win-x64 ^
    --self-contained false ^
    -o "%OUTPUT_DIR%" ^
    /p:PublishSingleFile=true ^
    /p:IncludeNativeLibrariesForSelfExtract=true ^
    /p:DebugType=None ^
    /p:DebugSymbols=false

if %ERRORLEVEL% NEQ 0 (
    echo [错误] 发布失败
    pause
    exit /b 1
)

echo.
echo ========================================
echo   发布成功！
echo   输出目录: %OUTPUT_DIR%
echo   运行时: 需要 .NET 8.0 Desktop Runtime
echo   所有 DLL 已整合进 ModernTRCC.exe
echo ========================================
echo.
echo 注意：请将 ffmpeg.exe 和 ffprobe.exe 复制到 ffmpeg 子目录
echo       下载地址: https://www.gyan.dev/ffmpeg/builds/
echo.
pause
