# Recuperación de datos del laboratorio

Restauración realizada el **4 de noviembre de 2026, America/Guayaquil**,
con web `k8s-v18`, chat `k8s-v2` y PostgreSQL 16. Las fechas UTC del
[registro de evidencia](restoration-evidence-20261104.json) corresponden al
5 de noviembre; el horario local todavía corresponde al día 4.

## Archivos revisados

- `database/backup.sql.tar.gz`: respaldo completo, generado con PostgreSQL 18.4.
- `database/seed-extra.sql`: 50 especies adicionales.
- `database/seed-extra-50.sql`: otras 50 especies adicionales.
- `script.sql`: estructura histórica y estados de conservación iniciales. No se
  ejecutó sobre el respaldo porque recrea tablas existentes y contiene un
  esquema anterior; no constituye otro conjunto completo de registros.

## Procedimiento y conservación

1. Se guardó un respaldo preventivo de la base activa en formato `pg_dump -Fc`.
2. Se preparó una copia del SQL compatible con PostgreSQL 16, eliminando
   únicamente `SET transaction_timeout` y los delimitadores `\restrict` y
   `\unrestrict`. No se modificó el archivo original ni los registros del dump.
3. Se restauró todo el respaldo en una base separada, dentro de una transacción
   y deteniendo la operación ante cualquier error.
4. Se copiaron las tablas actuales a esquemas de conservación para comparar e
   integrar los datos. Se mantuvieron las credenciales actuales de cuatro
   cuentas coincidentes por nombre normalizado y sus asignaciones de roles.
   Los identificadores de las cuentas recuperadas son distintos; es necesario
   cerrar sesión y volver a ingresar. La recuperación contiene siete usuarios
   distintos, no once cuentas duplicadas.
5. Los tres productos actuales y cuatro cursos ya tenían equivalentes en el
   respaldo. Se identificaron por SKU y título, respectivamente. Se conservaron
   las existencias actuales de esos tres productos y se incorporaron los cinco
   movimientos actuales con sus UUID y referencias de usuario/producto/curso
   ajustadas. Los 62 movimientos históricos también se conservaron.
6. Se incorporó el esquema de auditoría de la base actual y se verificó que el
   esquema resultante corresponde a las nueve migraciones presentes en la
   aplicación. Se preparó también el esquema del chat; no había mensajes en la
   base actual que copiar.
7. Los extras se cargaron con control de conflictos por identificador. Se
   añadieron las 100 especies. Tres registros apuntaban a la familia 70,
   inexistente: se conservó ese identificador con el nombre explícito
   **Pendiente de clasificar (datos extra)**. Esta etiqueta indica una revisión
   pendiente, no una familia taxonómica validada. La restauración no certifica
   la exactitud biológica ni las imágenes de los datos de laboratorio.
8. Se reajustaron las secuencias, se comprobaron las restricciones y se
   actualizaron las estadísticas con `ANALYZE`.
9. Una réplica temporal, fuera del Service público, verificó seis páginas por
   HTTPS. Después se pausaron brevemente web y chat y se compararon huellas
   de todas las tablas actuales contra su copia: no hubo cambios perdidos.
10. Se guardó un segundo respaldo y se intercambiaron los nombres de las bases
    de forma transaccional. La base recuperada quedó como `biogama_ecuador`;
    la anterior se conserva como `biogama_before_restore_20261105`.

## Resultado

| Datos | Registros finales |
|---|---:|
| Especies | 20.183 |
| Observaciones | 966.700 |
| Investigadores | 2.001 |
| Familias, incluida la clasificación pendiente | 73 |
| Reservas naturales | 402 |
| Ubicaciones | 10.004 |
| Publicaciones | 500 |
| Cursos | 35 |
| Productos | 48 |
| Usuarios | 7 |
| Pedidos | 9 |
| Detalles de pedido | 15 |
| Pagos históricos | 8 |
| Inscripciones | 13 |
| Movimientos de inventario | 67 |
| Correos pendientes | 0 |
| Migraciones registradas | 9 |

La web y el chat quedaron con dos réplicas disponibles cada uno. Las rutas
`/api/info`, `/`, `/Products`, `/Products/Availability`, `/Courses` y
`/Identity/Account/Login` respondieron HTTP 200 antes y después de activar la
recuperación, con verificación normal del certificado HTTPS. Las credenciales
conservadas se comprobaron comparando sus hashes y sellos de seguridad, sin
publicarlos. No se efectuaron cobros ni envíos de correo para esta recuperación.

Estas comprobaciones validan la carga y el acceso público; no sustituyen una
prueba manual de autenticación ni una nueva medición de rendimiento con este
volumen de datos. Los cursos conservan sus fechas históricas: algunos pueden
aparecer como finalizados. Los resultados anteriores de k6, SonarQube y ZAP
pertenecen a sus respectivas versiones y conjuntos de datos.

## Respaldo y reversión

Los respaldos preventivos, SQL de trabajo y comprobaciones privadas se guardan
en `.local/database-recovery/`, excluida de Git. Contienen datos de cuentas y
no deben adjuntarse al informe ni publicarse como evidencias. El JSON enlazado
arriba solo contiene conteos, códigos HTTP, versiones y la huella del archivo
de origen.

Para revertir, se deben pausar web y chat, conservar primero los cambios nuevos
de la base restaurada y volver a intercambiar los nombres de las bases desde
una conexión a `postgres`; después se reactivan las réplicas. No se eliminó la
base anterior ni se alteró la configuración de conexión de los manifiestos.
