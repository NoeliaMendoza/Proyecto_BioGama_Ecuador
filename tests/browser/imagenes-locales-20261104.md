# Fotografías locales y orden de las capas

Cambio desplegado en web `k8s-v20` el 4 de noviembre de 2026,
America/Guayaquil. Las fechas de los JSON están expresadas en UTC.

## Diagnóstico

El usuario observó marcadores de reemplazo en lugar de todas las fotografías.
En un Chrome con perfil nuevo, las fotografías sí estaban cargadas y por encima
del marcador. Esa comprobación no describía necesariamente la caché ni los
errores de red del navegador del usuario.

Se identificó que `site.css` importaba `components.css` y otras hojas sin
versionarlas individualmente. Una copia antigua de los estilos podía conservar
la imagen en el flujo normal, después de un marcador de altura completa,
dentro de un contenedor con `overflow: hidden`. Una simulación controlada de
esa disposición reprodujo una imagen presente y cargada, pero invisible en
el centro del contenedor. Esto demuestra el mecanismo; no constituye una
inspección de la caché real del usuario.

También se detectó que el manejador anterior eliminaba la etiqueta `<img>` al
recibir un error. La primera comprobación solo contaba imágenes rotas que
seguían en el DOM, por lo que era insuficiente para verificar que las fotos
realmente se mostraran.

## Corrección

- Todas las plantillas comparten `_ApplicationStyles`, que carga cada hoja
  con `asp-append-version`. Se eliminaron las importaciones indirectas sin
  versión de `site.css`, manteniendo el orden de la cascada.
- Las capas son explícitas: marcador en nivel 0, foto en nivel 1 e insignia
  de estado en nivel 2.
- Se descargaron las 35 imágenes originales referenciadas por los cursos:
  5.555.555 bytes en total, con validación del formato, límite de tamaño y
  comprobación de los orígenes HTTPS y sus redirecciones.
- Los archivos se incluyen en `wwwroot/images/courses/`. El Tag Helper
  `asp-course-image` selecciona la copia local mediante la huella de la URL.
  Las direcciones originales permanecen en la base de datos. Si no hay copia
  local para una imagen futura, conserva la dirección original.
- Un error oculta la imagen con `hidden`, conservando su elemento y dirección.
  Un evento de carga correcto vuelve a mostrarla. No se utilizan manejadores
  inline ni se amplían las directivas CSP para realizar esta corrección.

## Comprobaciones

- Compilación sin advertencias ni errores y **178/178 pruebas .NET**.
- **5/5 pruebas de interfaz**, incluyendo conservación del nodo y recuperación
  después de un evento `load`.
- Chrome recorrió las tres páginas y desplazó cada fila para activar la carga
  diferida. Se comprobaron las **34 fotografías de los cursos visibles**:
  elemento presente, URL local, ancho natural positivo y ausencia de `hidden`.
- La prueba bloqueó los servidores externos de imágenes: hubo **cero
  solicitudes de fotografías externas** y cero violaciones CSP en el recorrido.
- Las primeras tres fotos se comprobaron además mediante `elementFromPoint`:
  la imagen era el elemento superior en el centro de su contenedor.
- La simulación del CSS antiguo conservó la foto cargada pero fuera de la
  capa visible. Se retiró el estilo de simulación antes de guardar la captura
  final; no se modificó la aplicación para esa simulación.

Evidencias:

- [Comprobación en Chrome](../quality/course-local-images-browser-check.json).
- [Inventario y huellas de las imágenes](../quality/course-local-assets.json).
- `.local/evidencias-cursos/v20/01-fotos-locales.png`: catálogo real con las
  fotografías cargadas desde BioGama y servidores externos bloqueados.
- `.local/evidencias-cursos/v20/02-ultima-fila-fotos-locales.png`: última fila
  de la primera página bajo las mismas condiciones.

Las capturas permanecen fuera de Git. Los resultados corresponden a las
condiciones indicadas, no sustituyen una comprobación del navegador del usuario
ni un nuevo análisis SonarQube/ZAP.
