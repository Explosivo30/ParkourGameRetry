# EZCollabTool

Versión: 0.3.0

Unity: 6

Plataforma: Editor only (Windows, macOS y Linux)

EZCollabTool permite que varios Unity Editors trabajen en tiempo real sobre la misma escena. Un editor actúa como host y ejecuta un servidor TCP embebido; los demás se conectan como clientes. No usa servicios externos, suscripciones ni Git.

## Estado de esta versión

Se sincronizan:

- Movimiento, rotación y escala a 20 Hz, más un estado completo consolidado al terminar la edición.
- Creación y borrado de primitivas, GameObjects vacíos, prefabs, componentes y objetos genéricos de Unity.
- ProBuilder después de cada modificación: caras, vértices, topología, UV, submeshes y materiales.
- Añadir, eliminar y modificar componentes y propiedades serializadas del Inspector, incluidos materiales, luces y colliders.
- Nombre, estado activo, padre y orden entre hermanos de la jerarquía.
- Undo/Redo colaborativo: el resultado del Undo o Redo local se publica como una nueva revisión para los demás editores.
- Locks autoritativos: el objeto bloqueado por otro peer aparece gris, no se puede seleccionar ni mover y se limpia cualquier selección de caras o vértices de ProBuilder.
- Detección por GUID y hash de assets/prefabs ausentes o con versiones distintas, visible en la ventana y en la consola.
- Snapshot inicial autoritativo dividido en bloques y recuperación incremental tras una desconexión.

Límites actuales:

- El Undo/Redo comparte el estado resultante; no es una transacción distribuida que pueda deshacer simultáneamente acciones de varios usuarios.
- Los archivos de assets no se transfieren por red. Se avisa si faltan o difieren y el equipo debe sincronizarlos mediante su control de versiones.
- Los scripts y componentes deben existir en todos los proyectos para poder reconstruirse.
- No hay autenticación, cifrado ni resolución automática de conflictos fuera de los locks.

## Requisitos

Todos deben:

1. Usar Unity 6.
2. Tener la misma versión del proyecto, la misma rama y haber hecho pull antes de entrar.
3. Tener instalados los mismos assets y prefabs.

El package depende de com.unity.nuget.newtonsoft-json 3.2.1 y com.unity.probuilder 6.0.9.

## Instalación

Opción recomendada:

1. Copia la carpeta EZCollabTool dentro de TuProyecto/Packages/.
2. Abre Unity; el package.json hará que se detecte como package local.

También puede instalarse desde Window > Package Manager > + > Add package from disk, seleccionando el package.json.

## Uso

### Host

1. Abre Window > EZCollabTool.
2. Escribe tu nombre y el puerto TCP. El valor por defecto es 9876.
3. Pulsa Start session (host).
4. Comparte con el equipo la IP accesible del host.

### Clientes

1. Abren Window > EZCollabTool.
2. Escriben su nombre, la IP del host y el mismo puerto.
3. Pulsan Join session.

Solo el host necesita aceptar conexiones entrantes en ese puerto. En una LAN se usa su IP local. Para trabajo por Internet es preferible una red privada entre los miembros del equipo; el protocolo actual es TCP plano y todavía no incluye autenticación ni cifrado.

## Snapshot e identidad

Los GUIDs colaborativos viven únicamente en memoria y nunca añaden componentes ni modifican el Inspector. Al entrar un cliente, el snapshot usa la identidad persistente de los objetos de escena para asociar la copia local con los GUIDs del host. Así conserva los componentes que ya existen en la escena base, crea solo los objetos que faltan y elimina objetos locales que no están en el estado autoritativo del host.

Los objetos creados durante la sesión reciben GUIDs nuevos y los comparten mediante los mensajes de creación.

## Red

El protocolo usa TcpListener y TcpClient, no WebSocket:

~~~text
4 bytes little-endian con la longitud
payload JSON UTF-8
~~~

Cada conexión serializa sus escrituras para impedir que dos mensajes mezclen sus bytes. Mientras un cliente recibe el snapshot, el servidor guarda sus deltas pendientes y los envía después. El historial conserva hasta 4096 mensajes o aproximadamente 32 MB de JSON; si la revisión solicitada ya no está, se envía de nuevo un snapshot completo.

Mensajes actuales:

- SceneSnapshotBegin, SceneSnapshotChunk y SceneSnapshotEnd
- RecoveryComplete
- CreateObject
- DestroyObject
- TransformDelta
- ComponentDelta
- LockRequest, LockGranted, LockDenied, LockRelease
- ObjectLocked, ObjectUnlocked
- PeerJoined, PeerLeft
- Heartbeat

## Rendimiento en escenas grandes

Durante la edición no se usa `GameObject.Find`, `FindObjectsOfType` ni un recorrido completo de la escena. Se mantiene un índice GUID → GameObject y los cambios llegan mediante `ObjectChangeEvents`. Las creaciones, deltas y recorridos de subjerarquías se procesan en colas con límites por frame.

El recorrido O(N) de una escena grande solo es necesario al iniciar la sesión y al producir o aplicar un snapshot completo. Por tanto, una escena de 10.000 objetos no provoca una búsqueda de 10.000 objetos con cada movimiento, aunque la primera conexión y la serialización de meshes/componentes sí tardarán en proporción al tamaño real de la escena.

Los snapshots se envían en bloques de 64 objetos. Para obtener buen rendimiento todos los editores deben partir de la misma escena y los mismos assets; meshes ProBuilder muy densos generan mensajes JSON mayores que objetos normales.

## Git

EZCollabTool no ejecuta git add, commit, push ni pull. La sincronización de archivos sigue siendo responsabilidad del equipo. El plugin sincroniza el estado vivo de la escena, no el contenido del repositorio.

## Notas operativas

- Si un cliente pierde momentáneamente al host, realiza hasta cinco intentos de reconexión y solicita las revisiones posteriores a la última aplicada.
- Si el host termina la sesión definitivamente, los clientes no pueden recuperarla.
- Un cliente espera un máximo de 120 segundos por la sincronización inicial.
- Un mensaje de red puede ocupar como máximo 64 MB.
- Los prefabs se resuelven primero por GUID, se comprueban por hash y deben existir en todos los proyectos.
