# Pruebas locales de rendimiento

Ejecutar desde la raiz del repositorio, con BioGama disponible en localhost:30080.

```powershell
k6 run --summary-export tests/k6/results/smoke.json tests/k6/smoke-test.js
k6 run --summary-export tests/k6/results/baseline-info.json tests/k6/baseline-test.js
k6 run -e TEST_PATH=/Products --summary-export tests/k6/results/baseline-products.json tests/k6/baseline-test.js
```

La prueba smoke usa 1 usuario virtual durante 10 segundos y comprueba el JSON.
Cada baseline usa 100 usuarios virtuales durante 30 segundos, con una pausa de
0.1 segundos por iteracion. No es una tasa fija de solicitudes: depende del
tiempo de respuesta. Las pruebas HTTP no descargan recursos ni ejecutan JavaScript
como un navegador.

Se adopta el objetivo del ejemplo de clase: errores HTTP menores al 1% y p(95)
menor a 250 ms. Se exige ademas que todas las comprobaciones de respuesta pasen.
Un umbral incumplido produce una salida distinta de cero; conservar sus resultados.

Comparar /api/info (sin consulta SQL) con /Products (consultas a PostgreSQL y
renderizado HTML). No extrapolar estos resultados a pagos, IA o tiempo real.
La base inicial tiene 3 productos y 0 especies: son resultados de un conjunto
pequeno. El generador de carga, los dos pods web y PostgreSQL comparten equipo.
La persistencia de conexiones HTTP puede hacer que un usuario mantenga el mismo pod.

Guardar los JSON y las capturas del resumen para el informe. No inventar mediciones.
Documentar replicas, recursos, datos y condiciones de red al comparar ejecuciones.

Referencia: https://grafana.com/docs/k6/latest/using-k6/thresholds/
