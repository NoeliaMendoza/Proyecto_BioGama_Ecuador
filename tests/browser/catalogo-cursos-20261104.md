# Catálogo de cursos recuperados: estados e imágenes

Esta nota documenta `k8s-v19`. La carga de fotografías y la comprobación de capas
se reforzaron posteriormente en [k8s-v20](imagenes-locales-20261104.md).

Comprobación del 4 de noviembre de 2026, America/Guayaquil, sobre web
`k8s-v19`, dos réplicas y la base restaurada. El JSON conserva la hora UTC.

La vista anterior confundía cualquier curso iniciado con uno sin cupos y
desactivaba su enlace de consulta. Ahora distingue cursos finalizados, en curso,
abiertos y agotados; el filtro de agotados solo incluye cursos futuros sin cupos.
El fin se compara por fecha, incluyendo el último día del curso. Las reservas
continúan cerradas una vez iniciada la actividad. Consultar los detalles de un
curso histórico permanece permitido. No se modificaron las fechas de la base.

Las imágenes restauradas usan `i.pinimg.com`, `upload.wikimedia.org` y
`picsum.photos`, que redirige imágenes a `fastly.picsum.photos`. Se agregaron esos
orígenes HTTPS concretos a `img-src` mediante la configuración existente. Las
directivas de scripts y estilos mantienen sus restricciones. Si una imagen
remota falla, un manejador externo de eventos deja visible el marcador del
curso; no utiliza atributos `onerror` ni código inline.

Validación:

- Compilación de pruebas sin advertencias ni errores; **172/172 pruebas .NET**,
  incluidas cuatro regresiones de filtros de estado.
- **5/5 pruebas de acciones de interfaz**, incluidas imágenes que fallan antes
  y después de inicializar el manejador.
- Comprobación en Chrome aislado del catálogo, sus cuatro filtros y el detalle
  de un curso histórico. Sin enlaces de detalle desactivados, imágenes rotas
  completadas ni violaciones CSP en las páginas recorridas. Los filtros sin
  registros muestran un estado vacío.
- Se desplazó la página hasta la última fila para cargar sus imágenes lazy.
  Las imágenes de aves, rehabilitación de fauna y anfibios de la captura del
  usuario cargaron correctamente y sus etiquetas muestran «Finalizado».

Resultado legible por máquina:
[course-catalog-browser-check.json](../quality/course-catalog-browser-check.json).

Capturas reales guardadas localmente, fuera de Git:

- `.local/evidencias-cursos/v19/01-catalogo-recuperado.png`: inicio del catálogo.
- `.local/evidencias-cursos/v19/02-catalogo-ultima-fila.png`: fila de la captura
  inicial con las tres imágenes visibles y el estado corregido.

Las imágenes dependen de servicios externos; el marcador evita mostrar imágenes
rotas cuando esos servicios fallan. Estas comprobaciones no constituyen un
nuevo análisis SonarQube ni un nuevo informe OWASP ZAP.
