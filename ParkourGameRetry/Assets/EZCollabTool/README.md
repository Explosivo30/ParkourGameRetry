# EZCollabTool

Versión: 0.2.0

Unity: 6

Plataforma: Editor only (Windows, macOS y Linux)

EZCollabTool permite que varios Unity Editors trabajen en tiempo real sobre la misma escena. Un editor actúa como host y ejecuta un servidor TCP embebido; los demás se conectan como clientes. No usa servicios externos, suscripciones ni Git.

## Estado de esta versión

Se sincronizan:

- Movimiento, rotación y escala a 20 Hz mientras se edita.
- Creación de primitivas, GameObjects vacíos, prefabs y objetos con componentes de Unity.
- Geometría generada y objetos editables de ProBuilder en el momento de su creación.
- Borrado de objetos y jerarquías.
- Locks autoritativos: un objeto bloqueado por otro peer no se puede seleccionar ni mover y aparece gris en la jerarquía.
- Locks de selección y presencia de peers.
- Snapshot inicial autoritativo de la escena del host.

Todavía no se sincronizan:

- Cambios posteriores en propiedades de componentes distintas de Transform (sí se copia su estado al crear el objeto).
- Renombrado.
- Reparenting durante una sesión.
- Assets que no existan en el proyecto de los demás.
- Interpolación visual de transforms.
- Undo colaborativo global.

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

Cada conexión serializa sus escrituras para impedir que dos mensajes mezclen sus bytes. Mientras un cliente recibe el snapshot, el servidor guarda sus deltas pendientes y los envía después en orden.

Mensajes actuales:

- SceneSnapshot
- CreateObject
- DestroyObject
- TransformDelta
- ComponentDelta (reservado)
- LockRequest, LockGranted, LockDenied, LockRelease
- ObjectLocked, ObjectUnlocked
- PeerJoined, PeerLeft
- Heartbeat

## Git

EZCollabTool no ejecuta git add, commit, push ni pull. La sincronización de archivos sigue siendo responsabilidad del equipo. El plugin sincroniza el estado vivo de la escena, no el contenido del repositorio.

## Notas operativas

- Si el host se desconecta, la sesión termina.
- Un cliente espera un máximo de 20 segundos por el snapshot inicial.
- Un frame de red puede ocupar como máximo 10 MB.
- Los prefabs deben existir en la misma ruta en todos los proyectos.
