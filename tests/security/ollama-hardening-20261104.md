# IA operativa y errores controlados: k8s-v16

## Problema observado

La exploración manual amplió el alcance de ZAP hasta `POST /api/ia/generar`.
Ollama no estaba instalado ni ejecutándose; además, localhost dentro de un pod
apunta al propio pod. La respuesta 500 incluía la dirección del proveedor y el
mensaje de conexión rechazada. ZAP lo identificó como divulgación de errores.

En esa sesión, las alertas de BioGama fueron HSTS y divulgación del error de IA,
más cuatro informativas. Los otros avisos globales del árbol de ZAP deben
atribuirse a sus propios sitios. La alerta de cookie Secure ya no aparece en el
resumen filtrado: `.local/evidencias-csp/20261104/zap-after-cookies-summary.json`.

## Implementación

- Servicio Ollama dentro de Kubernetes, una réplica, ClusterIP 11434 sin NodePort.
- Imagen oficial 0.40.14 fijada también por digest en `k8s/06-ollama.yaml`.
- Modelo gemma2:2b, identificador observado 8ccf136fdd52, aproximadamente 1,6 GB.
- PVC ollama-models de 5 GiB: el modelo no depende del sistema de archivos efímero.
- Contenedor UID/GID 1000, sin escalamiento ni capacidades, seccomp RuntimeDefault
  y raíz de solo lectura. Directorio temporal y modelos usan volúmenes separados.
- Límite de cuatro CPU y 3 GiB; una generación paralela y un modelo cargado.
- Readiness comprueba que el modelo esté disponible; liveness comprueba el puerto.
- La web usa `Ollama:BaseUrl=http://ollama:11434`, modelo gemma2:2b y timeout 180 s.
  El servicio interno utiliza HTTP; HTTPS se mantiene en la entrada de BioGama.
  Se declara NetworkPolicy de ingreso desde las réplicas web. Su aplicación
  efectiva depende del CNI; no se afirma una verificación completa de aislamiento.

Los límites de contexto y salida se envían dentro de options, como establece la
API del proveedor: contexto 1024, salida máxima 256 tokens, cuatro hilos y dos
minutos de permanencia en memoria tras la solicitud.

Si falla el proveedor, el navegador recibe 503, Retry-After y un mensaje general.
Las excepciones y estados del proveedor se registran en el servidor; no se
devuelven su dirección, mensajes internos ni cuerpos de error. Se conservan
cancelación de solicitudes y estados HTTP de fallo. Kestrel deja de emitir Server.

## Validación real

160/160 pruebas .NET aprobadas; incluyen fallo de transporte, error HTTP del
proveedor, JSON inválido, respuesta incompleta, cancelación y respuesta correcta.
La prueba del controlador introduce deliberadamente detalles privados en un
proveedor simulado y comprueba que no lleguen a la respuesta pública.

Chrome realizó una consulta libre desde el botón de la interfaz y obtuvo una
respuesta en español, sin violaciones CSP. La primera generación observada tardó
aproximadamente 27 s. Una petición posterior a la API tardó 3,49 s y otra desde
la interfaz 6,19 s. Son observaciones individuales de calentamiento, no p95 ni
una prueba de capacidad. El modelo utilizó CPU y alrededor de 1,8 GB según ollama ps.

Evidencias locales:

- `.local/evidencias-ia/20261104-v16/01-ia-respuesta-real.png`: pregunta y respuesta
  reales en modo Libre.
- `.local/evidencias-ia/20261104-v16/generation-api.json`: respuesta real por HTTPS.
- `tests/quality/browser-security-check.json`: comprobación de ocho páginas y
  generación real, activada explícitamente para esta corrida.
- `tests/quality/coverage-runtime.json`: resultado de las pruebas .NET.

Las respuestas del modelo son asistencia generativa; esta prueba no valida su
exactitud científica. Queda pendiente una nueva exploración manual de ZAP sobre
v16 y otro análisis SonarQube para medir los cambios de esta versión.

## Reproducción

```powershell
kubectl apply -f k8s/06-ollama.yaml
kubectl wait '--for=jsonpath={.status.phase}=Running' pod -l app=ollama -n biogama --timeout=180s
kubectl exec deployment/ollama -n biogama -- /bin/ollama pull gemma2:2b
kubectl rollout status deployment/ollama -n biogama
docker build -t biogama-web:k8s-v16 .
kubectl apply -f k8s/02-web.yaml
kubectl rollout status deployment/biogama-web -n biogama
```

Referencias: [imagen oficial](https://hub.docker.com/r/ollama/ollama),
[modelo](https://ollama.com/library/gemma2),
[API de generación](https://github.com/ollama/ollama/blob/main/docs/api.md).
