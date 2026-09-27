@echo off
rem Reconstroi as cenas a partir da arte atual e gera Build\Windows\LITHOSTRIDE.exe.
rem Requisitos: Unity Hub aberto e logado (licenca) e o projeto FECHADO no editor.

set UNITY="C:\Program Files\Unity\Hub\Editor\6000.6.2f1\Editor\Unity.exe"
set LOG="%~dp0Logs\build_windows.log"

if not exist %UNITY% (
    echo Unity 6000.6.2f1 nao encontrado em %UNITY%
    pause
    exit /b 1
)

echo Gerando executavel... isso leva alguns minutos.
%UNITY% -batchmode -projectPath "%~dp0." -executeMethod Lithostride.EditorTools.WindowsBuild.RebuildAndBuild -logFile %LOG%

if errorlevel 1 (
    echo Build falhou. Veja %LOG%
) else (
    echo Build concluida: Build\Windows\LITHOSTRIDE.exe
)
pause
