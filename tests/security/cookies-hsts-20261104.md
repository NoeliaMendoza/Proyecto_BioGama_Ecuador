# Cookies HTTPS y revisión de HSTS, k8s-v15

## Resultado manual posterior a la corrección CSP

La sesión nueva del usuario en ZAP confirmó cero alertas altas y medias,
dos tipos de alertas bajas y cuatro informativos. El resumen saneado está en
`.local/evidencias-csp/20261104/zap-after-csp-summary.json`.
Es un resultado de exploración manual; las rutas y acciones alcanzadas pueden
diferir respecto de la primera sesión. No es una prueba exhaustiva de seguridad.

## Corrección de la cookie

La alerta 10011 identificó la cookie antifalsificación sin Secure.
Program.cs configura ahora cookies antifalsificación, autenticación y sesión
con CookieSecurePolicy.Always fuera de Development. El modo de desarrollo usa
SameAsRequest para conservar la ejecución local por HTTP.
No se cambian los valores de cookies ni se elimina la validación CSRF.

Tras desplegar v15 en dos réplicas, una respuesta HTTPS real de Products emitió
la cookie antifalsificación con Secure y HttpOnly. La evidencia saneada se
guarda en `.local/evidencias-csp/20261104/verification-v15.json`.

153/153 pruebas .NET aprobadas, incluida emisión real de Set-Cookie y políticas
de autenticación/sesión. Chrome comprobó ocho páginas sin violaciones CSP.
Dos cuentas temporales completaron autenticación y conversación en pods de chat
distintos, con recuperación de historial. Se eliminaron las cuentas y mensajes.
Las capturas y el resultado están en `.local/evidencias-chat/20261104-csp-v15/`.

## HSTS en localhost

La alerta 10035-1 continúa pendiente en el laboratorio. UseHsts ya está activado
fuera de Development; el framework excluye localhost y las direcciones loopback.
Las pruebas confirman encabezado HSTS para un nombre de host de producción
sintético servido por TestServer y ausencia para localhost. No se consultó un
dominio externo real ni se modificaron DNS o certificados para esa prueba.

No se fuerza HSTS para localhost: la política es por host y afecta todos sus
puertos, incluidos servicios de desarrollo HTTP como SonarQube. La solución para
un despliegue público es un nombre dedicado con HTTPS y certificado válidos.
No se cambia el estado de la alerta ni se la declara falso positivo para
eliminarla del reporte. Se documenta como limitación del entorno local.

Referencias oficiales:

- [Microsoft: cookies antifalsificación](https://learn.microsoft.com/aspnet/core/security/anti-request-forgery?view=aspnetcore-10.0)
- [Microsoft: hosts excluidos de HSTS](https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.httpspolicy.hstsoptions.excludedhosts?view=aspnetcore-10.0)
- [MDN: alcance de HSTS por host y puertos](https://developer.mozilla.org/en-US/docs/Web/HTTP/Reference/Headers/Strict-Transport-Security)

Queda pendiente repetir ZAP manualmente sobre v15 para confirmar el cierre de
10011. La comprobación de encabezados y las pruebas no sustituyen esa corrida.
