# Correccion de S6964: valores omitidos en formularios

Entrada: listado del usuario con 18 advertencias S6964 de SonarAnalyzer.
La compilacion era correcta, pero los modelos admitian propiedades numericas,
fechas o booleanos omitidos con valores predeterminados sin error de binding.

Se agrego BindRequired a las propiedades senaladas. Tambien se validan los
identificadores de relaciones positivos de Record y Location y el intervalo
1900-2100 de YearCreated, que ya aparecia en el formulario de reservas.

Para los Id de entidades y formularios administrativos se distingue el caso:
- Create excluye Id de la lista de propiedades admitidas. El servidor o la
  base de datos lo generan; el usuario no tiene que enviarlo.
- Edit conserva el binding requerido del identificador y la comprobacion
  entre el identificador de la ruta y el del modelo.

Los formularios siguen usando los mismos nombres de campos. No se hicieron
cambios en tipos de columna ni migraciones. BindRequired se aplica a binding
de formularios MVC, no se presenta como validacion de cuerpos JSON.

Se agregaron 27 pruebas: diez omisiones de datos obligatorios, altitud cero
explicita aceptada, ocho comprobaciones Create/Edit de Id y ocho solicitudes
HTTP de creacion correctas sin Id, con tokens antifalsificacion reales y base
en memoria. Se usa la metadata de los parametros reales de los controladores
para comprobar sus reglas de binding. No se cargan datos en PostgreSQL.

El reporte de la suite completa queda en automated-tests.trx y coverage.xml.
El cierre de las advertencias S6964 requiere otro analisis de SonarQube; una
compilacion ordinaria fuera de la sesion del escaner no demuestra su cierre.
