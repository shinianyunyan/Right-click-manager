@echo off    
dotnet publish .\RightClickManager.csproj -c Release -r win-x64 -o bin/output/x64
dotnet publish .\RightClickManager.csproj -c Release -r win-x86 -o bin/output/x86

del /q bin\output\x64\RightClickManager.pdb
del /q bin\output\x86\RightClickManager.pdb

rename "bin\output\x64\RightClickManager.exe" "右键菜单管理器.exe"
rename "bin\output\x86\RightClickManager.exe" "右键菜单管理器.exe"
