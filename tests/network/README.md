# Laboratorio de red

Preparar con kubectl apply -f k8s/04-network-lab.yaml.
Los pods son independientes de BioGama. Ambos corren en docker-desktop:
no representan una red entre maquinas fisicas distintas. CPU, virtualizacion
y los limites de los contenedores tambien influyen en el throughput medido.

## Primera evidencia: condiciones normales

```powershell
kubectl get pods -n biogama -o wide
kubectl exec -n biogama network-client -- tc qdisc show dev eth0
kubectl exec -n biogama network-client -- iperf3 -c iperf-server -t 10 | Tee-Object tests/network/iperf-normal.txt
kubectl exec -n biogama network-client -- iperf3 -c iperf-server -u -b 10M -t 10 | Tee-Object tests/network/udp-normal.txt
```

TCP mide throughput y retransmisiones. UDP con oferta de 10 Mbit/s permite
observar jitter y perdida reportada por el receptor. Ninguna de estas pruebas
determina por si sola la latencia HTTP ni el tiempo de actualizacion de stock.
Para cuantificar esos efectos, comparar tambien solicitudes a la aplicacion
desde network-client bajo condiciones normales y degradadas.

NET_ADMIN se concede solamente al pod network-client para ensayos tc/netem.
Las reglas en su interfaz eth0 afectaran el trafico que sale de ese pod,
incluidas sus consultas a BioGama, pero no el de los navegadores en Windows.
No se aplican reglas al host ni a los pods de la aplicacion.

Fuente de herramientas: https://github.com/nicolaka/netshoot
