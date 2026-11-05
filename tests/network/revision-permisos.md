# Revisión del permiso de red del laboratorio

El hallazgo `kubernetes:S5849` señala `NET_ADMIN` en
`k8s/04-network-lab.yaml`. El permiso es real: permite modificar la interfaz
de red del contenedor y no debe concederse a los servicios de la aplicación.

Se conserva únicamente en el cliente temporal de pruebas para ejecutar
`tc qdisc ... netem`. No se utiliza modo privilegiado, red del host ni montajes
del host. Se eliminan las capacidades predeterminadas, se impide escalar
privilegios, se usa seccomp RuntimeDefault y no se monta el token de Kubernetes.
La web, el chat y sus datos no necesitan esta capacidad.

Una vez concluidas las mediciones, retirar solo los recursos de laboratorio:

```powershell
kubectl delete -f k8s/04-network-lab.yaml --ignore-not-found
```

Esta revisión describe la necesidad y las mitigaciones; no demuestra ausencia
de riesgo y no cierra el hallazgo de SonarQube. Su aceptación requiere una
decisión explícita y documentada del responsable. No se deben desactivar reglas,
excluir este manifiesto ni marcarlo como falso positivo para obtener cero.

Los resultados anteriores de red se conservan como evidencia histórica. Si se
repiten con otra configuración o versión, guardar una nueva corrida con fecha.
