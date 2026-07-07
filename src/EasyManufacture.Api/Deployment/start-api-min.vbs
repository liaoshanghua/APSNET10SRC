' APS 计划任务用：最小化启动 start-api.bat
Option Explicit
Dim sh, fso, base
Set sh = CreateObject("WScript.Shell")
Set fso = CreateObject("Scripting.FileSystemObject")
base = fso.GetParentFolderName(WScript.ScriptFullName)
If Right(base, 1) <> Chr(92) Then base = base & Chr(92)
sh.CurrentDirectory = base
' 7=最小化窗口；False=不等待
sh.Run "cmd /c " & Chr(34) & base & "start-api.bat" & Chr(34), 7, False
