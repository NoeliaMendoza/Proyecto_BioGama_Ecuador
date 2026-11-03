# Disponibilidad de productos en tiempo real (k8s-v4)

La funcion se integra en /Products. /Realtime redirige al catalogo.

## Flujo real

1. El administrador registra una entrada, salida o ajuste en /Admin/Inventory.
2. Se guarda en PostgreSQL. Despues del commit se publica StockChanged.
3. SignalR y Redis distribuyen el evento a las conexiones de ambas replicas.
4. Los clientes consultan /Products/Availability con sus filtros y pagina.
5. Se actualizan cantidades, total disponible, estado de stock y compra.

Tambien notifican las modificaciones administrativas de productos, reservas y
liberaciones del carrito y commits de confirmacion de pago. PostgreSQL sigue
siendo la autoridad: el cliente no puede modificar stock por el hub.

El evento lleva identificador, fecha UTC y pod emisor, sin datos del comprador.
Es una invalidacion: el cliente consulta el estado actual en vez de aplicar
deltas repetidos o fuera de orden. Se serializan las consultas del navegador.

Al reconectar, recuperar el foco o cada 30 segundos se vuelve a sincronizar.
Las notificaciones no son durables. Si Redis falla, un cambio de inventario
confirmado no se revierte. Hay un solo Redis en este laboratorio. La suma y la
lista se consultan por separado: no constituyen una instantanea atomica.

## Evidencia manual

Abrir http://127.0.0.1:30080/Products en dos navegadores. En otra ventana,
iniciar sesion como administrador y abrir http://127.0.0.1:30080/Admin/Inventory.
Registrar entrada de una unidad con producto, cantidad, referencia y notas.
Observar el cambio en los dos catalogos sin recargar. Registrar una salida de
una unidad del mismo producto para restaurar su disponibilidad.

Para un caso de venta, agregar un producto al carrito desde una sesion iniciada
y observar la disminucion de disponibilidad en otro navegador; retirarlo del
carrito para liberar la reserva. No es necesario realizar un pago.

## Verificacion automatizada

```powershell
$admin = Get-Credential -UserName admin@biogama.ec -Message 'Administrador de pruebas'
.\tests\realtime\verify.ps1 -AdminCredential $admin
```

El script autentica con antifalsificacion, conecta dos WebSockets en pods
distintos, registra una entrada real y compara evento y disponibilidad. Al
final registra una salida compensatoria. Permanecen ambos movimientos en la
auditoria. Usar un entorno sin cambios concurrentes para comparar.

Resultado: PASS; disponibilidad de 30 a 31 y regreso a 30. Evidencia en
stock-verification.txt. El catalogo responde 200 e incluye el cliente JS; el
acceso anonimo al inventario redirige al login. La actualizacion visual debe
observarse manualmente. No se ejecutaron pagos contra pasarelas externas.

Fuente:
https://learn.microsoft.com/en-us/aspnet/core/signalr/redis-backplane?view=aspnetcore-10.0
