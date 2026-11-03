# Comparacion de condiciones de red

Tabla 1. Resultados obtenidos en los pods del laboratorio.

| Indicador | Normal | Degradado (100 ms de salida y 2% de perdida configurada) |
| --- | ---: | ---: |
| Throughput TCP del receptor | 25.4 Gbit/s | 2.70 Mbit/s |
| Throughput UDP del receptor (oferta 10 Mbit/s) | 10.0 Mbit/s | 9.72 Mbit/s |
| Perdida UDP del receptor | 0/8634 (0%) | 153/8633 (1.8% redondeado) |
| Jitter UDP del receptor | 0.056 ms | 0.194 ms |
| HTTP /Products promedio | 10.79 ms | 364.60 ms |
| HTTP /Products maximo | 15.32 ms | 424.15 ms |
| Respuestas HTTP 200 | 10/10 | 10/10 |

En la Tabla 1, la respuesta HTTP promedio aumenta aproximadamente 33.8 veces.
El servicio responde en las diez muestras de cada condicion, pero conservar
disponibilidad no implica conservar rendimiento. UDP registra una perdida
cercana al 2% configurado. El jitter expresa variacion, no latencia absoluta:
no es esperable que sea igual a los 100 ms de retraso constante introducido.

Se comparan las filas receiver de iperf3 para reflejar los datos recibidos.
El TCP normal registra 8354 retransmisiones y el degradado 46, pero sus
volumenes transferidos son muy diferentes: no deben compararse esos conteos
aislados como si representaran tasas de perdida equivalentes.

Ambos pods y BioGama comparten el nodo docker-desktop. Los 25.4 Gbit/s no
representan capacidad de Internet ni de una red fisica entre equipos.
Solo se recogieron diez solicitudes HTTP por condicion. No equivalen a una
prueba k6 de cien usuarios ni permiten afirmar disponibilidad absoluta.

La degradacion se aplico en la salida de network-client; no afecto directamente
al navegador de Windows. Por ello no se midio aqui el retraso visual del stock
por WebSocket. La salida final noqueue confirma que se retiro netem.
