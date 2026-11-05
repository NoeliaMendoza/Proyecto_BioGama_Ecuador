# Seguimiento del análisis del 4 de noviembre de 2026

Referencia: `evidencias/20261104-120402-8cac76/sonarqube/`.

## Resultado medido antes de esta tanda

| Métrica | Valor |
| --- | --- |
| Quality Gate | OK |
| Cobertura de código nuevo | 88,3 % |
| Duplicación de código nuevo | 0,0 % |
| Hallazgos nuevos | 0 |
| Cobertura global | 26,7 % |
| Duplicación global | 6,8 % |
| Hallazgos globales | 118 |

## Cambios posteriores preparados

- Identificadores y etiquetas asociadas en filtros de cursos, productos,
  inventario, catálogo de especies y consulta de IA. La indicación «Acción»
  sobre botones se representa como texto, no como una etiqueta de formulario.
- Cabeceras de columnas en la tabla de proveedores de inicio de sesión externo.
- `FamilyId` de especies exige presencia en el formulario y un valor positivo.
  Se amplió el caso de omisión en las pruebas de model binding.
- Opciones de deserialización de Ollama compartidas, evitando instancias por consulta.
- Tooltips de Bootstrap inicializados con `getOrCreateInstance`, evitando
  instancias redundantes y la construcción sin conservar la referencia.
- Solicitudes y límites explícitos de almacenamiento efímero para Redis y los
  dos contenedores del laboratorio de red.

No se modificaron umbrales de SonarQube ni se suprimieron reglas. Los avisos
del generador Python utilizan aleatoriedad reproducible para datos sintéticos;
requieren una revisión contextual, no afirmar automáticamente que existe una
vulnerabilidad explotable ni cambiar a un generador criptográfico sin necesidad.
Las migraciones históricas siguen incluidas en la métrica de duplicación global.

## Validación y límites

La compilación de la aplicación y del proyecto de pruebas finalizó. La ejecución
de xUnit fue bloqueada por Control de aplicaciones de Windows al cargar
`BioGamaEcuador.dll`, con código `0x800711C7`; no es un resultado de pruebas
aprobadas. No se cambiaron políticas de seguridad ni se intentó eludir el bloqueo.
El resultado se conserva en `.local/quality-followup/quality-followup.trx`.

Después se compiló una copia de las fuentes en un contenedor Linux del SDK .NET
10, sin configuraciones privadas ni credenciales. El resultado fue **121 pruebas
aprobadas, cero fallidas y cero omitidas**. Esta es la validación de las pruebas
de esta tanda; el fallo inicial de Windows se conserva como diagnóstico separado.

La comprobación `node --check wwwroot/js/site.js` y `git diff --check` pasó.
La primera validación de manifiestos no pudo conectar con Kubernetes. Tras iniciar
Docker Desktop, `kubectl apply --dry-run=server` validó correctamente los archivos
`03-redis.yaml` y `04-network-lab.yaml`. No se desactivó la validación ni se
desplegaron estos cambios posteriores.

Se requiere publicar otro análisis para confirmar
cuántos hallazgos se cierran. El Gate OK y las métricas de la tabla corresponden
al análisis de referencia, anterior a esta tanda.
