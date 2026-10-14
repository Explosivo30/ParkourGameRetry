# Distrito Neón

Escena: Assets/Scenes/DistritoNeon.unity.
Abrir con doble clic o con **Parkour > Open Distrito Neon**, y pulsar Play.

## Jugar

Se entra en exploración libre, con el reloj apagado. El panel de carrera está junto al punto inicial del mercado.

| Control | Acción |
| --- | --- |
| WASD / stick izquierdo | Moverse |
| Ratón / stick derecho | Mirar |
| Espacio | Saltar; mantener junto a una pared para trepar |
| Ctrl | Deslizarse |
| Botón derecho del ratón | Gancho, apuntando al anclaje amarillo |
| E / acción Interact del mando (Y / triángulo) | Iniciar carrera junto al panel |
| R | Recuperar posición; en resultados, repetir |
| T | Cancelar y volver al mercado en exploración |
| Esc / clic izquierdo | Liberar / capturar el cursor |

Las marcas **menta** conectan superficies transitables. Las **amarillas** indican atajos. Durante la carrera se enciende únicamente la baliza del siguiente checkpoint.

## Recorrido

Seis manzanas en dos columnas y tres filas, en un área jugable de 180 × 210 metros:

    NORTE
    CAPA                 CANAL ABIERTO
      ↑                  → pasarela exterior
    RODANTES             AVENIDA ESPECTRO
      ↑                    ↓ plataformas
    MERCADO              TERRAZA COMÚN
      └──── puente de regreso ────┘
    SUR

La carrera sigue Mercado → Rodantes → Capa → Canal Abierto → Avenida Espectro → Terraza Común.

- Mercado: mostrador para saltar o rodear y primera subida.
- Rodantes: conducto bajo para deslizarse, saltos y bancos de skate.
- Capa: dos trepadas de 2,6 metros.
- Canal: pasarela continua; un wall-run permite recortar el desvío.
- Espectro: plataformas a la izquierda; gancho opcional en la línea amarilla.
- Terraza: meta, plantas, faroles y conexión de regreso al mercado.

El recorrido principal se completa sin gancho ni wall-run. Las calles son transitables y las rampas públicas conectan cada barrio con sus azoteas. También se puede regresar por el puente sur. Los accesos no son puertas de un solo sentido.

## Carrera y recuperación

E inicia una cuenta atrás de tres segundos desde la salida. Hay cinco checkpoints ordenados y una meta: tocar la meta sin los anteriores no termina la carrera.

R y las caídas fuera del recorrido devuelven al último checkpoint durante la carrera. El tiempo continúa y aumenta el contador de recuperaciones. En exploración se usa el punto seguro del barrio; las caídas largas desde las azoteas también recuperan al jugador. Caminar por las calles o bajar por las rampas no cuenta como caída.

Al acabar se muestra tiempo, mejor marca, recuperaciones y estilo máximo. **Repetir carrera** vuelve a la salida con cuenta atrás. **Seguir explorando** deja al jugador en la terraza y apaga el reloj. T vuelve al mercado desde cualquier modo.

El récord usa la clave PlayerPrefs **DistritoNeon.Race.v1.Best**. No utiliza la clave de Distrito Libre. El suelo húmedo es visual: no se modifica el agarre ni la física del jugador.

## Arte y edición

- Los edificios, comercios, balcones, conductos, pasarela decorativa, cables, faroles, anuncios y holograma proceden de HIVEMIND/CyberpunkCity/URP.
- WhiteCity se usa exclusivamente en el horizonte.
- Materiales, texto, iluminación y ambiente propios: Assets/Materials/DistritoNeon.
- Las colisiones jugables son cajas y rampas sencillas; los detalles del asset están fuera de las colisiones.
- Se conservan el renderizador URP del proyecto y los ajustes de movimiento existentes.
- Luz principal con sombras, luces locales sin sombras, LOD de los prefabs y una reflexión de 128 px que se calcula una sola vez.
- No incluye personajes, conversaciones, persecuciones ni interiores completos.

El paquete URP original reutiliza identificadores del paquete HDRP. La copia importada aquí tiene identificadores propios y sus referencias internas actualizadas, para que las dos versiones convivan. Los dos scripts de demostración URP están aislados en Hivemind.Cyberpunk.URP.asmdef. La versión HDRP original permanece intacta.

## Pruebas y herramientas

Controlador del barrio: Assets/Scripts/ParkourScene/NeonDistrictCourse.cs.
Estados: Exploration, Countdown, Racing y Results.
API de pruebas: BeginRace(), Recover(), CancelRace(), ContinueExploring(), Mode, Elapsed, BestTime, CountdownRemaining, NextCheckpoint, Recoveries, PeakRank y CurrentZone.
InputReader expone OnInteractPressed utilizando la acción Interact ya existente.
La recuperación reutiliza PlayerStateMachine.ResetForCircuit.

NeonDistrictSceneBuilder.BuildBatch genera únicamente esta escena y sus variantes. Una ejecución explícita reemplaza la escena generada: abrirla desde el menú nunca regenera una escena que ya existe.

NeonDistrictPlaytest.RunBatch recorre la carrera con entradas de teclado y el CharacterController real. Prueba estados, resultados, guardado, el regreso al mercado, ambos atajos y accesos a pie. Usa una clave temporal de récord, que elimina al terminar.

Las capturas y los registros reproducibles se escriben en Logs/NeonPreviews, Logs/NeonValidation.txt y Logs/NeonPlaytest.txt. Los resultados finales de esta entrega se documentan en DistritoNeonValidation.md.