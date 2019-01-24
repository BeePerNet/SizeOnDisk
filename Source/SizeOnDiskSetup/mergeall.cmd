
if not exist FinalMasterInstaller md FinalMasterInstaller
copy en-US\SizeOnDiskSetup.msi FinalMasterInstaller\SizeOnDiskSetup.msi

call ..\..\CreateEmbedLangTransform.cmd de-DE 1031
call ..\..\CreateEmbedLangTransform.cmd es-ES 1034
call ..\..\CreateEmbedLangTransform.cmd fr-FR 1036
call ..\..\CreateEmbedLangTransform.cmd it-IT 1040
call ..\..\CreateEmbedLangTransform.cmd pt-BR 1046
call ..\..\CreateEmbedLangTransform.cmd ru-RU 1049
call ..\..\CreateEmbedLangTransform.cmd sv-SE 1053
REM not at this time, but good sample with ja.wxl       call ..\..\CreateEmbedLangTransform.cmd ja-JP 1041

cscript ..\..\WiLangId.vbs FinalMasterInstaller\SizeOnDiskSetup.msi Package 1031,1034,1036,1040,1046,1049,1053

echo *** Installer languages verification ***
cscript ..\..\wisubstg.vbs FinalMasterInstaller\SizeOnDiskSetup.msi
echo 1033 (default)

