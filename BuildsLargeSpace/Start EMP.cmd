@echo off
setlocal
pushd "%~dp0" || exit /b 1
if not exist "PBRLargeSpace.exe" (
    echo PBRLargeSpace.exe is missing. Copy the entire BuildsLargeSpace folder.
    pause
    popd
    exit /b 1
)
start "" "%~dp0PBRLargeSpace.exe" -pbr-large-space emp -screen-fullscreen 1
popd
