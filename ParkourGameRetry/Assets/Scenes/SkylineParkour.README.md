# Skyline / Rooftop Run

Abrir `Assets/Scenes/SkylineParkour.unity` en Unity.

Circuito independiente entre azoteas, con el controlador de movimiento del proyecto, cámara en primera persona, seis checkpoints, cronómetro y recuperación al caer.

## Recorrido

1. Saltos: obstáculo inicial y dos huecos de 4 m.
2. Ascenso: tres terrazas de 2,8 m para encadenar salto, trepada y subida al borde.
3. Ritmo: túnel de 1,8 m de altura libre, saltos y rampa descendente.
4. Pared: panel turquesa para correr por la pared; la recepción está 3 m más baja.
5. Precisión: plataformas estrechas desplazadas lateralmente.
6. Último vuelo: gancho sobre un hueco de 14 m, ruta alternativa de saltos y meta.

## Controles

- WASD: movimiento; ratón: cámara.
- Espacio: salto. Mantener frente a una pared para trepar.
- Ctrl o C mientras corres: deslizar.
- Clic derecho apuntando a la esfera turquesa: gancho.
- R: volver al checkpoint; después de completar el circuito, comenzar una nueva vuelta.
- Esc: liberar cursor; clic izquierdo: capturarlo de nuevo.

El cronómetro comienza al avanzar desde la salida. El turquesa señala la ruta; el naranja identifica las paredes de trepada; las bandas doradas indican checkpoints.

## Edición y validación

Cada tramo está agrupado bajo `01 / ROOFTOP COURSE`. Las plataformas son primitivas con BoxCollider y materiales compartidos. Los edificios de fondo no tienen colisiones.

`Parkour > Create Skyline Rooftop Scene` abre la escena si ya existe. El generador no sobrescribe una escena existente.

La escena se generó y revisó en una copia temporal de Unity. El proyecto principal también compila correctamente tras completar los métodos obligatorios de los estados NPC. El registro de compilación está en Logs/NpcStateCompile.log.

Validación completada: 10 comprobaciones de juego superadas (inicio, cámara, avance, salto, trepada, deslizamiento, wall-run, recuperación, meta y nueva vuelta). Informes en Logs/SkylineSceneValidation.txt y Logs/SkylinePlaytest.txt. No sustituye una prueba manual completa de todas las rutas.
