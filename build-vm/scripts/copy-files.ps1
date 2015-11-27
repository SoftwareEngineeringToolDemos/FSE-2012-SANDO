#Desktop Location
$desktop = [Environment]::GetFolderPath("Desktop")

# Copy Files
Copy-Item -path c:\vagrant\Files\* -Destination $desktop -Recurse

