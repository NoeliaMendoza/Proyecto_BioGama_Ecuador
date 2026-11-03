# Analisis inicial de seguridad con ZAP

Fecha: 3 de noviembre de 2026, America/Guayaquil.
Herramienta: ZAP 2.17.0, imagen oficial cuyo identificador esta en metadatos.json.
Aplicacion: imagen Kubernetes biogama-web:k8s-v7, una replica de BioGama.
Destino interno: http://127.0.0.1:8080 compartiendo la red del pod en Docker
Desktop. No equivale a analizar un dominio HTTPS publico ni el balanceador.

El baseline registra 53 URLs y 32 endpoints en los insights del reporte JSON.
Se uso spider tradicional limitado a un minuto y reglas pasivas; formularios
deshabilitados, sin autenticacion ni escaneo activo. Codigo de salida 2: WARN.

| ID | Hallazgo | Riesgo | Ejemplo afectado | Correccion propuesta |
|---|---|---|---|---|
| 10038 | Falta Content-Security-Policy | Medio | /, /Products, /Courses | Definir una politica compatible con scripts, estilos, SignalR y proveedores usados; probar sus restricciones. |
| 10020 | Falta proteccion contra clickjacking | Medio | /, /Courses, /Public/Catalogo | Definir frame-ancestors y/o X-Frame-Options segun el uso de marcos. |
| 90003 | Recursos externos sin Subresource Integrity | Medio | / y /Products | Revisar los recursos exactos; usar versiones fijas con integrity/crossorigin cuando proceda o servirlos localmente. |
| 10017 | JavaScript incluido desde otro dominio | Bajo | / e /Ia | Revisar los dominios y necesidad de cada dependencia; restringirlos o servir las dependencias localmente. |
| 10021 | Falta X-Content-Type-Options | Bajo | /Products y /Courses | Agregar nosniff y comprobar los tipos MIME de los recursos. |

Son cinco tipos de alerta (tres medios y dos bajos), no cinco URLs ni cinco
vulnerabilidades explotadas. Adicionalmente el JSON registra la observacion
informativa 10112, identificacion de gestion de sesion, que no demuestra por
si misma una vulnerabilidad. La confianza figura por separado del riesgo.
Los reportes guardan ejemplos limitados; el registro muestra ocurrencias
adicionales. No sumar los ejemplos como si fueran el total de peticiones.

No se detectaron alertas altas en este alcance. Eso no demuestra ausencia
de vulnerabilidades altas en la aplicacion. El reporte registra 31 % de
fallos de red; por tanto la exploracion tiene limitaciones y no debe
presentarse como una auditoria completa. Revisar las solicitudes fallidas
antes de una evaluacion mas amplia, incluidas dependencias externas.

La ejecucion anterior 20261103-123408-693774 fallo al resolver
host.docker.internal y solo registro dos URLs. Sus cero alertas no son
evidencia de seguridad: esta marcada como no concluyente en sus metadatos.

Este analisis evalua v7, no los cambios locales posteriores. Para comparar
antes/despues de una correccion, desplegar una nueva imagen identificada y
repetir con las mismas opciones. Los informes generados se excluyen del
analisis de fuentes de SonarQube para no tratarlos como codigo del producto.

Referencias:
https://www.zaproxy.org/docs/docker/baseline-scan/
https://www.zaproxy.org/docs/desktop/addons/spider/automation/
