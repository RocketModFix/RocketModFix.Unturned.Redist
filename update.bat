@echo off
setlocal enabledelayedexpansion

rem Prompt the user for the target directory containing replacement files
set /p "sourceDirectory=Enter the path to the directory Unturned Managed directory to update files: "

rem Set the destination directory where the files will be replaced
set "destinationDirectory=.\redist"

rem Ensure the destination directory exists
if not exist "%destinationDirectory%" (
    echo Destination directory does not exist.
    exit /b 1
)

rem Loop through all files in the source directory
for %%F in ("%sourceDirectory%\*.*") do (
    rem Check if the file exists in the destination directory
    if exist "%destinationDirectory%\%%~nxF" (
        rem Replace the file in the destination directory
        copy /y "%%F" "%destinationDirectory%"
        echo %%~nxF replaced.
    ) else (
        echo %%~nxF does not exist in the destination directory.
    )
)

echo Replacement completed.
exit /b 0