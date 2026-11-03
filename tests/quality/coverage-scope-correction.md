# Correccion del alcance de cobertura despues de 68.4 %

El fichero local .sonarqube/out/ScannerEngineInput.json del ultimo analisis
clasifica el proyecto BioGamaEcuador.Tests como Test y stock.test.cjs como test.
Sin embargo, incluye como fuentes principales estos scripts de prueba:

- tests/check_generator_config.py: comprobacion aislada de configuracion.
- tests/python/collect.py: ejecutor que instrumenta esa comprobacion.
- tests/k6/*.js: escenarios de carga y humo, no JavaScript de la aplicacion.

Se configura sonar.coverage.exclusions exclusivamente para esas rutas. Sus
hallazgos de analisis estatico siguen analizandose: esta propiedad solo cambia
el calculo de cobertura. No se excluyen datos.py, controladores, servicios,
vistas ni product-stock.js. Los umbrales del Quality Gate siguen iguales.

La correccion cambia el denominador de cobertura; cualquier aumento causado
por ella debe describirse como cambio de medicion, no como codigo adicional
ejecutado por pruebas. Las 65 pruebas C# y las nueve de JavaScript siguen siendo
la evidencia de comportamientos probados. No se anuncia aprobacion del Gate
hasta que el servidor procese un nuevo analisis.

Se agrega al exportador metricas-codigo-nuevo.json para guardar cantidades
de lineas y condiciones nuevas, incluso si el desglose por archivo sigue
devolviendo HTTP 403. Las advertencias de permisos no equivalen a pruebas fallidas.

Referencia oficial sobre separacion entre fuentes y pruebas:
https://docs.sonarsource.com/sonarqube-server/2025.5/project-administration/setting-analysis-scope/setting-initial-scope
