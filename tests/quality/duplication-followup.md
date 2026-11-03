# Resultado y correccion de accesibilidad

Fuente: evidencias/20261103-120544-78bd7e/sonarqube/quality-gate.json y
hallazgos-abiertos.json; captura del dashboard compartida por el usuario.

Tras compartir los campos de cinco pares de formularios:
- Duplicacion global: 8.7 % a 7.0 %.
- Cobertura global: 15.4 % a 15.9 %.
- Problemas abiertos: 171 a 169.
- Cobertura de codigo nuevo: 86.0 %, aprobada.
- Duplicacion de codigo nuevo: 0.0 %, aprobada.
- Gate: ERROR por dos hallazgos nuevos, limite cero.

Los dos hallazgos apuntan a Views/Records/_FormFields.cshtml: el campo de
nombre de investigador de solo lectura carecia de id y su label no tenia for.
Se asigno id=CurrentResearcherName y for=CurrentResearcherName. Se amplio la
prueba de la rama de investigador para comprobar ambos atributos junto con
la codificacion HTML del nombre. Compilacion sin errores ni advertencias.

La correccion no altera el valor oculto ResearcherId ni los permisos del
controlador. Su cierre en SonarQube requiere publicar un analisis nuevo.
