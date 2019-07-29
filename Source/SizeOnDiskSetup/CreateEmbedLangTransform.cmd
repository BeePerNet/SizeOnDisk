set lang=%1
set langcode=%2
echo Merging %lang% %langcode%
cscript ..\..\WiLangId.vbs %lang%\SizeOnDiskSetup.msi Product %langcode%
REM cscript ..\..\WiLangId.vbs %lang%\SizeOnDiskSetup.msi Package %langcode%
..\..\MsiTran.exe -g en-US\SizeOnDiskSetup.msi %lang%\SizeOnDiskSetup.msi %lang%\language.mst
cscript ..\..\wisubstg.vbs FinalMasterInstaller\SizeOnDiskSetup.msi %lang%\language.mst %langcode%
REM cscript ..\..\wisubstg.vbs FinalMasterInstaller\SizeOnDiskSetup.msi
