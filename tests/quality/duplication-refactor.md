# Reduccion de duplicacion en formularios

Punto de partida: captura del usuario con 8.7 % global y 28 archivos afectados.
La lista incluye 950 lineas duplicadas en SyncInventoryMovementsSchema.cs y
otras migraciones historicas. No se modificaron ni excluyeron migraciones.

Se extrajeron cinco parciales tipadas _FormFields.cshtml, reutilizadas por
Create.cshtml y Edit.cshtml de Families, Species, Records, Locations y
NaturalReserves. Los campos comunes tienen una sola definicion. Cada vista
mantiene accion de formulario, identificadores ocultos, controles de estado y
boton correspondientes. Las diferencias de seleccion inicial de Species se
mantienen mediante el indicador IsCreateForm, sin cambiar los nombres enviados.

Validacion: compilacion de la solucion sin errores ni advertencias; 72 pruebas
C# aprobadas y ninguna omitida. Siete escenarios nuevos renderizan las parciales
Razor reales: nombres sin prefijo para model binding, validacion de campos,
valores iniciales de Species y selector/identificador de investigador, incluyendo
codificacion HTML de su nombre. El reporte coverage.xml se regenero.

El porcentaje posterior no esta medido todavia. Se necesita publicar un nuevo
analisis de SonarQube. No se cambiaron umbrales ni se ocultaron archivos del
calculo de duplicacion. Esta modificacion esta en el codigo local; no se ha
reconstruido ni desplegado una imagen nueva en Kubernetes en este paso.

El objetivo inmediato es disminuir repeticion mantenida a mano. Un 0 % global
no se puede afirmar mientras las migraciones y los demas bloques de la lista
sigan participando en el calculo. No se presenta una exclusion de archivos
generados como si fuera una refactorizacion.
