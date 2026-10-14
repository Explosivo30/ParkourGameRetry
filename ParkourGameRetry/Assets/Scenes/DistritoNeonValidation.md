# Distrito Neón — validación de entrega

Fecha: 18 de septiembre de 2026.
Unity 6000.0.58f2, renderizador URP existente.

## Resultado

**38 comprobaciones superadas. Sin errores de compilación ni excepciones durante la ejecución final.**

- Vuelta principal continua mediante teclado simulado y CharacterController real: **01:08,78**.
- Distancia recorrida: **670,4 metros**.
- Recuperaciones: **0**.
- Estilo máximo de la vuelta automática: **B**.
- Ruta principal completada sin wall-run ni gancho.
- Wall-run y gancho comprobados por separado, entrando en sus estados reales y llegando a la plataforma de destino.
- Las seis zonas se visitaron en una sola vuelta de exploración que regresó al mercado; el reloj permaneció a cero.
- Todas las rampas públicas se recorrieron en subida y bajada.
- Caída larga en exploración: regreso al punto seguro del barrio, sin activar cronómetro.

El tiempo es una referencia automática reproducible, no un tiempo de primera partida de una persona.

## Carrera e interfaz

Se comprobaron inicio con E junto al panel, rechazo de E lejos de él, interacción del mando, cuenta atrás de tres segundos, bloqueo y liberación del jugador, rechazo de meta al saltarse checkpoints, guardado y recarga de mejor marca, selección inicial de los botones con mando, resultados congelados y regreso a exploración desde la terraza.

R y una caída devuelven al checkpoint realmente alcanzado, conservan el tiempo y cuentan recuperaciones. T cancela al mercado. Repetir carga el récord anterior y reinicia los datos de la vuelta.

El test usa DistritoNeon.AcceptanceTest.Best y la elimina al terminar. El récord de juego es DistritoNeon.Race.v1.Best; Distrito Libre usa su propia clave.

## Materiales y capturas

- 4.411 referencias de material inspeccionadas; ninguna ausente.
- Identificadores de assets duplicados en Assets: 0.
- Revisión de vistas desde la altura del jugador en los seis barrios.
- Captura adicional de conjunto y de la pantalla real de resultados.
- Bordes menta legibles, superficies principales despejadas y cartel de Espectro elevado para ver el aterrizaje.
- Cajas simples en superficies jugables; detalles pequeños sin colisión.
- Pavimento húmedo con textura y normales del asset; sin cambios de agarre.
- Sin cambios en scripts o parámetros de movimiento ni NPC.

## Rendimiento a 1080p

Equipo: NVIDIA GeForce RTX 3070 y AMD Ryzen 5 7600X.

| Vista | Media equivalente | Mediana de fotograma | Percentil 95 |
| --- | ---: | ---: | ---: |
| Mercado | 222,6 FPS | 4,30 ms | 5,05 ms |
| Avenida Espectro | 228,4 FPS | 4,31 ms | 5,02 ms |
| Terraza Común | 403,3 FPS | 2,40 ms | 3,02 ms |

Método: editor en modo batch, cámara de 1920 × 1080, sin límite de fotogramas, 60 muestras de calentamiento y 180 de medición por vista. Cada muestra fuerza un render y una lectura síncrona de la GPU, para incluir trabajo gráfico completado. Incluye sobrecoste del editor y de la lectura; no representa una medición de un ejecutable distribuible ni el mínimo de toda la ruta.

Las tres vistas medidas superan el objetivo de 60 FPS (16,67 ms). Se conservó URP y se limitaron sombras a la luz principal; luces locales sin sombras, LOD en los prefabs, horizonte simplificado y reflexión de 128 px actualizada una sola vez.

## Reproducir y editar

- Abrir Assets/Scenes/DistritoNeon.unity o el menú Parkour > Open Distrito Neon.
- Generación explícita: NeonDistrictSceneBuilder.BuildBatch.
- Prueba completa: NeonDistrictPlaytest.RunBatch.
- Vistas: Logs/NeonPreviews/01_Mercado.png a 08_Results.png.
- Informes completos: Logs/NeonValidation.txt y Logs/NeonPlaytest.txt.
- Controles y notas de edición: DistritoNeonREADME.md.

La escena y sus materiales son independientes. Los cambios previos del proyecto, incluidos los de Distrito Libre, se han conservado.