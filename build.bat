REM u2pa is best build using VisualStudio, however this script can build the assemblies as well.
mkdir bin
del /S /Q bin
cd Lib
C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe /target:library /r:Resources\LibUsbDotNet.dll /out:..\bin\u2pa.lib.dll /recurse:*.cs
mkdir ..\bin\xml
copy xml\*.x* ..\bin\xml\
copy Resources\*.dll ..\bin
cd ..
cd Cmd
C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe /r:..\bin\u2pa.lib.dll /out:..\bin\u2pa.exe /recurse:*.cs
mkdir ..\bin\help
copy help\*.* ..\bin\help
cd ..
