#Desktop Location
$desktop = [Environment]::GetFolderPath("Desktop")

# Copy Files
Copy-Item -path c:\vagrant\files\* -Destination $desktop -Recurse

