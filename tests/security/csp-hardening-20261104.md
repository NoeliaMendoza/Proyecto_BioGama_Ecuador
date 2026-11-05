# Corrección CSP: versión k8s-v14

## Antes

La sesión manual de ZAP sobre `https://localhost:30080` identificó siete tipos
de alerta: dos medios, dos bajos y tres informativos. Los medios propios fueron
`10055-4` (img-src demasiado amplio) y `10055-6` (style-src unsafe-inline).
Los dominios externos de Google se separan del resultado de BioGama.

Un resumen de solo nombres, riesgos y rutas de esa sesión está preservado en
`.local/evidencias-csp/20261104/zap-before-summary.json`. Sus 85 registros son
instancias de siete tipos; no deben presentarse como 85 vulnerabilidades distintas.
No incluye credenciales, cookies, consultas URL ni cuerpos de peticiones.

## Cambios

- Se migraron 85 atributos de estilo a clases CSS en las vistas y en Identity.
- Los bloques style de Razor reciben el mismo nonce criptográfico por respuesta
  que los scripts; `style-src-attr 'none'` bloquea atributos de estilo arbitrarios.
- La barra de cupos usa un elemento progress y valores numéricos acotados.
- img-src permite self, data y orígenes HTTPS concretos de
  `Security:ImageOrigins`. El manifiesto incluye biogama.ec y los tres orígenes
  realmente utilizados por las imágenes actuales: encrypted-tbn0.gstatic.com,
  http2.mlstatic.com y avesconservacion.org. No permite cualquier sitio HTTPS.
- La configuración rechaza comodines, HTTP, credenciales, rutas y consultas.
  Para incorporar otro proveedor de imágenes, revisar y añadir su origen concreto.

La comprobación de v13 detectó imágenes externas bloqueadas. Se restauró v12
mientras se preparaba v14 con la lista explícita. El resultado final corresponde
a `biogama-web:k8s-v14`, dos réplicas disponibles; el chat conserva k8s-v2.

## Validación

150/150 pruebas .NET aprobadas, sin advertencias ni errores de compilación.
Chrome aislado comprobó inicio, productos, IA, cursos, detalle de curso, acceso,
registro y recuperación de contraseña: cero eventos securitypolicyviolation.
Se comprobaron stock en vivo y manejadores de IA sin ejecutar el servicio IA
ni modificar datos del negocio en esa prueba pública.

La comprobación del chat usó dos cuentas temporales y dos perfiles Chrome:
conexión a pods distintos, consulta/respuesta sin recargar, recuperación del
historial y cero violaciones CSP. Se eliminaron las cuentas y sus conversaciones.
Capturas reales y resultado en `.local/evidencias-chat/20261104-csp-v14/`.

El reporte público de navegador está en `tests/quality/browser-security-check.json`.
Estos controles no equivalen a una nueva corrida de ZAP ni certifican ausencia
de vulnerabilidades. No se desactivaron reglas ni se alteró el estado de alertas.

## Evidencia posterior pendiente

Guardar la sesión y reporte anteriores de ZAP. Crear otra sesión, explorar las
mismas rutas en v14 y exportar HTML/JSON limitados a BioGama. Comparar 10055-4 y
10055-6 con el reporte anterior. Las alertas históricas de una sesión existente
no desaparecen automáticamente por actualizar la aplicación.
