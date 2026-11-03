# Correccion de cuatro hallazgos nuevos tras el refuerzo CSP

Fuente: evidencias/20261103-133645-6d0618/sonarqube/quality-gate.json y
hallazgos-abiertos.json. El Gate fallo solamente por new_violations=4.
new_coverage=93.1 y new_duplicated_lines_density=0.0 estaban aprobadas.

Tres hallazgos ASP0015 pedian propiedades tipadas del diccionario de cabeceras.
Se usan XContentTypeOptions, XFrameOptions y ContentSecurityPolicy en lugar de
indexadores de cadena. Los valores de proteccion no cambian.

El cuarto, javascript:S2187, senalaba el fichero de comprobacion de Chrome:
contenia aserciones pero se ejecutaba como script, sin registrar una prueba.
Ahora registra un test de node:test con timeout y conserva las comprobaciones
reales de CSP, stock en vivo y controles IA; no se elimino ni excluyo el archivo.

Validacion: compilacion sin errores ni advertencias, 110 pruebas C# aprobadas
y una prueba Node/Chrome real aprobada. El resultado de paginas se guarda en
browser-security-check.json. La suite jsdom existente de 15 casos no cambia.

La prueba Chrome consulta la imagen desplegada v10. El cambio de API de
cabeceras, equivalente en comportamiento, esta en el codigo local y aun no
se reconstruyo en otra imagen. Se necesita publicar otro analisis SonarQube
para confirmar el cierre de los cuatro hallazgos; no se declara Gate aprobado.
