# Contribución y versionamiento

## Ramas y revisión

- `main` contiene el trabajo integrado. Preparar cambios en ramas `feat/`,
  `fix/`, `docs/` o `chore/` con un nombre descriptivo.
- La integración distribuida se prepara en `feat/integracion-distribuida`.
- Publicar la rama y abrir un pull request contra `main`; describir problema,
  comportamiento resultante, validación y limitaciones. Integrar tras revisión.
- No usar `push --force` en ramas compartidas ni reescribir historial publicado
  sin una decisión explícita del equipo.

## Commits

Usar Conventional Commits y agrupar por propósito:

```text
feat(realtime): synchronize product availability across replicas
fix(security): verify payment callbacks against the provider
refactor(ui): share create and edit fields
test: cover required form binding and CSP behavior
docs: describe measured results and deployment
```

Separar dependencias, lógica de negocio, interfaz, infraestructura, pruebas y
documentación cuando formen cambios revisables. Incluir juntos los archivos
que se necesitan para compilar o funcionar: una separación por extensión no
debe dejar constructores, vistas o configuraciones incompatibles.

No afirmar que un commit individual pasó una prueba si solo se ejecutó sobre
el árbol final. Identificar el commit o snapshot validado y los comandos usados.

## Herramientas y comprobaciones

`global.json` registra el SDK .NET 10.0.403 usado, permitiendo parches de la
misma banda. `.node-version` registra Node 24.22.0. Conservar el lockfile de
las herramientas JavaScript; instalar mediante `npm ci`.

Desde la raíz del repositorio:

```powershell
dotnet restore BioGamaEcuador.sln
dotnet build BioGamaEcuador.sln --no-restore
dotnet test tests/BioGamaEcuador.Tests/BioGamaEcuador.Tests.csproj --no-build
npm.cmd ci --prefix tests/browser --ignore-scripts --no-fund
node --test tests/browser/stock.test.cjs tests/browser/markdown.test.cjs tests/browser/ui-actions.test.cjs
git diff --check
```

La comprobación Chrome es adicional y necesita el laboratorio desplegado:

```powershell
node --test tests/browser/security-browser-check.test.cjs
```

Las ayudas de `scripts/` son locales, están ignoradas y no forman parte de un
clon. Para las pruebas versionadas usar los comandos directos anteriores.
No ejecutar el generador masivo, pagos reales o envíos de correo para validar
una edición documental. Las pruebas de red, carga y fallo de pods se ejecutan
solo en el laboratorio y requieren registrar condiciones y versión.

## Qué publicar

Incluir código, manifiestos, pruebas, lockfiles, licencias de bibliotecas y
resultados seleccionados. Verificar `git diff --cached` antes de cada commit.

Mantener fuera de Git credenciales, configuraciones privadas, `scripts/`,
`.local/`, perfiles de navegador, `node_modules`, `bin/obj`, reportes grandes
de cobertura y exportaciones repetidas. Los archivos completos de evidencia
permanecen locales; [evidencias/README.md](evidencias/README.md) explica qué
subconjunto se publica. No presentar un manifiesto local como si cubriera
únicamente los archivos publicados.

Si una clave estuvo en un commit previo, retirarla del archivo actual no la
borra del historial. Rotar la credencial que siga vigente. No reescribir el
historial para intentar resolverlo durante una subida ordinaria.

## Versiones y despliegues

- Usar etiquetas Git anotadas con formato SemVer: `vMAJOR.MINOR.PATCH`.
- Para una versión candidata, usar un identificador como `v1.0.0-rc.1`;
  no llamarla entrega final o versión productiva sin verificar su alcance.
- Las etiquetas publicadas son inmutables: no mover una etiqueta a otro commit.
- Las etiquetas de imagen (`k8s-v10`) identifican artefactos de laboratorio y
  no sustituyen a las versiones del código. Registrar ambas cuando se despliegue.
- No cambiar `APP_VERSION`, imágenes o métricas históricas solo para hacerlas
  coincidir con una etiqueta nueva. Reconstruir y medir si se necesita una
  comparación real de esa versión.

Una nueva versión candidata puede conservar riesgos conocidos documentados.
Para este laboratorio quedan pendientes dos restricciones CSP, disponibilidad
de base/Redis/nodo, y pruebas completas de las integraciones externas.
