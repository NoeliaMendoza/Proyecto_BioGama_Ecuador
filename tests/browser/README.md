# Pruebas del catalogo en tiempo real

Se ejecuta el archivo real wwwroot/js/product-stock.js dentro de un DOM jsdom.
Se simulan fetch, SignalR y temporizadores, sin consultar la aplicacion ni Redis.
Son pruebas de comportamiento del cliente; no reemplazan pruebas visuales en
Chrome/Edge ni pruebas de transporte WebSocket entre pods.

Los nueve casos verifican disponibilidad inicial con filtros, agotamiento y
reposicion, limites de cantidad tras reservas, producto retirado, reconexion,
fallos HTTP, reintentos iniciales, eventos concurrentes, respaldo periodico y foco.
Se comprueban texto, estado de controles y solicitudes de sincronizacion.

```powershell
.\scripts\Invoke-BrowserTests.ps1
```

Resultado observado: 9 aprobadas, 0 fallidas; cobertura de product-stock.js de
100% en lineas, ramas y funciones con c8. Este porcentaje corresponde solo a ese
archivo: no equivale a cobertura global ni cobertura de codigo nuevo de SonarQube.
El reporte esta en tests/quality/javascript.lcov.info y se importa usando
sonar.javascript.lcov.reportPaths en Analyze-And-Save.ps1.

Las dependencias son herramientas de pruebas, no se publican en la aplicacion.
package-lock.json fija la instalacion. glob y minimatch se fijan mediante
overrides a versiones corregidas; se validaron con la suite y npm audit sin
vulnerabilidades conocidas. No se uso npm audit fix --force.
