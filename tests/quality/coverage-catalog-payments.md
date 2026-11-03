# Ampliacion posterior a new_coverage=67.8

Referencia: evidencias/20261103-110731-05d23c/sonarqube/quality-gate.json.
El unico incumplimiento reportado fue cobertura nueva 67.8 frente a 80.

Se agregaron 12 escenarios; la suite C# completa paso con 65 pruebas,
ninguna fallida ni omitida. No hubo cambios en el codigo de produccion.

- Seis pruebas HTTP renderizan el catalogo Razor real: stock disponible,
  stock bajo, stock agotado, productos inactivos/eliminados y limites de
  paginacion. Verifican unidades descontando reservas, controles de compra,
  token antifalsificacion y concordancia con el endpoint de disponibilidad.
- Cuatro respuestas del proveedor malformadas o incompletas mantienen pagos
  y pedidos pendientes, sin movimientos de inventario (respuesta HTTP 503).
- Dos pruebas validan aprobacion y rechazo verificados, ignorando el estado
  contradictorio remitido por el webhook. Repetir la notificacion mantiene
  un solo movimiento y el stock esperado.

Los servicios de pagos se simulan; no se realizaron cargos ni conexiones a
PayPhone. Para las dos pruebas de transicion se permite explicitamente la
simulacion de transacciones de EF InMemory. Estas pruebas verifican estados
e idempotencia secuencial, no rollback, concurrencia ni atomicidad PostgreSQL.
Los otros escenarios no habilitan esa opcion.

El nuevo porcentaje de SonarQube queda pendiente de publicar y procesar el
analisis. No se modificaron umbrales ni exclusiones de codigo de negocio.
Los resultados reproducibles quedan en automated-tests.trx y coverage.xml.
