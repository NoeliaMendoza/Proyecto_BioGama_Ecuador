# Pruebas posteriores al resultado 63,8 %

Resultado previo: evidencias/20261103-105240-b9b521/sonarqube/quality-gate.json.
Solo falla new_coverage (63,8 frente al minimo 80). Las advertencias de
exportacion son independientes del resultado del Quality Gate.

Se ampliaron las pruebas C# de 43 a 53: telefonos validos e invalidos al
preparar una compra, impedimento de comprar un pedido ajeno, metadatos de
conexion de los dos hubs dirigidos exclusivamente al cliente que se conecta
y respuestas de salud/disponibilidad. Los pagos se prueban sin cargos reales.
Resultado local: 53 ejecutadas, 53 aprobadas, ninguna omitida, segun
automated-tests.trx. coverage.xml contiene la cobertura dinamica actualizada.

Las nueve pruebas JavaScript existentes siguen documentadas en browser-tests.txt
y javascript.lcov.info. Su 100 % corresponde exclusivamente a product-stock.js,
no a la aplicacion completa ni a una prueba con navegadores reales.

Se agrego una comprobacion aislada de load_db_config de datos.py: rechaza claves
ausentes y lee host, puerto y clave desde variables de entorno. Extrae la
funcion mediante AST para evitar ejecutar el generador masivo. No importa el
generador completo ni abre conexiones ni inserta datos. Se ejecuta en un
contenedor sin red y con el proyecto montado en solo lectura; el reporte
python-coverage.xml incluye las lineas sin ejecutar del resto de datos.py.
Esta comprobacion solo valida la configuracion, no el generador completo.

Analyze-And-Save.ps1 importa los tres reportes de cobertura y detiene la
publicacion si falla cualquiera de los pasos de prueba. No se cambiaron
umbrales del Gate ni se excluyo codigo de negocio para aumentar la cobertura.

El porcentaje nuevo de SonarQube sigue pendiente: requiere ejecutar nuevamente
Analyze-And-Save.ps1 con el token del usuario y esperar el procesamiento.
Este conjunto de evidencias locales no demuestra que el Gate haya aprobado.
