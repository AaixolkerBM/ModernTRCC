@echo off
chcp 65001 >nul
echo ========================================
echo   Modern TRCC - 构建脚本
echo ========================================
echo.

where dotnet >nul 2>&1
if %ERRORLEVEL% NEQ 0 (
    echo [错误] 未检测到 .NET SDK
    echo 请从 https://dotnet.microsoft.com/download 安装 .NET 8.0 SDK
    pause
    exit /b 1
)

echo [1/2] 还原 NuGet 包...
dotnet restore "%~dp0ModernTRCC.sln"
if %ERRORLEVEL% NEQ 0 (
    echo [错误] 包还原失败
    pause
    exit /b 1
)
echo.

echo [2/2] 构建项目 (Release)...
dotnet build "%~dp0ModernTRCC.sln" -c Release --no-restore
if %ERRORLEVEL% NEQ 0 (
    echo [错误] 构建失败
    pause
    exit /b 1
)

echo.
echo ========================================
echo   构建成功！
echo   输出: src\ModernTRCC.App\bin\Release\net8.0-windows\
echo ========================================
echo.
pause
