# Cobertura Linux para el análisis de SonarQube

El intento del 4 de noviembre a las 17:28 compiló, pero Windows bloqueó la DLL
durante la carga de las pruebas (`FileLoadException`, `0x800711C7`). Ese intento
no publicó un análisis nuevo; el Gate OK de las 12:04 corresponde al anterior.

## Solución preparada

`Analyze-And-Save.ps1` usa ahora `CoveragePlatform=Linux` como valor predeterminado.
La compilación asociada a SonarScanner se mantiene en Windows, mientras que las
pruebas y la recolección se ejecutan en una copia de las fuentes dentro de Docker
con el SDK .NET 10. No se cambian las políticas de Windows ni se ejecuta la DLL
bloqueada del host: la copia se compila de nuevo en Linux.

El contexto de pruebas no incluye `.git`, `.local`, credenciales, `node_modules`
ni los archivos privados `appsettings.json` y `appsettings.Development.json`.
Se registra un manifiesto SHA256 de las fuentes copiadas.

El recolector es Coverlet 6.0.4 en formato OpenCover, usando
`tests/coverage.runsettings`. El importador verifica que el TRX tenga pruebas
ejecutadas, que no haya fallos y que el reporte contenga líneas ejecutadas del
ensamblado `BioGamaEcuador`. VSTest puede duplicar un adjunto: se aceptan sus
copias únicamente si tienen el mismo SHA256; reportes diferentes se rechazan.

Las rutas `/src/...` se convierten a las rutas reales del proyecto en Windows
para asociar la cobertura con los archivos analizados. No se alteran visitas,
ramas ni contadores. SonarScanner importa exclusivamente ese reporte mediante
`sonar.cs.opencover.reportsPaths=tests/quality/coverage.opencover.xml`.

## Resultado observado

- Compilación Linux: cero errores y cero advertencias.
- 121 pruebas aprobadas, cero fallidas y cero omitidas.
- Reporte original OpenCover generado, con 1244 puntos de secuencia visitados
  del ensamblado BioGamaEcuador.
- 240 archivos del reporte asociados con rutas locales.
- Metadatos del recolector y del reporte activo: `tests/quality/coverage-runtime.json`.

Los artefactos originales y el manifiesto de fuentes están bajo
`.local/LinuxCoverage/`; los reportes vigentes se copian a `tests/quality/` para
que el exportador los conserve en la siguiente instantánea.

## Ejecución local

Con Docker Desktop, Kubernetes, PostgreSQL, Redis y SonarQube disponibles:

```powershell
.\scripts\Analyze-And-Save.ps1
```

El token se introduce únicamente en el prompt oculto. La alternativa histórica
Windows sigue disponible con `-CoveragePlatform Windows` en entornos donde su
ejecución esté permitida. Los helpers permanecen locales, como se acordó para
la carpeta `scripts/`.

Este cambio usa otro recolector y plataforma respecto a la cobertura dinámica
Windows anterior. Al comparar métricas, documentar ese cambio junto con las
mejoras del código; no atribuir automáticamente todas las diferencias a nuevas
pruebas ni afirmar que las métricas de SonarQube ya están actualizadas.
