:run.bat
@echo off
cd "Server_Executive\bin\Debug"
start Server_Executive.exe 8080
start Server_Executive.exe 8081
cd "..\..\..\"
cd "WPF_GUI\bin\Debug"
start WPF_GUI.exe
start WPF_GUI.exe 
cd "..\..\..\"