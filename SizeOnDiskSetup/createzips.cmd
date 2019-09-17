if not exist FinalMasterInstaller md FinalMasterInstaller
del /q FinalMasterInstaller\*.zip

del /q ..\..\..\SizeOnDisk\bin\Release\*.xml

md SizeOnDiskFull
del /s /q SizeOnDiskFull
xcopy /S ..\..\..\SizeOnDisk\bin\Release\* SizeOnDiskFull
cd SizeOnDiskFull
..\..\..\7za a -mm=Deflate -mfb=258 -mpass=15 -r ..\FinalMasterInstaller\SizeOnDiskFull.zip *
cd ..

md SizeOnDiskMinimal
del /s /q SizeOnDiskMinimal
xcopy ..\..\..\SizeOnDisk\bin\Release\SizeOnDisk.exe SizeOnDiskMinimal
xcopy ..\..\..\SizeOnDisk\bin\Release\WPFByYourCommand.dll SizeOnDiskMinimal
xcopy ..\..\..\SizeOnDisk\bin\Release\WPFLocalizeExtension.dll SizeOnDiskMinimal
xcopy ..\..\..\SizeOnDisk\bin\Release\XAMLMarkupExtensions.dll SizeOnDiskMinimal
xcopy ..\..\..\SizeOnDisk\bin\Release\System.Collections.Immutable.dll SizeOnDiskMinimal
xcopy ..\..\..\SizeOnDisk\bin\Release\WinProps.dll SizeOnDiskMinimal
xcopy ..\..\..\SizeOnDisk\bin\Release\GongSolutions.WPF.DragDrop.dll SizeOnDiskMinimal
cd SizeOnDiskMinimal
..\..\..\7za a -mm=Deflate -mfb=258 -mpass=15 -r ..\FinalMasterInstaller\SizeOnDiskMinimal.zip *
cd ..
