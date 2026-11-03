# Evidencias publicadas y archivos locales

Se publican las consultas JSON de SonarQube en las carpetas fechadas, que
permiten contrastar los cambios de metricas y Quality Gate. Las carpetas
completas de cada exportacion se conservan en el equipo de trabajo; incluyen
copias repetidas de cobertura y resultados que no se incorporan a Git.

Los manifiestos de esas exportaciones locales describen su contenido completo,
no el subconjunto publicado en el repositorio. No se publican como manifiestos
del subconjunto. Los archivos originales no se han eliminado.

Para las otras pruebas consultar las fuentes publicadas en:
- tests/k6/results: resultados y analisis de rendimiento.
- tests/network: mediciones y comparacion normal/degradado.
- tests/realtime: verificacion con clientes en replicas distintas.
- tests/quality: resumen de pruebas, auditorias y notas de calidad.
- tests/security/zap: reportes originales y comparaciones de seguridad.

Los XML grandes de cobertura, TRX y LCOV generados quedan fuera de Git. El
resumen JSON de pruebas identifica el hash del TRX local del que se extrajo.
Las capturas compartidas solo en el chat deben reunirse antes de cerrar el PDF.
