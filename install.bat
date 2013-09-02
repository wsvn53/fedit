rem add UNC support
reg add  "HKEY_CURRENT_USER\Software\Microsoft\Command Processor" /v DisableUNCCheck /t REG_DWORD /d 1 /f

@if exist "%ProgramFiles%\\Fiddler2\\Scripts\\Fedit.dll" (
	rem if update
	@echo "Copy %CD%\\Fedit.dll to %ProgramFiles%\\Fiddler2\\Scripts\\Fedit.dll"
	@copy "%CD%\\Fedit\\bin\\Release\\Fedit.dll" "%ProgramFiles%\\Fiddler2\\Scripts\\"
)else if exist "%userprofile%\\Documents\\Fiddler2\\Scripts\\Fedit.dll" (
	rem if update
	@echo "Copy %CD%\\Fedit.dll to %userprofile%\\Documents\\Fiddler2\\Scripts\\Fedit.dll"
	@copy "%CD%\\Fedit\\bin\\Release\\Fedit.dll" "%userprofile%\\Documents\\Fiddler2\\Scripts\\"
)else if exist "%userprofile%\\My Documents\\Fiddler2\\Scripts\\Fedit.dll" (
	rem if update
	@echo "Copy %CD%\\Fedit.dll to %userprofile%\\My Documents\\Fiddler2\\Scripts\\Scripts\\Fedit.dll"
	@copy "%CD%\\Fedit\\bin\\Release\\Fedit.dll" "%userprofile%\\My Documents\\Fiddler2\\Scripts\\"
)else if exist "%ProgramFiles%\\Fiddler2\\" (
	@echo "Copy %CD%\\Fedit.dll to %ProgramFiles%\\Fiddler2\\Scripts\\"
	@copy "%CD%\\Fedit\\bin\\Release\\Fedit.dll" "%ProgramFiles%\\Fiddler2\\Scripts\\"
)else if exist "%userprofile%\\Documents\\Fiddler2\\" (
	@echo "Copy Fedit.dll to %userprofile%\\Documents\\Fiddler2\\"
	copy "%CD%\\Fedit\\bin\\Release\\Fedit.dll" "%userprofile%\\Documents\\Fiddler2\\Scripts\\"
)else if exist "%userprofile%\\My Documents\\Fiddler2\\" (
	@echo "Copy Fedit.dll to %userprofile%\\My Documents\\Fiddler2\\"
	copy "%CD%\\Fedit\\bin\\Release\\Fedit.dll" "%userprofile%\\My Documents\\Fiddler2\\Scripts\\"
)
@echo "installed."
@pause
