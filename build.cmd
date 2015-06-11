@echo Off
set config=%1
if "%config%" == "" (
   set config=Release
)

REM Package restore
tools\nuget.exe restore NuGet.Services.Search.sln -OutputDirectory %cd%\packages -NonInteractive

REM Build
%WINDIR%\Microsoft.NET\Framework\v4.0.30319\msbuild NuGet.Services.Search.sln /p:Configuration="%config%" /m /v:M /fl /flp:LogFile=msbuild.log;Verbosity=Normal /nr:false
