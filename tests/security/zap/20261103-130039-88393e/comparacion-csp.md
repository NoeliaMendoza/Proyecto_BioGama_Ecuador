# Refuerzo CSP: v8 a v10

Origen: v8, ejecucion 20261103-124617-d2a5ca.
Resultado final: v10, ejecucion 20261103-130039-88393e.

Se retira unsafe-inline de script-src y se agrega script-src-attr none.
Cada respuesta genera un nonce aleatorio de 32 bytes. Una TagHelper propia
lo aplica a los scripts confiables de Razor y lo pasa al SDK de PayPal con
data-csp-nonce. Las respuestas HTML usan no-store para evitar mezclar un
documento almacenado con una politica que contenga otro nonce.

Los eventos onclick/onsubmit se sustituyen por listeners de ui-actions.js.
Se conservan cantidad del carrito, confirmaciones, impresion, navegacion a
compras y acciones de IA. El selector IA utiliza delegacion de eventos y
escapa nombres/codigos antes de construir su HTML. El CSS global importa
ahora la fuente local en lugar de Google Fonts.

| Resultado ZAP JSON | v8 | v10 |
|---|---:|---:|
| Alertas medias individuales | 3 | 2 |
| Alertas bajas | 0 | 0 |
| URLs del resumen baseline | 65 | 66 |

Desaparece 10055-5 (script-src unsafe-inline). Permanecen 10055-4 (img-src
admite imagenes HTTPS de cualquier dominio) y 10055-6 (estilos inline).
No se eliminaron reglas ni se ignoraron alertas. La consola agrupa ambas
variantes bajo una regla 10055; no confundir WARN-NEW 1 con una sola variante.
Las observaciones informativas se deben revisar aparte de los riesgos.

Verificacion:
- 110 pruebas C# aprobadas, incluyendo correspondencia nonce/cabecera,
  valor distinto en cada respuesta, scripts Razor autorizados y atributo
  de compatibilidad del SDK PayPal.
- 15 escenarios JavaScript aprobados; 100 % de lineas y ramas de los tres
  archivos propios incluidos en LCOV, no de toda la aplicacion.
- Chrome headless real, perfil nuevo: /Products y /Ia sin violaciones CSP,
  disponibilidad en vivo conectada, selector de especies probado con texto
  malicioso sintetico y boton IA enlazado a su manejador. Este ultimo se
  sustituye temporalmente para no llamar al servicio IA; los datos sinteticos
  solo existen en el navegador. browser-check.json conserva los resultados.
- Dos replicas v10 disponibles. No se ejecutaron migraciones ni pagos reales.

La primera comprobacion en Chrome sobre v9 encontro un @import antiguo a
Google Fonts bloqueado. Se corrigio en v10; se conserva la evidencia inicial
en la carpeta 20261103-125557-44c101/browser-check-inicial.json.

Limites: las pruebas de nonce del SDK no equivalen a un pago Sandbox completo.
ZAP sigue siendo anonimo y pasivo; revisar sus insights de fallos de red al
interpretar el alcance. Un resultado favorable no constituye auditoria total.
Los cambios todavia deben publicarse en el siguiente analisis SonarQube.

Referencias primarias:
https://developer.mozilla.org/en-US/docs/Web/HTTP/Guides/CSP
https://developer.paypal.com/sdk/js/configuration/
