# Correccion del bloqueo de cobertura en Windows

La ejecucion del usuario del 3 de noviembre de 2026 a las 10:10 no descubrio
pruebas porque Control de aplicaciones bloqueo BioGamaEcuador.dll al cargarla
en testhost.exe. El TRX registra FileLoadException 0x800711C7 y cero pruebas.
Los eventos 3077 de CodeIntegrity confirman ese bloqueo. No fue un fallo de
aserciones de las pruebas ni una ausencia del adaptador xUnit.

Se probo dotnet-coverage 18.12.0 con instrumentacion administrada dinamica.
Se deshabilita la instrumentacion estatica en la configuracion del recolector;
no se cambia la politica de Windows ni se agrega una exclusion de seguridad.
El alcance sigue siendo el ensamblado BioGamaEcuador completo, sin excluir
clases de negocio para incrementar la cobertura.

Resultado observado: 35 pruebas aprobadas, 0 fallidas y reporte coverage.xml.
La salida se conserva en dynamic-coverage-tests.txt. Se actualizo
Analyze-And-Save.ps1 para usar ese recolector e importar el XML mediante
sonar.cs.vscoveragexml.reportsPaths. Tambien comprueba el TRX y rechaza una
ejecucion con cero pruebas; no publicara un analisis como si hubiera pasado.

La validacion del pipeline completo en la sesion interactiva del usuario
requiere ejecutar otra vez Analyze-And-Save.ps1 con su token oculto.

## Correccion del paso de argumentos

La siguiente ejecucion del usuario detecto otro fallo: el comando anidado
con una ruta absoluta con espacios se fragmentaba al pasar por PowerShell.
Se reemplazo por Invoke-CoverageTests.ps1, que pasa ejecutable y argumentos
separados despues de --. Analyze-And-Save.ps1 llama a ese mismo script.

Se ejecuto directamente el helper corregido desde la ruta real del proyecto:
35 pruebas ejecutadas, 35 aprobadas y coverage.xml generado. Esta comprobacion
valida el mismo paso de pruebas usado por el pipeline, sin requerir un token.

Referencias:
https://learn.microsoft.com/en-us/dotnet/core/additional-tools/dotnet-coverage
https://docs.sonarsource.com/sonarqube-community-build/analyzing-source-code/test-coverage/dotnet-test-coverage
