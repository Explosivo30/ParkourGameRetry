# Distrito Libre — Entre azoteas

Abrir `Assets/Scenes/DistritoLibre.unity` y pulsar Play. También se puede abrir desde `Parkour > Open or Create Distrito Libre`.

Prototipo jugable de una carrera entre azoteas, de unos 420 metros. El objetivo de diseño es una primera vuelta de aproximadamente un minuto; los atajos permiten mejorar el tiempo. Los nombres de los colectivos son provisionales.

## Recorrido

1. **Bajo la red:** salida sobre un depósito de tránsito, vigilancia abajo, un obstáculo y dos saltos de 3 m.
2. **Rodantes:** azoteas de skaters, mobiliario de madera, un conducto para deslizar y saltos con carrera. El conducto también se puede rodear.
3. **Capa:** taller de graffiti y dos terrazas de 2,6 m que se suben manteniendo Espacio.
4. **Canal Abierto:** pasarela segura a la izquierda; un wall-run permite cruzar directamente el hueco de 8 m.
5. **Horizonte:** plataformas anchas a la izquierda; el anclaje amarillo ofrece un atajo con gancho.
6. **La terraza común:** último salto, rampa y meta bajo las banderolas. Un lugar compartido por los colectivos.

Marcas **menta**: recorrido principal. **Amarillo**: atajos opcionales y meta. **Naranja**: obstáculos y paredes de trepada. Las geometrías del circuito tienen colisión; la ciudad de fondo es decorativa.

## Controles y tiempos

- WASD y ratón: movimiento y cámara.
- Espacio: saltar; mantener frente a una pared para trepar/subir el borde.
- Ctrl: deslizar mientras corres.
- Clic derecho apuntando al anclaje: gancho.
- R: recuperar el último checkpoint; después de terminar, nueva vuelta.
- T: comenzar una vuelta nueva desde la salida.
- Esc: liberar el cursor; clic izquierdo: volver a capturarlo.

El cronómetro empieza al cruzar el arco de salida. Hay cinco checkpoints intermedios que deben cruzarse en orden. Una caída o recuperación no reinicia el tiempo. La meta muestra el tiempo con centésimas, el número de recuperaciones, el rango de estilo máximo y la mejor marca, guardada localmente entre sesiones.

Llegar a la meta sin los checkpoints no completa la carrera. El tiempo queda congelado en la pantalla de resultados. Una nueva vuelta limpia el estilo, el movimiento y los checkpoints, conservando la mejor marca.

## Edición y comprobación

La escena es independiente de SkylineParkour y SampleScene. Usa el mismo controlador y los mismos valores de movimiento. No necesita añadirse a Build Settings para probarse en Play Mode; el reinicio ocurre dentro de la escena.

- `01 / CIRCUIT - 420 m`: los seis tramos, cada uno en su propio sistema de coordenadas.
- `02 / CITY & COMMUNITIES`: edificios de fondo y detalles de la ciudad.
- `03 / ORDERED GATES`: salida, checkpoints y meta, con volúmenes y posiciones de recuperación.
- `Distrito Libre / Circuit`: temporizador y resultados.

El generador abre la escena si ya existe; no la sobrescribe. Materiales propios en `Assets/Materials/DistritoLibre`. Los murales y edificios son arte provisional editable para probar el ritmo del nivel, no arte final.

Validación estructural: `Logs/DistritoLibreSceneValidation.txt`.
Prueba de juego: `Logs/DistritoLibrePlaytest.txt`.
Capturas: `Logs/DistritoLibrePreviews`.

Comprobación de juego: una vuelta continua por el recorrido principal, sin teletransportes entre tramos y sin caídas, terminó en **43,13 segundos** con entradas automatizadas. Es una referencia técnica, no una estimación del tiempo de una primera partida humana. También se comprobaron los dos atajos, el orden de checkpoints, la recuperación, el tiempo congelado y el reinicio de vuelta.
