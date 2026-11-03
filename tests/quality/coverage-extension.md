# Ampliacion de pruebas despues del resultado 48.2%

Fuente anterior: evidencias/20261103-102317-b7308f/sonarqube/quality-gate.json.
El Gate indica new_coverage=48.2 frente a 80 y new_violations=1 frente a 0.
Las metricas globales indican 174 problemas abiertos y cobertura global 9.1%.
No confundir esas medidas globales con sus equivalentes sobre codigo nuevo.

El hallazgo nuevo ASP0027 se refiere a public partial class Program, declaracion
que este SDK ya no necesita. Se retiro la declaracion en lugar de suprimir la
regla; el proyecto de pruebas sigue compilando y el host de pruebas arranca.
Referencia: https://learn.microsoft.com/en-us/aspnet/core/diagnostics/asp0027

Se ampliaron las pruebas C# de 35 a 43 con escenarios de cuentas iniciales y
registros: claves ausentes, roles correctos, conservacion de credenciales y
privilegios existentes, error explicito ante claves invalidas, fechas UTC,
notificaciones fallidas y control del perfil del investigador.

Se incorporaron nueve pruebas de comportamiento del cliente JS en jsdom. Se
obtuvieron 100% de lineas, ramas y funciones del archivo product-stock.js.
No se ha medido todavia el nuevo porcentaje de SonarQube: debe importar ambos
reportes, coverage.xml y javascript.lcov.info. No se rebajo ningun umbral ni
se excluyo codigo de negocio del analisis para aprobar el Gate.

Evidencias actuales:
- automated-tests.trx y automated-tests.txt: 43 pruebas C# aprobadas.
- coverage.xml: cobertura dinamica C# de Microsoft.
- browser-tests.txt: nueve pruebas JavaScript y tabla de cobertura.
- javascript.lcov.info: reporte LCOV del archivo de produccion.
- npm-audit.json: cero vulnerabilidades conocidas en herramientas JavaScript.

Analyze-And-Save.ps1 ejecuta ambos pasos de pruebas antes de publicar. Si uno
falla, no publica el analisis como si hubiera pasado.
