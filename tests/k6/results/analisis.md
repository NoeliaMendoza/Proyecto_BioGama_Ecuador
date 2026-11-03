# Primera medicion de rendimiento

Fecha local: 2 de noviembre de 2026, aproximadamente 21:59-22:01
(America/Guayaquil). k6 v2.2.0 en Windows, destino localhost:30080.
BioGama k8s-v2 con 2 replicas; PostgreSQL con 1 replica.
Todos los componentes y el generador de carga comparten el mismo equipo.
La base contiene 3 productos y 0 especies. Sin degradacion de red inducida
durante estas pruebas. No se simularon fallos deliberados.

Tabla 1. Resultados de las cargas constantes (100 VUs, 30 s, pausa 0.1 s).

| Ruta | Solicitudes | Solicitudes/s | Promedio (ms) | p(95) (ms) | Errores HTTP |
| --- | ---: | ---: | ---: | ---: | ---: |
| /api/info | 28172 | 935.89 | 5.59 | 13.29 | 0% |
| /Products | 5995 | 198.43 | 401.12 | 717.93 | 1/5995 (aprox. 0.0167%) |

La Tabla 1 muestra mayor latencia y menor tasa de solicitudes completadas en
/Products, que consulta PostgreSQL y genera HTML. /api/info solo devuelve
metadatos, por lo que no representa el costo de una operacion de negocio.
Estas diferencias no permiten atribuir el cuello de botella exclusivamente
a PostgreSQL: tambien intervienen renderizado, CPU, registros y recursos compartidos.

El objetivo de clase p(95)<250 ms se cumple en /api/info y se incumple en
/Products. Ambas rutas cumplen errores HTTP <1%, pero /Products incumple
la comprobacion adicional que exige HTTP 200 en todas las solicitudes.
k6 termino correctamente con codigo 0 en smoke y baseline-info, y con codigo
1 en baseline-products por los umbrales incumplidos. Esto es un resultado
de rendimiento, no un fallo de instalacion de k6.

La prueba smoke obtuvo 10 respuestas validas, sin errores HTTP, con p(95)
de 9.67 ms. Las metricas originales se conservan en los tres archivos JSON.

Despues de la carga, ambos pods web y PostgreSQL seguian Running, sin reinicios.
La revision limitada a las ultimas 200 lineas por pod no encontro una excepcion
que explique la respuesta fallida. No se capturo su codigo exacto durante la
prueba; la causa queda sin determinar. En una prueba de diagnostico posterior
conviene registrar el estado de las respuestas fallidas.

Una ejecucion por escenario no demuestra capacidad maxima ni resultados
estables. Para comparar cambios, mantener el mismo conjunto de datos,
recursos, pausas y numero de usuarios, y realizar repeticiones controladas.
Estas pruebas no evaluan aun tiempo real, resiliencia ni seguridad.
