
if not exist FinalMasterInstaller md FinalMasterInstaller
copy en-US\SizeOnDiskSetup.msi FinalMasterInstaller\SizeOnDiskSetup.msi

echo %CD%
echo *** Transforms fr ***
call ..\..\CreateEmbedLangTransform.cmd fr-FR 1036
echo *** Transforms de ***
call ..\..\CreateEmbedLangTransform.cmd de-DE 1031
echo *** Transforms es ***
call ..\..\CreateEmbedLangTransform.cmd es-ES 1034
echo *** Transforms it ***
call ..\..\CreateEmbedLangTransform.cmd it-IT 1040
echo *** Transforms pt ***
call ..\..\CreateEmbedLangTransform.cmd pt-BR 1046
echo *** Transforms ru ***
call ..\..\CreateEmbedLangTransform.cmd ru-RU 1049
echo *** Transforms sv ***
call ..\..\CreateEmbedLangTransform.cmd sv-SE 1053
REM not at this time, but good sample with ja.wxl       call ..\..\CreateEmbedLangTransform.cmd ja-JP 1041

echo *** Merge ***
REM cscript ..\..\WiLangId.vbs FinalMasterInstaller\SizeOnDiskSetup.msi Package 1036,1031,1034,1040,1046,1049,1053

echo *** Installer languages verification ***
cscript ..\..\wisubstg.vbs FinalMasterInstaller\SizeOnDiskSetup.msi
echo The end

