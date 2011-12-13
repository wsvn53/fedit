@if exist "C:\\Program Files\\Fiddler2\\" (
	@echo "Copy Fedit.dll to C:\\Program Files\\Fiddler2\\"
	copy Fedit\\bin\\Release\\Fedit.dll "C:\\Program Files\\Fiddler2\\Scripts\\"
)else if exist "D:\\Program Files\\Fiddler2\\" (
	@echo "Copy Fedit.dll to D:\\Program Files\\Fiddler2\\"
	copy Fedit\\bin\\Release\\Fedit.dll "D:\\Program Files\\Fiddler2\\Scripts\\"
)else if exist "%userprofile%\\Documents\\Fiddler2\\" (
	@echo "Copy Fedit.dll to %userprofile%\\Documents\\Fiddler2\\"
	copy Fedit\\bin\\Release\\Fedit.dll "%userprofile%\\Documents\\Fiddler2\\Scripts\\"
)else if exist "%userprofile%\\My Documents\\Fiddler2\\" (
	@echo "Copy Fedit.dll to %userprofile%\\My Documents\\Fiddler2\\"
	copy Fedit\\bin\\Release\\Fedit.dll "%userprofile%\\My Documents\\Fiddler2\\Scripts\\"
)
@echo "installed."
@pause