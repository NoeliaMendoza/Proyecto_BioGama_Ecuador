# Correcciones prioritarias y pruebas

## Seguridad y datos

- datos.py obtiene la conexion de PGHOST, PGPORT, PGDATABASE, PGUSER y PGPASSWORD.
  La clave es obligatoria; no hay una clave predeterminada en el archivo. Eliminar
  la literal no rota una clave que hubiera sido usada anteriormente.
- Los formularios administrativos exigen la presencia de cantidades, precios,
  fechas y stock requeridos mediante BindRequired. En especial, omitir NewStock
  deja ModelState invalido en vez de interpretar la omision como ajuste a cero.
- InventoryMovementService rechaza cantidades no positivas, stock negativo y
  productos vacios antes de modificar existencias. Conserva las comprobaciones
  de reservas y disponibilidad. Una transferencia requiere sucursales distintas.
- Los cuerpos JSON de PayPal se validan antes de consultar pagos o proveedores;
  el identificador de orden es obligatorio y no admite Guid.Empty.
- MarkAsPaid requiere token antifalsificacion ademas del rol administrador.
- CreateLink, Details y ConfirmPayPal comprueban propiedad de la orden o rol
  administrador. No se captura un pago de otro usuario antes de comprobar acceso.
- El webhook PayPhone no confia en status ni amount de la notificacion recibida.
  Consulta con la credencial del comercio el endpoint HTTPS documentado de
  consulta por clientTransactionId; contrasta ID, importe en centavos y moneda.
  Solo los estados terminales verificados producen cambios. Un estado pendiente,
  una respuesta inconsistente o un error del proveedor no aprueban el pago.
- PostgreSQL no monta el token de ServiceAccount. PostgreSQL y web declaran
  solicitudes y limites de almacenamiento efimero, separados de los PVC.

La consulta de verificacion PayPhone sigue la documentacion oficial de API Sale:
https://docs.payphone.app/api-sale
La comprobacion contra una cuenta real y la compatibilidad del endpoint de
consulta con la configuracion API Link del comercio quedan pendientes de una
prueba Sandbox. Si el proveedor no ofrece una respuesta verificable, se devuelve
503 y no se modifica el pago; no se acepta la notificacion como prueba de cobro.

## Pruebas ejecutadas

35 pruebas C# aprobadas, 0 fallidas y 0 omitidas. Se usa WebApplicationFactory
con un almacen EF InMemory independiente por caso y respuestas HTTP del proveedor
simuladas localmente. No se usa la base Kubernetes ni se realizan pagos externos.

Los casos incluyen cantidades invalidas, reservas y liberaciones, confirmaciones,
stock insuficiente, ajustes incompletos, datos JSON omitidos, CSRF, autorizacion,
acceso a pagos de otro usuario y webhooks falsos, inconsistentes o no verificables.
Se comprueba que esas peticiones no cambian pagos ni inventario.

El emisor de tokens y la autenticacion de prueba solo existen en el proyecto de
tests. El proyecto web excluye su codigo y contenido del ensamblado y publicacion.
El almacen InMemory no demuestra aislamiento transaccional ni bloqueos de
PostgreSQL: esas propiedades requieren sus propias pruebas con el proveedor real.

Se valido tambien la sintaxis de datos.py y load_db_config en Python 3.12, en un
contenedor temporal sin red. Se extrajo solo la funcion de configuracion mediante
AST; no se importaron dependencias del generador ni se ejecuto una carga de datos.

## Cobertura y evidencia

- automated-tests.txt: salida de la suite.
- automated-tests.trx: resultados estructurados.
- coverage.xml: cobertura dinamica de Microsoft del ensamblado de aplicacion.
- coverage.opencover.xml: evidencia de la ejecucion anterior con Coverlet.
- nuget-tests-after.json: auditoria de dependencias de las pruebas.
- data-before-deploy.txt y data-after-deploy.txt: comparacion de conteos.

La cobertura global del ensamblado incluye codigo generado y no es
equivalente al porcentaje sobre codigo nuevo que calcula SonarQube. No se afirma
que el Gate ya pase. Analyze-And-Save.ps1 ahora ejecuta build y test antes de
end, y configura sonar.cs.vscoveragexml.reportsPaths. No se excluye codigo de negocio
del reporte para incrementar el porcentaje.

Ejecutar las pruebas:

```powershell
dotnet test tests/BioGamaEcuador.Tests/BioGamaEcuador.Tests.csproj
```

Analizar y guardar evidencias con cobertura:

```powershell
.\scripts\Analyze-And-Save.ps1
```

El usuario introduce un token vigente de forma oculta. El token de usuario con
permisos de lectura y analisis permite tambien descargar el resultado por API.
El siguiente analisis determinara el numero de hallazgos y la cobertura nuevos.
