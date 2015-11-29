
start /WAIT C:\vagrant\scripts\vs_premium.exe /s

xcopy "C:\vagrant\Files\Sando\unzip.exe" "%localappdata%\microsoft\VisualStudio\11.0\Extensions"
xcopy "C:\vagrant\Files\Sando\extensions.zip" "%localappdata%\microsoft\VisualStudio\11.0\Extensions"

cd %localappdata%\microsoft\VisualStudio\11.0\Extensions
unzip.exe extensions.zip