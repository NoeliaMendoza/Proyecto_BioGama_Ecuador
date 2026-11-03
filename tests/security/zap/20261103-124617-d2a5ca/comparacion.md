# Comparacion ZAP antes/despues: v7 y v8

Antes: 20261103-123625-cda8f1, imagen biogama-web:k8s-v7.
Despues: 20261103-124617-d2a5ca, imagen biogama-web:k8s-v8.
Misma imagen ZAP, spider tradicional de un minuto, sin envio de formularios,
sin autenticacion ni ataques activos. Red de una replica Docker Desktop.

| Medida | Antes | Despues |
|---|---:|---:|
| URLs registradas por baseline | 53 | 65 |
| Endpoints en insights JSON | 32 | 38 |
| Alertas de riesgo medio (entradas JSON, sin informativas) | 3 | 3 |
| Alertas de riesgo bajo | 2 | 0 |
| Observaciones informativas JSON | 1 | 2 |
| Fallos de red segun insights | 31 % | 31 % |

Las cinco alertas iniciales ya no aparecen en el segundo reporte:
- 10020: se agrega X-Frame-Options SAMEORIGIN y frame-ancestors self.
- 10021: se agrega X-Content-Type-Options nosniff.
- 10038: existe Content-Security-Policy, con base-uri y object-src restringidos.
- 10017 y 90003: Chart.js, Marked, iconos y fuente se sirven localmente con
  versiones identificadas; no se depende de esos CDN en las paginas observadas.

No se declara que todos los riesgos medios esten resueltos. La nueva politica
genera tres alertas medias diferentes que requieren trabajo posterior:

| Referencia | Alerta | Motivo y trabajo pendiente |
|---|---|---|
| 10055-4 | CSP: Wildcard Directive | img-src admite cualquier origen HTTPS; inventariar los dominios reales de imagenes y restringirlos. |
| 10055-5 | CSP: script-src unsafe-inline | Mantiene compatibilidad con scripts y manejadores actuales; migrar a archivos y nonces, verificando carrito, IA y pagos. |
| 10055-6 | CSP: style-src unsafe-inline | Existen estilos inline; migrarlos a clases o nonces para retirar la excepcion. |

El resumen de consola agrupa esas tres variantes bajo la regla 10055 y muestra
WARN-NEW: 1. El JSON las distingue: no se debe afirmar que queda una sola
alerta individual. La observacion de comentarios sospechosos corresponde al
aviso de compilacion de Marked en su libreria publicada; no evidencia una
filtracion de credenciales. La otra observacion identifica gestion de sesion.

Tambien se agrego sanitizacion DOMPurify al Markdown devuelto por IA, con
pruebas que conservan formato y eliminan scripts, atributos de evento y URLs
javascript. Esta mejora se verifica mediante pruebas y no se atribuye a una
alerta que el baseline no habia detectado.

Validacion: 106 pruebas C# y 12 pruebas JavaScript aprobadas. LCOV mide 100 %
de los dos archivos propios probados: product-stock.js y markdown-renderer.js,
no toda la aplicacion ni las bibliotecas externas. Se conservaron licencias,
versiones e integridades npm en wwwroot/lib/security-dependencies.json.

Se desplegaron dos replicas v8 sin ejecutar migraciones ni inicializacion de
datos. verificacion-despliegue.json comprueba HTTP y negociacion de SignalR,
sin guardar su token. No es una prueba de navegador ni un pago real PayPal.

Limitaciones: exploracion anonima, fallos de red todavia presentes, sin prueba
de HTTPS publico, flujo de pagos autenticado ni auditoria completa. Las URLs
descubiertas cambiaron al alojar mas recursos localmente: no son una muestra
identica de peticiones. La comparacion se apoya en las cabeceras y referencias
de recursos comprobadas, no solo en la variacion de contadores.

Las nuevas bibliotecas de terceros y los reportes generados se excluyen del
analisis de fuentes propias de SonarQube, con rutas concretas documentadas en
Analyze-And-Save.ps1. No se excluyen middleware, controladores ni JS propio.
SonarQube debe analizar otra vez estos cambios; no se declara su Gate actual.

Referencias:
https://www.zaproxy.org/docs/docker/baseline-scan/
https://developer.mozilla.org/en-US/docs/Web/HTTP/Reference/Headers/Content-Security-Policy
https://developer.mozilla.org/en-US/docs/Web/Security/Defenses/Subresource_Integrity
