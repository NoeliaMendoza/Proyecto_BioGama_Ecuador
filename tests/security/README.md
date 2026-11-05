# Seguridad con OWASP ZAP

Desde PowerShell en la raiz del proyecto, con Docker y Kubernetes disponibles:

```powershell
docker pull ghcr.io/zaproxy/zaproxy:stable
.\scripts\Invoke-ZapBaseline.ps1
```

El script usa la imagen oficial y comparte la red de un pod web de Docker
Desktop. Para la version HTTPS actual accede a https://localhost:8080 dentro
de esa red. Valida previamente el certificado con la copia publica
`.local/tls/certificate.pem`, sin omitir su verificacion, y comprueba que
el catalogo devuelve HTTP 200 y HTML. Este metodo evita depender del DNS
host.docker.internal, que no resuelve en este entorno. Analiza una replica;
no es una prueba de balanceo del NodePort ni del acceso externo.
La imagen desplegada actualmente puede ser anterior a las ultimas
refactorizaciones locales: los metadatos identifican exactamente que se evalua.
La comprobacion previa de TLS no sustituye una auditoria de configuracion TLS.
Genera HTML, JSON, XML, Markdown, registro
de ejecucion y metadatos en zap/<fecha-identificador>. Export-Evidence.ps1 copia
esas ejecuciones a seguridad dentro de una carpeta de evidencias con manifiesto.

Alcance: navegacion tradicional anonima y reglas pasivas, sin escaneo activo
ni envio de formularios. No demuestra seguridad de operaciones autenticadas
ni de pagos. Las alertas son hallazgos que necesitan validacion e interpretacion.
El codigo de salida 2 significa alertas WARN, no un error de infraestructura.
No se deshabilitan reglas para obtener un resultado favorable.

Los metadatos identifican la imagen realmente desplegada. Un analisis sobre
k8s-v7 no verifica automaticamente cambios locales posteriores: reconstruir y
desplegar una imagen identificable es necesario antes de comparar correcciones.

En el informe, registrar por alerta: nombre, riesgo, confianza, URL afectada,
evidencia y correccion recomendada. La ausencia de alertas de una categoria
solo describe las paginas alcanzadas por este analisis.

Documentacion oficial:
https://www.zaproxy.org/docs/docker/baseline-scan/
https://www.zaproxy.org/docs/desktop/addons/spider/automation/

## Comprobacion adicional en navegador real

Con la aplicacion disponible en el puerto 30080, Node.js 24 y Chrome instalado
en su ruta normal de Windows:

```powershell
node tests/browser/security-browser-check.test.cjs
```

Abre Chrome headless con un perfil nuevo en .local, comprueba CSP en /Products
y /Ia, stock en vivo, selector de especies con datos sinteticos solo en el
navegador y conexion del boton IA con su manejador. Sustituye ese manejador
durante la comprobacion para no invocar IA. No inicia sesion, no compra ni
escribe datos del negocio. Guarda browser-security-check.json en tests/quality.
Esta comprobacion es adicional a la suite de 15 escenarios jsdom, y no se
ejecuta automaticamente durante el analisis SonarQube.
