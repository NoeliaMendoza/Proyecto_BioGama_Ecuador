# Accesibilidad de filtros, carrito y confirmacion

Base: exportacion 20261103-121404-4b5b12; Gate aprobado, 167 hallazgos,
duplicacion global 7.0 % y cobertura global 15.9 %, segun captura del usuario.

Se asociaron etiquetas a sus controles con atributos for/id en los filtros
de Families, Species, Locations, Researchers, NaturalReserves y Records.
Cuando no existia etiqueta visible se agrego una visually-hidden para
lectores de pantalla, conservando la disposicion del formulario.

En el carrito, cada cantidad tiene etiqueta e identificador basado en el
Id unico del detalle, manteniendo name=quantity y sus limites originales.
En la confirmacion de borrado de registros, las celdas de descripcion son
ahora encabezados th con scope=row, vinculando cada concepto con su valor.

Los parametros de filtrado, acciones y reglas de negocio no cambiaron.
Compilacion sin errores ni advertencias. La suite existente se vuelve a
ejecutar con cobertura; automated-tests.trx conserva sus resultados.
El numero de alertas cerradas queda pendiente del siguiente analisis.
No se han publicado estos cambios en una nueva imagen de Kubernetes.
