# Refactorización de los nueve archivos señalados por SonarQube

Referencia: captura del usuario con duplicación global del 1,6 %, nueve archivos.
Los porcentajes siguientes son anteriores a esta refactorización.

| Archivos | Repetición anterior | Cambio |
| --- | --- | --- |
| SalesModuleSeeder.cs | 96 líneas | Catálogo JSON embebido y una lógica de carga por tipo de entidad |
| Home/Index.cshtml | 144 líneas | Parciales compartidas de gráficos y actividad reciente |
| Locations, Families, Researchers, NaturalReserves y Species / Index.cshtml | 22–27 líneas por archivo | Parcial de acciones por fila, con las mismas diferencias entre roles |
| SetPassword y ChangePassword / cshtml.cs | 25 líneas por archivo | Modelo base de contraseña nueva y confirmación |

## Contratos que se preservan

- Los investigadores pueden editar las entidades del catálogo, pero no otros
  investigadores. Solo el administrador ve la acción Eliminar. Las restricciones
  de los controladores permanecen independientes de la visibilidad de botones.
- Se conserva el orden de los botones y el uso de tooltips de cada listado.
- Las contraseñas mantienen sus nombres de binding, mensajes, mínimo y máximo,
  y comparación de confirmación. El cambio de contraseña conserva OldPassword.
- La actividad reciente codifica el contenido y conserva el mensaje sin datos.
- El catálogo inicial mantiene cuatro cursos y tres productos, precios, stock,
  imágenes, textos, horarios y vínculos opcionales con especies. Las fechas
  relativas se calculan desde un único instante UTC. La carga sigue omitiéndose
  si ya existen cursos; no se ejecutó sobre los datos reales del usuario.
- El recurso JSON viaja dentro del ensamblado y se incluye en la copia de fuentes
  de las pruebas. No requiere un archivo editable externo en producción.

No se excluyen estas vistas, el catálogo ni sus servicios de SonarQube. El nuevo
porcentaje se debe medir con otra ejecución; este documento no certifica 0 %.
La exclusión de CPD de las migraciones generadas, ya documentada, sigue siendo
la misma que en la medición del 1,6 %.

Validación: compilación Windows sin advertencias ni errores y 145/145 pruebas
.NET aprobadas en Linux, ninguna omitida. El primer intento de las pruebas de
renderizado necesitó completar su contexto de rutas; la suite final incluye
comprobación del enlace Details y de la matriz de permisos. Resultados activos:
`tests/quality/coverage-runtime.json` y `tests/quality/automated-tests.txt`.
