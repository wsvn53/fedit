# Overflow
`Fedit` is a plugin for [Fiddler](http://www.fiddler2.com/). You can use this plugin to edit response directly, instead of add AutoResponder rule manually.  
This plugin required Fiddler version `2.1.1.3` or later.  

Plugin Version: 1.1.0

#Install
Run the bat file `install.bat` at root path, or:  
Copy `Fedit/bin/Release/Fedit.dll` to `%My Documents%/Fiddler2/Scripts/`.  
or copy to ``%Program Files%/Fiddler2/Scripts/` (Fiddler install path).

#Usage
1. Select one or more sessions in session list.
2. Right-click, execute "Edit" command in popup menu.  

**Note:** You do not need to remove AutoResponder rule or delete the temporary file, it will clean automatically at next time you start up Fiddler.

# Setting
If installed correctly, you will see a tab page at left side of fiddler named "Fedit".  
Add your favor editor for file type, e.g. 
    default notepad
    .css    notepad
    .js     C:\\Program Files\Notepad++/notepad++.exe
    .txt    notepad
    .htm    C:\\Program Files\Notepad++/notepad++.exe
**Notice** that, `filetype` must start with `.`  
By default, there is a `default` editor for you `notepad`.

# Contact
* Twitter:&nbsp; [@ethan168](https://twitter.com/ethan168)
* Email: &nbsp;&nbsp; [wsvn53@gmail.com](mailto:wsvn53@gmail.com)
* Blog: &nbsp;&nbsp;&nbsp;&nbsp; [http://imethan.com](http://imethan.com)