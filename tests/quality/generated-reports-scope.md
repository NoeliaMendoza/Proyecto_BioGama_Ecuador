# Correccion del alcance: reportes generados

La exportacion evidencias/20261103-104640-20a635 muestra new_coverage=34.6,
new_duplicated_lines_density=4.24242 y new_violations=4.

Los cuatro hallazgos nuevos estan en archivos de la herramienta de cobertura:
- css:S4649: tests/quality/js-coverage/lcov-report/base.css.
- Web:InputWithoutLabelCheck: lcov-report/index.html.
- Web:InputWithoutLabelCheck y Web:S5256: lcov-report/product-stock.js.html.

El HTML contiene una copia del codigo del catalogo para mostrar su cobertura;
no forma parte de la aplicacion publicada. Contarlo como codigo nuevo altera
la base de comparacion e introduce duplicacion ajena a un cambio funcional.

Correccion metodologica:
1. Invoke-BrowserTests.ps1 genera los reportes temporales en .local/js-coverage.
2. Se conserva el LCOV de la ejecucion en tests/quality/javascript.lcov.info.
3. Sonar excluye solo .local/**, evidencias/** y los directorios generados
   tests/quality/js-coverage/** y tests/quality/TestResults/**. Esto tambien
   protege el analisis de reportes anteriores que ya existan en disco.
4. wwwroot/js/product-stock.js, Controllers, Services, Data, Models, Views,
   manifiestos, Dockerfile y datos.py permanecen en el alcance de produccion.

No se cambio el Gate ni sus umbrales (80% cobertura, 3% duplicacion nueva,
cero problemas nuevos). Tampoco se agregaron exclusiones de cobertura de
clases de negocio. Se mantienen ambos reportes C# y JavaScript.

El proximo analisis debe recalcular los indicadores. No se afirma que alcance
80% hasta obtener ese resultado. Para el informe, dejar constancia de esta
correccion de alcance: el analisis anterior incluia artefactos generados.
