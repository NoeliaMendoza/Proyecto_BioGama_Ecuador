# Mejora de calidad: llamadas asincronas

## Antes

Captura del primer analisis proporcionada por el usuario:
- Quality Gate: Passed.
- Problemas abiertos: 219.
- Seguridad: 53; fiabilidad: 108; mantenibilidad: 66.
- Duplicacion: 8.7%; cobertura reportada: 0.0%.

Las categorias pueden solaparse y no deben sumarse como problemas unicos.
El Quality Gate aprobado no significa ausencia de problemas.

El reporte local .sonarqube/out/0/Issues.json identifica cuatro casos de S6966.
Se guardaron sus ubicaciones y mensajes antes de editar en s6966-before.json.

## Cambio

| Archivo | Antes | Despues |
| --- | --- | --- |
| Program.cs (dos casos) | app.Run() | await app.RunAsync() |
| Controllers/FamiliesController.cs | Families.Any(...) | await Families.AnyAsync(...) |
| Areas/Identity/Pages/Account/Manage/_Layout.cshtml | RenderSection(...) | await RenderSectionAsync(...) |

Se usan las alternativas asincronas recomendadas por el analizador. La consulta
de existencia en la ruta de concurrencia deja de ser una consulta sincrona.
No se ha medido una mejora de latencia o throughput como consecuencia del cambio.

## Verificacion

Resultado de compilacion en build-after.txt. Para confirmar el cierre de los
hallazgos se debe ejecutar un segundo analisis SonarScanner con el mismo project
key biogama-ecuador, alcance y perfil de calidad, y esperar su procesamiento.
No se modifican exclusiones del analisis para aparentar una reduccion de problemas.
La cobertura no se obtiene de las pruebas k6.

Conservar captura del detalle de S6966 antes y despues, y del dashboard final.
No afirmar que el total bajo de 219 a 215 hasta observar el segundo resultado.

## Segunda tanda de mejoras

| Problema | Correccion |
| --- | --- |
| S2068: clave inicial dentro del codigo | Claves de las cuentas iniciales desde configuracion, sin valores predeterminados; se omite la cuenta si falta su clave |
| Dos S4502: antifalsificacion desactivada en PayPal | ValidateAntiForgeryToken en CreatePayPalOrder y CapturePayPalOrder; la vista emite el token y lo envia en el header |
| S6444: regex sin timeout | Timeout de 100 ms, limite de longitud y rechazo controlado si se agota el tiempo |
| Generacion de codigos con Random.Shared | RandomNumberGenerator.GetInt32 para cada caracter |
| Error de pago insertado mediante innerHTML | textContent para mostrar mensajes sin interpretarlos como HTML |
| Resultados de Identity ignorados | Comprobacion explicita de IdentityResult al crear cuentas y roles |

Las cuentas existentes no se modifican. README.md describe la configuracion
necesaria para crear usuarios iniciales en una base nueva. Esto no rota claves
que ya estuvieran guardadas en la base de datos.

La prueba verify-security.ps1 verifica HTTP 400 en ambos endpoints sin token,
y que con token valido se llega a la validacion de una orden inexistente.
No crea ordenes, captura pagos ni llama a proveedores externos:

```powershell
$admin = Get-Credential -UserName admin@biogama.ec -Message 'Administrador de pruebas'
.\tests\quality\verify-security.ps1 -AdminCredential $admin
```

Reanalizar en SonarQube para confirmar los hallazgos cerrados. El cambio de un
Security Hotspot no autoriza a marcarlo Safe sin revisar su comportamiento.
La compra completa en PayPal Sandbox y la inicializacion de una base nueva
requieren sus propias pruebas; no quedan demostradas por este test HTTP.

Referencia CSRF:
https://learn.microsoft.com/en-us/aspnet/core/security/anti-request-forgery?view=aspnetcore-10.0

## Dependencias vulnerables

Se fijaron dependencias transitivas a versiones corregidas:
- NuGet.Packaging: 6.12.1 -> 6.12.5.
- NuGet.Protocol: 6.12.1 -> 6.12.5.
- System.Security.Cryptography.Pkcs: 10.0.0 -> 10.0.14.

Packaging y Protocol llegaban desde Web.CodeGeneration.Design; Pkcs tambien
llegaba desde MimeKit. Se conserva el scaffolding y no se suprimen alertas NU190x.
nuget-before.json registra tres paquetes con cuatro alertas; nuget-after.json
no registra paquetes vulnerables en la auditoria que incluye transitivas.
build-dependencies-after.txt confirma 0 errores y 17 advertencias de codigo
restantes (diferentes de NU190x). La imagen k8s-v6 incorpora estas dependencias.

Fuentes de las versiones corregidas:
https://github.com/advisories/GHSA-g4vj-cjjj-v7hg
https://github.com/advisories/GHSA-2fmv-rv2c-59x7
https://github.com/advisories/GHSA-846f-f764-76mx

## Tercera tanda y conservacion de evidencia

Se corrigen las 17 advertencias de compilacion restantes: inicializacion y
nulabilidad coherentes en confirmacion de registro y auditoria; proteccion ante
pagos y detalles ausentes; tipo nullable correcto en el proveedor de cultura;
y uso de ToTable(..., table => table.HasCheckConstraint(...)) en EF Core.
Las diez restricciones conservan nombre y SQL; no se cambia el esquema.
En Checkout se agregan seis etiquetas asociadas con sus controles; tres textos
de agrupacion dejan de usar etiquetas label sin un control asociado.

build-third-batch.txt registra una compilacion completa con 0 errores y
0 advertencias. Esto no equivale a cero hallazgos SonarQube: el siguiente
analisis debe medir el resultado. El Quality Gate de la captura con 210
problemas permanece pendiente de diagnostico; no se alteraron sus condiciones.

scripts/Analyze-And-Save.ps1 ejecuta begin/build/end con el mismo token oculto,
espera el procesamiento y exporta datos. scripts/Export-Evidence.ps1 puede
guardar solo las evidencias locales o consultar el proyecto por API.
Los tokens no se guardan. Las capturas reales deben incorporarse manualmente.
