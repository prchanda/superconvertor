REM If WINWORD.EXE and EXCEL.EXE exists, then required office binaries are already installed.
IF "%ComputeEmulatorRunning%" == "true" (
GOTO Finish
)ELSE (
IF EXIST "D:\Program Files (x86)\Microsoft Office\root\Office16\WINWORD.EXE" (
  ECHO Startup has already configured and installed required word processing binaries. Exiting. >> "%TEMP%\StartupLog.txt" 2>&1
  GOTO Finish
)
IF EXIST "D:\Program Files (x86)\Microsoft Office\root\Office16\EXCEL.EXE" (
  ECHO Startup has already configured and installed required excel processing binaries. Exiting. >> "%TEMP%\StartupLog.txt" 2>&1
  GOTO Finish
)

REM   Run your real exe task
ECHO Running setup.exe >> "%TEMP%\StartupLog.txt" 2>&1
start /wait %~dp0setup.exe /configure %~dp0configuration.xml >> %TEMP%\StartupLog.txt 2>>&1
ECHO Creating Desktop directory >> "%TEMP%\StartupLog.txt" 2>&1
MKDIR "D:\Windows\System32\config\systemprofile\Desktop"

IF %ERRORLEVEL% EQU 0 (
  REM   The application installed without error.

  ECHO The application installed without error. > "%RoleRoot%\Success.txt"

) ELSE (
  REM   An error occurred. Log the error and exit with the error code.

  DATE /T >> "%TEMP%\StartupLog.txt" 2>&1
  TIME /T >> "%TEMP%\StartupLog.txt" 2>&1
  ECHO  An error occurred running the setup. Errorlevel = %ERRORLEVEL%. >> "%TEMP%\StartupLog.txt" 2>&1

  EXIT %ERRORLEVEL%
)
)
:Finish

REM   Exit normally.
EXIT /B 0