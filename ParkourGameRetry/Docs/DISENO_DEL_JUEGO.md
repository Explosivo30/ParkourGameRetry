# Shadows of Nyesa — GDD

**Documento de diseño del juego · Borrador 0.20 · 4 de octubre de 2026**

Este GDD reúne lo hablado y convierte el enfoque en reglas y situaciones concretas. Se seguirá revisando en este mismo archivo. Las decisiones confirmadas se indican en la sección 2; el resto es una propuesta de diseño para discutir y probar. Las reglas descritas no se consideran implementadas.

## 1. El juego en una página

**Concepto:** una aventura de acción en primera persona donde Nebu y Ai'ka recorren juntos Nyesa. El jugador supera obstáculos y enemigos combinando parkour, lanzamiento de cuchillos, látigo/gancho y ralentización del tiempo; Ai'ka está físicamente presente, hace parkour y participa como compañera. Edgar aclara que su idea original también incluía crear plataformas y paredes para el parkour durante el combate, con su ubicación elegida por el jugador mediante el ratón.

**Experiencia principal:** elegir una posición, llegar hasta ella, atacar y encadenar el siguiente movimiento. El jugador reconoce oportunidades en el escenario y aprende a aprovecharlas durante el combate.

**Estructura propuesta:** campaña para un jugador, con un avance principal claro, capítulos y alternativas locales. Cada tramo conduce hacia un destino; algunos encuentros son obligatorios y otros admiten soluciones diferentes.

**Fantasía del jugador:** ser alguien capaz de atravesar un lugar peligroso y vencer sus amenazas gracias al dominio de su cuerpo, sus herramientas y sus poderes.

**Sensaciones buscadas:** libertad al elegir cómo actuar, tensión durante los enfrentamientos, satisfacción al superar un tramo y sentir que compartes la aventura con Ai'ka.

**Diferencia jugable que queremos comprobar:** un mismo espacio sirve para desplazarse y combatir. La altura, las paredes y los anclajes ofrecen ventajas frente a enemigos con funciones distintas.

## 2. Decisiones de Edgar y propuestas abiertas

| Tema | Estado |
| --- | --- |
| Reinterpretar Code-Memory / Shadows of Nyesa | Confirmado |
| Conservar el mundo y los personajes; revisar historia y jugabilidad | Confirmado |
| Mejorar especialmente movimiento e IA | Confirmado |
| Ai'ka acompaña físicamente al jugador y también hace parkour | Intención aclarada por Edgar |
| Referencia principal para su papel: Elizabeth de BioShock Infinite | Confirmada de nuevo por Edgar |
| Entrega de objetos al estilo «Booker, catch» | Aclarada por Edgar; lanzamiento localizado en prototipos de Ai'ka |
| Ai'ka crea plataformas y paredes para facilitar parkour y ayudar en combate | Intención original aclarada por Edgar; no terminada por falta de tiempo |
| El jugador elige con el ratón dónde colocar esas superficies | Intención aclarada por Edgar; control exacto pendiente |
| Ambos personajes pueden recorrer las superficies; en combate Nebu pelea y Ai'ka apoya | Aclarado por Edgar; Ai'ka mantiene el papel de Elizabeth |
| Visualizar la superficie antes de colocarla | Idea provisional de Edgar; interacción exacta pendiente |
| Colocar superficies en casi cualquier sitio válido | Elegido por Edgar; criterios de validez pendientes |
| Mantener varias superficies para encadenar una ruta | Elegido por Edgar; cantidad máxima pendiente |
| Paredes y plataformas desaparecen después de unos segundos | Elegido por Edgar; duración exacta pendiente de probar |
| Crear superficies antes de iniciar una maniobra y también en pleno salto | Ambas posibilidades elegidas por Edgar |
| Ai'ka crea superficies con un alcance amplio y se mantiene relativamente cerca | Aclarado por Edgar; distancia exacta pendiente |
| Nebu y Ai'ka empiezan colaborando por necesidad y construyen confianza | Elegido por Edgar |
| El jugador puede hablar con Ai'ka para mejorar la relación | Intención de Edgar; conversaciones y efectos concretos pendientes |
| Elegir las respuestas de Nebu en las conversaciones | Confirmado por Edgar; indica que mejoró el sistema para ello |
| Las respuestas pueden mejorar o empeorar la relación | Confirmado por Edgar |
| La relación afecta a respuestas, miradas y comportamiento expresivo de Ai'ka | Elegido por Edgar |
| Cuando se molesta, Ai'ka dice directamente lo que piensa | Elegido por Edgar |
| Que Nebu le mienta es un motivo importante de conflicto para Ai'ka | Elegido por Edgar; sinceridad relevante para la confianza |
| Descubrimiento posterior de una mentira que parecía funcionar al principio | Propuesta descartada por Edgar |
| Combate, poderes y prestaciones del apoyo conservan las mismas reglas en cualquier estado de relación | Límite explícito de Edgar |
| Combate y avance hacia adelante | Confirmado como base del juego |
| Libertad y tensión | Sensaciones elegidas |
| Escapar mediante parkour como centro | Descartado por la aclaración de Edgar |
| Combate que aprovecha el parkour para ganar posición | Propuesta de enfoque |
| Cuatro herramientas principales y tres funciones de enemigo | Propuesta |
| Historia concreta, equilibrio, cantidad de niveles y duración | Pendientes |

## 3. Mundo, personajes e historia

**Base conservada:** Nyesa, Nebu, Ai'ka y los demás personajes del mundo original. Los diálogos revisados mencionan la búsqueda de los padres, una población sometida, la Conjunción y su Séquito, poderes compartidos y un misterio en el planeta.

**Argumento de trabajo:** Nebu busca información sobre sus padres y se encuentra con Ai'ka. Su búsqueda se cruza con lo que la Conjunción hace en Nyesa. Ai'ka aporta conocimientos y una motivación propia relacionada con la población. Ayudarla permite avanzar, descubrir información y comprender mejor los poderes.

Este argumento es una propuesta apoyada en el original. Los acontecimientos y el desenlace se escribirán juntos.

**Relación elegida por Edgar:** Nebu y Ai'ka empiezan colaborando por necesidad y construyen confianza durante la aventura. El jugador puede hablar con ella para mejorar esa relación. Las respuestas elegidas pueden mejorar o empeorar la confianza, que se expresa mediante el diálogo, las miradas y el comportamiento de Ai'ka. El combate y las prestaciones del apoyo mantienen las mismas reglas. Sus motivos concretos para colaborar y las escenas que muestran esa evolución siguen pendientes de escribir.

**Evolución propuesta:** al principio se comunican sobre lo que necesitan y el siguiente objetivo; las experiencias compartidas abren conversaciones más personales; con mayor confianza, muestran interés por lo que le ocurre al otro. Esta evolución se comprobará en sus palabras, reacciones y momentos compartidos. Las etapas concretas son una propuesta, no un guion confirmado.

Ai'ka acompaña al jugador como personaje presente en el escenario. Edgar explica que la versión original tuvo que reducir ese papel por falta de tiempo. Esta reinterpretación recupera la intención de recorrer y vivir la aventura juntos.

La historia se presenta mediante conversaciones durante el recorrido, reacciones a lo que ocurre, detalles del escenario y escenas en momentos importantes. Cada capítulo debe responder una pregunta y abrir un objetivo comprensible para el siguiente.

## 4. Ciclo de juego

**Destino → desplazamiento → encuentro → resolución → descubrimiento → siguiente destino.**

1. El jugador entiende dónde debe llegar y qué pretende conseguir.
2. Recorre un espacio y encuentra conexiones o una oportunidad de movimiento.
3. Reconoce las amenazas y elige una posición o un orden de ataque.
4. Combate, cambia de posición y utiliza un poder cuando conviene.
5. Supera el encuentro o cumple su condición concreta.
6. Recibe información, una capacidad o una consecuencia narrativa.
7. Continúa hacia un tramo que exige una nueva combinación de lo aprendido.

El ritmo alterna acción y descanso. La exploración local permite encontrar información y accesos útiles. Una huida puede formar parte de una secuencia cuando la historia lo requiera.

## 5. Movimiento

La base del proyecto actual incluye salto, deslizamiento, carrera por paredes, trepada, agarre de bordes, rodar, dash, gancho y tirolina. El diseño de encuentros se concentra inicialmente en las acciones que ofrecen decisiones claras.

| Acción | Uso principal propuesto |
| --- | --- |
| Correr y saltar | Atravesar huecos y conectar posiciones |
| Deslizarse | Pasar por accesos bajos y alcanzar cobertura |
| Correr por paredes | Ganar una ruta lateral o una posición elevada |
| Trepar y agarrarse | Subir y recuperar una maniobra incompleta |
| Rodar al aterrizar | Continuar después de una caída preparada |
| Gancho | Llegar a un anclaje y cambiar de posición |

**Reglas de diseño:**

- Los puntos de apoyo, aterrizaje y anclaje se reconocen antes de iniciar una maniobra.
- Una transición termina con control suficiente para continuar o atacar.
- El jugador puede apuntar y disparar durante las maniobras que se hayan preparado para combate; esta compatibilidad requiere una prueba específica.
- La cámara permite leer la amenaza y mantener la orientación.
- Una caída pequeña puede dejar una ruta de recuperación cuando el escenario lo permita.
- Las rutas básicas se diseñan según el alcance real del controlador, comprobado jugando.

## 6. Combate y poderes

### Lanzador de cuchillos

Es la herramienta principal de ataque propuesta. Dispara hacia donde apunta el jugador, tiene un intervalo entre disparos y comunica claramente el impacto. Para la primera prueba, se propone recarga automática entre disparos.

Los enemigos vulnerables reciben daño; la protección frontal del guardia bloquea los impactos que llegan desde su zona protegida. Esto debe comunicarse con una respuesta visual y sonora distinta de un impacto válido.

El ataque por la espalda existe como referencia del original. Su inclusión y su función se decidirán después de probar el combate principal.

### Látigo / gancho

Su primera función es alcanzar anclajes del escenario. Permite cambiar de altura o de ángulo y conectar una maniobra con un ataque. El objetivo disponible se indica antes de usarlo y el final del desplazamiento conserva control.

El GDD utiliza ambos nombres porque el tutorial original dice «látigo» y su comportamiento de desplazamiento corresponde al gancho. Su nombre definitivo se elegirá junto con su representación.

### Ralentización del tiempo

Permite aprovechar una ventana breve para apuntar o ejecutar una maniobra. Se propone una reserva limitada que se recupera al derrotar enemigos. El tiempo afecta al mundo y a las acciones de forma coherente, mientras la cámara mantiene una respuesta controlable.

El primer ajuste de prueba será una activación corta. Su duración, recarga y relación con la velocidad del personaje siguen abiertas. Gastar la reserva no debe impedir continuar por el recorrido básico.

### Daño, derrota y recuperación

Como ajuste provisional, se propone que el jugador pueda soportar tres impactos antes de reiniciar el encuentro. La recuperación completa ocurre en el checkpoint. Este número sirve para probar el margen de error y se ajustará con partidas reales.

Reintentar devuelve jugador y enemigos al estado inicial del encuentro y restaura los recursos necesarios. Los enemigos derrotados permanecen fuera del combate durante el intento actual. La política de guardado entre sesiones queda pendiente.

### Valores iniciales para probar

| Elemento | Punto de partida propuesto | Qué se quiere comprobar |
| --- | --- | --- |
| Resistencia del jugador | Tres impactos | Que haya tensión y posibilidad de recuperarse de un error |
| Enemigo básico vulnerable | Un impacto válido de cuchillo | Que la precisión tenga una respuesta clara |
| Lanzador | Intervalo visible entre disparos | Que elegir el momento de ataque sea relevante |
| Poder temporal | Reserva breve, recuperación por derrotas | Que apoye las combinaciones sin resolver todo el encuentro |
| Reinicio | Checkpoint cercano al encuentro | Que repetir permita aprender rápidamente |

Estos valores son hipótesis de equilibrio. No describen valores comprobados de la build original ni una configuración ya aplicada.

## 7. Enemigos e IA hostil

| Tipo propuesto | Qué hace | Decisión que plantea |
| --- | --- | --- |
| Tirador | Amenaza desde una posición con buena visibilidad y anuncia su disparo | Cubrirse, cambiar de ángulo o alcanzarlo |
| Guardia protegido | Orienta su protección hacia el jugador y deja ventanas al girar o recuperarse | Flanquearlo y encontrar el lado vulnerable |
| Atacante cercano | Se aproxima por conexiones que puede recorrer y prepara un ataque cercano | Mantener distancia, cambiar de posición o resolverlo primero |

**Comportamiento común propuesto:** percibir → reaccionar → elegir posición → preparar ataque → atacar → recuperarse → reevaluar.

Los enemigos tienen tiempos de reacción y oportunidades visibles de respuesta. Los ataques respetan coberturas. Cuando pierden al jugador, actúan según su última información y las reglas del encuentro.

El tirador reajusta su posición o su orientación; el guardia intenta mantener la protección de frente; el atacante presiona el lugar ocupado por el jugador. Se combinan para crear decisiones, con capacidades de navegación acordes al escenario.

«Mejor IA» se evaluará por si sus acciones se entienden, si responden al movimiento del jugador y si permiten más de una solución útil. Los primeros enemigos no necesitan copiar todas las capacidades de parkour del protagonista.

## 8. Ai'ka como compañera presente

**Intención de Edgar:** Ai'ka recorre la aventura junto al jugador y también hace parkour. La referencia de su papel es Elizabeth de BioShock Infinite. Los comportamientos concretos siguientes son propuestas para adaptar esa referencia a Nyesa.

### Presencia durante el recorrido

Ai'ka alterna entre acompañar, adelantarse a una conexión que conoce, reunirse con el jugador y observar algo del entorno. Su posición se adapta al ritmo del jugador y mantiene libres los pasos estrechos y las recepciones de los saltos.

Si el jugador se detiene a explorar, Ai'ka puede mirar un objeto, descansar o comentar algo relevante. Las reacciones tienen pausas y condiciones para evitar repeticiones. En momentos narrativos mira al jugador y responde a lo que acaba de suceder.

El objetivo es que tenga intereses y reacciones reconocibles y que su presencia contribuya a la relación entre los personajes.

### Conversaciones y confianza

**Intención confirmada:** el jugador puede iniciar conversaciones con Ai'ka, elegir las respuestas de Nebu y mejorar su relación. Edgar indica que ha mejorado el sistema de diálogo para permitir elegir respuestas. Se integra esta interacción con los momentos de descanso, exploración y reunión del recorrido.

**Propuesta para probar:** después de una experiencia compartida, el jugador puede acercarse a Ai'ka y abrir un tema relacionado con lo ocurrido, sus motivos o su pasado. Las conversaciones posteriores reflejan lo que ya han hablado y lo que han vivido juntos. Con mayor confianza, Ai'ka puede compartir más información personal y mostrar reacciones de mayor cercanía.

**Consecuencias confirmadas por Edgar:** una respuesta puede mejorar o empeorar la relación. Ese cambio influye en cómo Ai'ka responde, se mueve y mira al jugador. La relación se expresa en su actitud y comunicación; las reglas de combate, poderes y apoyo se mantienen en todos sus estados.

**Forma de expresar el desacuerdo elegida por Edgar:** cuando Ai'ka se molesta, dice lo que piensa. Esta dirección guía la escritura de sus respuestas y las reacciones visibles de la relación.

**Motivo de conflicto elegido por Edgar:** a Ai'ka le molesta que Nebu le mienta. La sinceridad será importante al escribir sus conversaciones y la evolución de su confianza. Edgar descarta la propuesta de que una mentira parezca funcionar y Ai'ka la descubra más adelante. Los contextos concretos de conversación, la información que tiene Ai'ka, sus reacciones y las posibilidades de reparar el vínculo quedan pendientes de diseñar.

**Propuesta de escritura:** ante una respuesta que le duela o una decisión que desapruebe, expresa su desacuerdo y el motivo. Con más confianza, puede compartir también cómo le afecta personalmente. El tono, los temas que le importan y la forma de resolver el conflicto se definirán juntos.

**Ejemplos expresivos propuestos:** un tono más firme, una mirada sostenida o un gesto claro al plantear un desacuerdo; respuestas más personales y una postura más abierta cuando siente confianza. La selección de gestos y animaciones sigue pendiente. Estos cambios conservan las mismas prestaciones de ayuda y combate.

Los cambios de movimiento son expresivos: gestos, postura, orientación y acciones durante descansos o conversaciones. Su acompañamiento sigue permitiéndole prestar la ayuda necesaria. La confianza mantiene iguales el alcance y la disponibilidad de creación de superficies, recursos entregados, frecuencia y eficacia de apoyo. Una mala relación no bloquea habilidades ni retrasa una ayuda funcional.

Quedan por escribir los temas y respuestas, las reacciones concretas de Ai'ka y cómo se registra la confianza. Si una discrepancia respetuosa refuerza la relación dependerá de su personalidad y del contexto; todavía es una propuesta.

### Parkour y rutas

Se preparan conexiones de salto, trepada y carrera por paredes, con inicio, destino y acción definidos. Ai'ka decide qué conexiones usar para llegar a una posición de acompañamiento o reunión. Las animaciones y trayectorias se ajustan para que manos, pies y aterrizajes tengan contacto convincente.

Puede usar una ruta paralela y reunirse en el destino. El jugador sigue eligiendo su recorrido. Las acciones de Ai'ka incluyen mirar el destino, preparar la maniobra y continuar al aterrizar.

### Papel durante el combate

**Dirección aclarada por Edgar:** apoyo al estilo de Elizabeth, incluida la entrega visible de objetos que recuerda como «Booker, catch». Edgar precisa que Nebu se encarga de combatir y Ai'ka mantiene un papel de apoyo como Elizabeth. Ambos pueden hacer parkour por las superficies creadas. El apoyo de Ai'ka combina presencia, ayuda con recursos y creación de oportunidades de movimiento; sus maniobras concretas durante un enfrentamiento quedan por diseñar.

**Entrega de objetos: ofrecer, lanzar y recibir una ayuda.** La propuesta siguiente sigue abierta; su prioridad frente a la creación de superficies se decidirá con Edgar:

1. Ai'ka encuentra una posición segura desde la que puede entregar el objeto y detecta una necesidad del jugador.
2. Avisa con una frase breve y un gesto. El jugador dispone de una indicación para aceptar cuando está disponible.
3. Al aceptar, Ai'ka lanza el objeto y Nebu lo recibe con una respuesta visible y sonora; se aplica su efecto.
4. La entrega tiene un límite de frecuencia. Si el jugador no acepta, sigue combatiendo y Ai'ka continúa acompañándolo.

El recurso concreto queda pendiente. Para la primera prueba se propone una cura pequeña, porque permite comprobar la utilidad de la ayuda sin cambiar todavía la recarga del lanzador ni la del poder temporal. Es una hipótesis de diseño, no una decisión de Edgar. La recuperación de energía temporal sigue siendo otra opción.

La recepción debe adaptarse a un jugador que corre, salta o usa el gancho. Se probará una entrega asistida: el jugador decide aceptarla y el sistema garantiza que el objeto llegue, sin exigir una puntería adicional. La señal no debe forzar un giro brusco de cámara ni interrumpir una maniobra. Si se pierde una condición válida antes del lanzamiento, la oferta se cancela y no consume la ayuda.

### Plataformas y paredes elegidas por el jugador

**Intención original aclarada por Edgar:** Ai'ka podía ayudar creando tanto plataformas como paredes por las que hacer parkour. El jugador elegiría su ubicación con el ratón. La idea conectaba esa creación de superficies con la ayuda en combate y quedó sin terminar por falta de tiempo.

Esto introduce una decisión concreta durante la acción: elegir dónde disponer de una superficie y aprovecharla para continuar un movimiento o conseguir una posición. La función de las paredes y plataformas se diseña junto al combate, el movimiento y el papel de Ai'ka.

**Combinación elegida:** colocar varias paredes y plataformas en lugares válidos y enlazarlas con el escenario. Ejemplo propuesto: crear una pared lateral para correr por ella y una plataforma donde aterrizar, cambiar el ángulo frente a un enemigo y continuar atacando. La colocación libre y la posibilidad de encadenar están confirmadas; el ejemplo concreto sirve para probarlas.

**Aclarado por Edgar:** ambos personajes pueden recorrer las paredes y plataformas. En el combate Nebu pelea y Ai'ka actúa como apoyo, siguiendo la referencia de Elizabeth. Quedan por concretar las maniobras de apoyo de Ai'ka y cómo el jugador indica, orienta y confirma la colocación.

Edgar sugiere provisionalmente visualizar la superficie antes de colocarla; no había definido ese control. Se propone explorar una vista previa al apuntar, con la interacción exacta pendiente. Edgar elige colocarlas en casi cualquier sitio válido, disponer de varias para encadenar una ruta y que desaparezcan después de unos segundos. Quedan por definir los criterios de validez, duración exacta, tamaño, cantidad máxima y frecuencia de creación. Las superficies deben servir a maniobras comprensibles y permitir seguir leyendo el enfrentamiento.

**Alcance aclarado por Edgar:** Ai'ka debe estar relativamente cerca y dispone de un rango alto para crear superficies. La distancia exacta y la referencia desde la que se mide el alcance quedan pendientes de concretar. Como propuesta de acompañamiento, Ai'ka buscará posiciones desde las que pueda seguir prestando apoyo mientras Nebu cambia de altura o encadena una ruta.

La prueba de creación comprobará el alcance durante el movimiento y la separación entre ambos personajes. La relación entre alcance, vista previa y tiempo de respuesta debe permitir preparar rutas y ampliar una maniobra aérea de forma comprensible.

**Momento de creación confirmado:** el jugador puede preparar una ruta antes de lanzarse y también crear superficies mientras está en el aire para improvisar el siguiente apoyo. La interacción exacta sigue pendiente. La prueba debe comprobar que apuntar, visualizar y confirmar una superficie encaja con la duración del salto y permite continuar la maniobra.

**Duración temporal confirmada:** las superficies desaparecen tras unos segundos. La primera prueba ajustará ese tiempo a una ruta encadenada. Como propuesta aún sin confirmar, una señal visual previa a la desaparición permitiría leer cuánto tiempo queda. El inicio del tiempo, su relación con la ralentización y el comportamiento si un personaje ocupa una superficie al desaparecer quedan pendientes.

**Referencia comprobada en archivos:** los prototipos de Ai'ka crean una plataforma cerca de la posición prevista del jugador. Esa prueba recoge parte de la idea; no se ha verificado allí la colocación con el ratón ni la creación de paredes.

La entrega de objetos sigue formando parte de la referencia de Elizabeth. La prioridad y combinación de ambas ayudas se hablarán con Edgar antes de fijar el primer prototipo.

En el prototipo se propone que Ai'ka se cuide sola y que su daño no produzca derrota de misión. Las necesidades de protección ligadas a escenas de historia se decidirán aparte.

### Ejemplo de presencia y cooperación

Al llegar a un hueco, Ai'ka observa el destino y alcanza una cornisa por una pared lateral. Se reúne con Nebu tras el cruce y comenta lo que encuentran.

En el encuentro siguiente busca una posición protegida. Cuando Nebu necesita el recurso elegido para la prueba, ofrece la ayuda; el jugador acepta, la ve lanzar el objeto y lo recibe mientras continúa la acción. Después Ai'ka sale de cobertura, reacciona al resultado y retoma el camino.

Esta secuencia es un ejemplo propuesto de acompañamiento y entrega. Debe completarse con una situación de creación de paredes y plataformas a partir del uso compartido y del papel de apoyo de Ai'ka. El orden de las pruebas sigue abierto.

### Cómo hacerlo viable

La base propuesta combina navegación por superficies con conexiones preparadas para las maniobras, animación ajustada a cada conexión y una lógica propia de acompañamiento. Las superficies creadas durante la acción necesitan una prueba adicional: qué trayectos permiten y cómo Ai'ka reconoce y utiliza esas oportunidades, según el papel de recorrido y apoyo acordado. Se separan tres decisiones: dónde debe estar, cómo llega y qué puede hacer allí.

Prioridad propuesta: seguridad del recorrido → acción en curso → acompañamiento o apoyo → actividad opcional. Una maniobra iniciada debe terminar o cancelarse de forma controlada antes de elegir otra.

Si una ruta deja de ser válida, se busca una alternativa o un punto de reunión alcanzable. Para un atasco persistente, se puede preparar una recuperación fuera del campo de visión y una entrada visible por una conexión coherente; es un recurso excepcional que se registrará en las pruebas. Un checkpoint restaura también a Ai'ka en una posición segura.

La ralentización del tiempo, cambios de escena y reinicios deben mantenerla en un estado coherente. Los diálogos importantes esperan a que pueda participar o se coordinan con un punto de reunión; el avance común no debe quedar bloqueado por un fallo de navegación.

Unity ofrece conexiones entre superficies navegables y control manual de su recorrido. Son piezas técnicas, no un sistema de compañera ya terminado: [NavMesh Link](https://docs.unity3d.com/Packages/com.unity.ai.navigation@2.0/manual/NavMeshLink.html) y [recorrido manual de conexiones](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/AI.NavMeshAgent.CompleteOffMeshLink.html).

### Referencia de diseño

El manual de BioShock Infinite describe ayudas de Elizabeth con recursos y cambios del entorno; su charla de desarrollo explica la combinación de acompañamiento, liderazgo y comportamiento dinámico y preparado. Tomamos esas funciones como referencia para diseñar una presencia propia de Ai'ka: [manual oficial de 2K](https://assets.2k.com/1a6ngf98576c/59e1MCrJXSmSt2aY7ghTr6/7b60c14b94bd09262d151ef4c90f60d5/BSI_PS3_MANUAL_ENG.pdf) y [charla del desarrollador en GDC](https://www.gdcvault.com/play/1020831/Bringing-BioShock-Infinite-s-Elizabeth).

## 9. Diseño de niveles y encuentros

Cada tramo tiene un destino reconocible, un desafío principal y conexiones que permiten continuar. Las alternativas locales cambian altura, exposición o ángulo de ataque y se vuelven a conectar con el avance principal. El diseño incluye una ruta válida para Ai'ka y lugares de reunión, conversación y apoyo; su recorrido puede diferir del elegido por el jugador.

Un espacio de combate incluye entrada, posiciones útiles, amenazas reconocibles, oportunidades de movimiento y una salida. Una pared transitable o un anclaje debe tener una utilidad verificable durante el encuentro.

Los enfrentamientos obligatorios terminan al resolver las amenazas o cumplir el objetivo indicado. Su condición se explica y la salida comunica que ya se puede continuar. Los encuentros opcionales pueden ofrecer información o una conexión adicional.

### Encuentro de ejemplo: el patio

El destino es un paso hacia la montaña. Un tirador ocupa una plataforma, un guardia protegido vigila el suelo y un atacante cercano se aproxima.

Una solución posible:

1. Deslizarse hacia el lateral y subir mediante una pared transitable.
2. Usar el gancho para alcanzar otra plataforma.
3. Activar la ralentización y disparar al tirador durante un salto.
4. Aterrizar con un ángulo favorable contra el guardia protegido.
5. Resolver la amenaza restante y continuar hacia la salida.

Otra solución empieza por el atacante cercano y utiliza cobertura para flanquear después. Las dos deben ser viables. La secuencia aérea se probará con controles reales antes de construir más encuentros basados en ella.

## 10. Campaña y progresión

La organización propuesta reinterpreta los lugares del original en tres bloques. Su número de escenas y duración definitiva siguen abiertos.

| Bloque | Función en la aventura | Progresión jugable propuesta |
| --- | --- | --- |
| Pueblo y exterior | Presentar a Nebu, Ai'ka y el motivo de avanzar hacia la montaña | Movimiento básico, adquisición de herramientas y primeros ataques combinados con desplazamiento |
| Cueva y posiciones de la Conjunción | Profundizar en el conflicto y descubrir qué ocurre en Nyesa | Combinar altura, funciones de enemigo y poder temporal |
| Encuentro final | Resolver un conflicto del capítulo y cambiar la situación de los personajes | Aplicar las herramientas aprendidas contra una amenaza culminante |

Cada capacidad nueva tiene una presentación, una situación sencilla para practicarla y un encuentro posterior para combinarla. La dificultad crece mediante combinaciones, presión y complejidad del espacio.

### Jefe final propuesto

Se prepara un patrón de ataques reconocible y oportunidades de actuar que requieren cambiar de posición. El jugador observa la preparación, evita el peligro y aprovecha una ventana para avanzar en la resolución.

La verticalidad y los anclajes participan en esas oportunidades. La resolución mediante daño, entorno o una combinación se decidirá con la historia. El jefe debe comprobar habilidades enseñadas antes.

## 11. Interfaz, sonido y presentación

**Interfaz durante la acción:** mirilla, objetivo breve, resistencia del jugador y reserva temporal. El anclaje disponible se reconoce y los impactos válidos se distinguen de los bloqueados. Las ayudas de Ai'ka que requieran una petición muestran una indicación breve únicamente cuando están disponibles.

**Comunicación de enemigos:** señal de preparación de ataque, sonido de disparo o aproximación y respuesta clara al daño. El jugador debe poder identificar la amenaza que acaba de actuar.

**Presentación propuesta:** ciencia ficción vinculada a Nyesa, con el exterior árido, asentamientos y espacios subterráneos como referencias del original. Los poderes tienen una identidad visual reconocible. Las superficies jugables mantienen contraste suficiente para leerlas durante el movimiento.

Las escenas de historia se sitúan en transiciones o momentos preparados. Los efectos de velocidad y cámara se ajustarán para permitir ver, orientarse y apuntar.

## 12. Estilo y recompensa

La recompensa principal es superar una situación y continuar la aventura. Descubrir información, dominar una ruta y aprender a combinar herramientas sostiene el progreso.

El proyecto actual tiene rangos de estilo y recompensas de velocidad. Su integración con el combate se evaluará después de comprobar el encuentro básico. El diseño deberá evitar que una ruta necesaria resulte inviable con un rango bajo.

Una buena combinación puede recibir reconocimiento visual y sonoro. La forma de puntuar, su efecto sobre el movimiento y las actividades secundarias quedan pendientes de probar.

## 13. Primera versión jugable y desarrollo

La campaña descrita es el horizonte del GDD. El prototipo debe comprobar tanto el combate como la presencia física de Ai'ka, y después integrar ambos en un tramo completo.

### Prueba de acompañamiento

Un recorrido corto con un salto, una trepada, una carrera por pared y dos lugares donde el jugador puede detenerse o cambiar de ruta. Ai'ka acompaña, realiza sus maniobras y se reúne de forma visible.

Se comprueban pausa, vuelta atrás, separación, caída, ruta alternativa y reinicio. Después se integra una reacción al entorno y una entrega de objetos en el encuentro de combate. Se comprueban oferta, aceptación, lanzamiento y recepción tanto quieto como en movimiento. El modelo puede ser provisional hasta comprobar la navegación; la sensación final requerirá el personaje y animaciones apropiadas.

### Prueba de creación de superficies

Una zona pequeña con un hueco y un enemigo en otra posición. El jugador elige con el ratón dónde Ai'ka crea una pared y una plataforma, y comprueba una ruta que encadena ambas con el escenario. Se prueban dos formas de completarla: preparar las superficies antes de saltar y crear el siguiente apoyo durante una maniobra aérea. El control exacto, las maniobras de apoyo de Ai'ka y el orden de esta prueba respecto a la entrega de objetos siguen abiertos.

### Prueba de una sala

Una entrada, dos alturas, cobertura, una pared transitable, un anclaje, un tirador, un atacante cercano y una salida. El jugador dispone de las cuatro herramientas propuestas y de un reinicio próximo.

Se busca comprobar un enfrentamiento de aproximadamente un minuto cuando ya se conocen los controles. La duración es una referencia de diseño, no una medición existente.

### Primer tramo completo

Un fragmento de aventura de unos cinco a diez minutos en primera partida: introducción breve, recorrido conjunto con Ai'ka, primer combate con una ayuda suya, una oportunidad de conversación iniciada por el jugador, una combinación nueva, segundo encuentro, consecuencia narrativa y salida hacia el siguiente tramo.

### Orden de trabajo propuesto

1. Revisar integración de ataque y poder temporal, y recursos de personaje y animación para Ai'ka.
2. Preparar la prueba de acompañamiento con seguimiento aliado y conexiones de parkour.
3. Comprobar ritmo, pausas, rutas alternativas y recuperación de Ai'ka.
4. Probar transiciones del jugador y capacidad de apuntar durante ellas.
5. Preparar la sala e integrar tirador y atacante cercano con señales claras.
6. Incorporar la entrega de objetos de Ai'ka: aviso, aceptación, lanzamiento, recepción, efecto y límite de frecuencia.
7. Probar dos soluciones del encuentro, daño y reinicio conjunto.
8. Incorporar el guardia protegido cuando la combinación básica funcione.
9. Convertir lo aprendido en un tramo con avance, compañía e historia.
10. Ampliar el contenido a partir de las pruebas, conservando la identidad acordada.

## 14. Criterios de prueba

| Pregunta | Evidencia que se buscará jugando |
| --- | --- |
| ¿Se entiende hacia dónde avanzar? | El jugador alcanza el destino sin instrucciones constantes |
| ¿El parkour ayuda a combatir? | Usa una pared, altura o anclaje para obtener una ventaja |
| ¿Movimiento y puntería funcionan juntos? | Apunta, ataca y reconoce el resultado durante una maniobra |
| ¿La IA aporta decisiones? | Puede explicar una amenaza y responder cambiando su plan |
| ¿Hay libertad local? | Se completan al menos dos soluciones útiles del encuentro |
| ¿El poder aporta algo? | Se usa en un momento concreto y el combate sigue siendo viable al agotarlo |
| ¿Repetir permite aprender? | El siguiente intento cambia ruta, orden o momento de ataque |
| ¿La aventura da ganas de continuar? | El jugador quiere conocer el siguiente tramo tras completarlo |
| ¿Se siente la compañía de Ai'ka? | Se la ve desplazarse, reunirse y reaccionar en situaciones relevantes |
| ¿Se percibe la evolución de la relación? | El jugador reconoce cambios de diálogo, mirada y comportamiento expresivo tras las conversaciones |
| ¿La relación conserva las prestaciones de apoyo? | En las mismas condiciones, distintos estados de confianza mantienen alcance, recursos, frecuencia y eficacia de las ayudas |
| ¿Su parkour funciona? | Completa salto, trepada y pared con aterrizajes y transiciones coherentes |
| ¿Se adapta al jugador? | Detenerse, regresar o tomar otra ruta mantiene un acompañamiento comprensible |
| ¿Su apoyo tiene utilidad? | El jugador reconoce la oferta, decide aceptarla y recibe un beneficio útil |
| ¿La entrega funciona en movimiento? | Correr, saltar o usar el gancho permite recibir la ayuda sin perder orientación ni bloquear la maniobra |
| ¿Puede ignorarse la oferta? | Continuar sin aceptarla no bloquea el combate ni consume el recurso ofrecido |
| ¿La colocación se entiende? | El jugador reconoce dónde aparecerá la pared o plataforma y la sitúa de forma intencional |
| ¿Se puede improvisar un apoyo en el aire? | El jugador coloca una superficie durante el salto y continúa la ruta de forma controlable |
| ¿Crear superficies aporta una decisión? | La superficie permite una maniobra o posición útil durante el encuentro |
| ¿Los reinicios son fiables? | Ambos personajes recuperan posiciones válidas y el objetivo sigue disponible |

Se registrarán problemas de orientación, cámara, transiciones, lectura de ataques, dificultad y ritmo. Las sensaciones se comprobarán jugando; los archivos y las pruebas técnicas no las demuestran por sí solos.

## 15. Próximas decisiones de diseño

- Confirmar si la propuesta de ganar posición mediante parkour representa la identidad buscada.
- Elegir el papel y la representación definitiva del látigo.
- Probar la relación entre ralentización, movimiento y recarga del poder.
- Decidir el papel del ataque por la espalda y de otras herramientas.
- Concretar el objetivo de Nebu, la primera escena de historia y la ayuda característica de Ai'ka dentro de su papel de compañera presente.
- Concretar qué necesita cada personaje al empezar; escribir las respuestas elegibles de Nebu y las reacciones de Ai'ka, y decidir cómo cada conversación afecta a la confianza.
- Definir los cambios de respuestas, miradas, gestos y movimiento expresivo según la confianza, conservando las mismas reglas de combate y apoyo.
- Escribir un conflicto por una mentira de Nebu: qué descubre Ai'ka, cómo expresa su desacuerdo y qué respuestas o acciones permiten reparar la confianza. Otros motivos de conflicto siguen abiertos.
- Elegir el recurso de la primera entrega de Ai'ka y ajustar su frecuencia, alcance, señales y recepción durante el movimiento.
- Diseñar cómo Ai'ka utiliza el parkour para acompañar y dar apoyo, conservando a Nebu como quien combate.
- Explorar la vista previa sugerida por Edgar para la colocación libre en lugares válidos; concretar orientación, confirmación y criterios de validez.
- Ajustar el alcance amplio de creación de Ai'ka y su relación con la distancia entre personajes, conservando las posibilidades de preparación e improvisación aérea.
- Ajustar los segundos de duración de las superficies y su señal de desaparición; concretar el inicio del tiempo, su relación con la ralentización y qué ocurre si un personaje está utilizándolas. Definir el límite de cantidad manteniendo la posibilidad de encadenar varias.
- Elegir con Edgar el orden de las pruebas de creación de superficies y entrega de objetos.
- Definir las animaciones y el estilo de parkour de Ai'ka, y su relación con los poderes temporales.
- Definir duración, número de niveles y guardado cuando el tramo de prueba aporte referencias reales.

## 16. Referencia revisada y estado del proyecto

**Original:** `D:/Unity/Code-Memory`.
**Build final:** `D:/Unity/Builds de Juegos/Shadows Of Nyesa Final`.

La herramienta de control de aplicaciones falló al iniciarse por un error de permisos de Windows. No se ha jugado ni completado la build. Se utilizó la alternativa autorizada por Edgar: revisar sus datos de niveles y contrastarlos con escenas, prefabs, tutoriales, diálogos y scripts. El original y su build no se han modificado.

La secuencia confirmada es **Level1 → Level2 → Bossfight → créditos**. Los tutoriales muestran disparo, látigo y ralentización. Level1 contiene la adquisición del lanzador y del gancho; Level2 y Bossfight tienen ambas herramientas activadas. El final contiene proyectiles, ojos menores, anclajes elevados y una cinemática. Su funcionamiento real y su resolución exacta están pendientes de probar jugando.

El proyecto actual ofrece una base de movimiento, estilo y navegación de NPC. Incluye estados de patrulla, persecución y una transición de parkour con arco genérico; ParkourLinkData declara tipos de maniobra, pero el estado actual no consume esos datos y regresa a persecución. Hay que desarrollar seguimiento aliado, selección de posición, acciones diferenciadas y recuperación. Esto no es todavía una compañera funcional.

Se han localizado el modelo de Ai'ka y animaciones de reposo y poder en el original. Las animaciones de desplazamiento y parkour, su rig y su compatibilidad están pendientes de revisar. El original sigue sin cambios.

**Prototipos adicionales de Ai'ka revisados en lectura:** D:/Unity/AIKA_IA y D:/Unity/AI'KA IA. En ambos, Assets/Scenes/SampleScene.unity conecta AiKaCombat.cs al objeto AI'KA, con referencias al jugador, un objeto lanzable y una plataforma.

AiKaCombat.cs permite lanzar el objeto hacia la posición del jugador al pulsar H, calculando una trayectoria balística. J crea una plataforma cerca de playerPrediction. Se ha verificado la conexión en los archivos de escena; no se ha ejecutado la prueba. En el script de lanzamiento y el prefab revisados no se encontró una recepción animada, aceptación de oferta ni aplicación de un recurso. Por tanto, hay una base existente de lanzamiento y plataforma; el ciclo completo de ayuda y su integración en la campaña siguen pendientes de verificar y desarrollar. No se han modificado estos prototipos.

Esta revisión corrige la versión 0.6: la intención de entregar objetos viene de Edgar y la creación de plataformas ya tiene una prueba previa. Sus reglas finales de uso y los recursos entregados siguen abiertos. En 0.8 Edgar precisa que pretendía también crear paredes, elegir con el ratón dónde colocar las superficies y conectarlas con el parkour y la ayuda en combate. Esa intención está confirmada por su explicación; la implementación completa no se ha comprobado.

**Diálogos revisados en el proyecto actual:** DialogoNode.cs almacena respuestas enlazadas y distingue los nodos de habla del jugador; DialoguesAssetMenu.cs recorre esas respuestas y el editor permite añadir o quitar enlaces. Esto verifica una base para escribir conversaciones ramificadas. Edgar indica que mejoró el sistema para elegir respuestas.

La DialogueUI.cs revisada avanza al primer nodo hijo al pulsar Siguiente; en ese archivo no se encontró una lista de opciones seleccionables ni efectos sobre la confianza. La elección durante la partida y la conexión con la relación quedan por comprobar; no se ha ejecutado el diálogo. Esta revisión no modifica el sistema existente.

La integración de combate y poder temporal debe revisarse y desarrollarse. La presencia de herramientas o scripts no prueba que la aventura propuesta esté implementada.

## 17. Mantenimiento y registro

Este archivo será el GDD compartido. Las decisiones de Edgar actualizarán las secciones correspondientes y la tabla de acuerdos. Cada versión distinguirá hechos del original, diseño propuesto y funcionamiento comprobado. Las correcciones más recientes sustituyen interpretaciones anteriores.

- **0.1:** propuesta inicial orientada a evasión; corregida en 0.3.
- **0.2:** Edgar elige reinterpretar Shadows of Nyesa conservando mundo y personajes.
- **0.3:** Edgar aclara el papel del combate y del avance; se revisan los niveles de la build final.
- **0.4:** propuesta concreta de ganar posición mediante parkour, con herramientas, enemigos y una sala de prueba.
- **0.5 — 2 de octubre de 2026:** organización como GDD: concepto, acuerdos, historia, ciclo de juego, movimiento, combate y poderes, IA, niveles, campaña, presentación, progreso, desarrollo, criterios de prueba y decisiones pendientes. Las nuevas reglas de equilibrio siguen siendo propuestas.
- **0.6 — 2 de octubre de 2026:** Edgar aclara que Ai'ka debe acompañar físicamente y hacer parkour, con Elizabeth de BioShock Infinite como referencia. Se añade presencia, apoyo contextual, navegación y recuperación; se integra la compañera en el prototipo, el desarrollo y las pruebas. Las ayudas concretas siguen siendo propuestas.
- **0.7 — 2 de octubre de 2026:** Edgar precisa la intención «Booker, catch». Se localiza lanzamiento de objetos y creación de plataformas en las escenas de prueba de dos proyectos de Ai'ka. Se prioriza una entrega visible y aceptada por el jugador, y se distingue la base existente del ciclo completo aún pendiente. El recurso entregado y las reglas de cooperación siguen siendo propuestas.
- **0.8 — 2 de octubre de 2026:** Edgar explica la intención original de crear plataformas y paredes para parkour y ayuda en combate, eligiendo su ubicación con el ratón. Se incorpora al concepto, acuerdos y pruebas; se retira la prioridad asumida de entregar objetos antes que crear superficies. Quedan preguntas sobre quién las recorre y el control de colocación.
- **0.9 — 2 de octubre de 2026:** Edgar aclara que ambos usan las superficies, con Nebu encargado de combatir y Ai'ka como apoyo al estilo de Elizabeth. Sugiere provisionalmente visualizar la colocación. Se actualizan acuerdos y preguntas; la libertad de colocación y las maniobras de apoyo siguen abiertas.
- **0.10 — 2 de octubre de 2026:** Edgar elige colocación libre en casi cualquier sitio válido. Se conserva la vista previa como idea provisional y quedan por definir validez, cantidad de superficies activas y duración.
- **0.11 — 2 de octubre de 2026:** Edgar elige varias paredes y plataformas para encadenar una ruta. Se incorpora esa combinación a la creación y su prueba; cantidad máxima, duración y retirada siguen abiertas.
- **0.12 — 2 de octubre de 2026:** Edgar elige que paredes y plataformas desaparezcan después de unos segundos. Se registra la duración temporal como acuerdo; segundos exactos y reglas de desaparición siguen pendientes de probar.
- **0.13 — 2 de octubre de 2026:** Edgar confirma que se pueden preparar superficies antes de saltar y crearlas también en pleno salto. Se incorporan ambas formas de jugar a la prueba de creación; controles y tiempo de respuesta siguen pendientes.
- **0.14 — 4 de octubre de 2026:** Edgar aclara que Ai'ka crea superficies con un rango alto y debe mantenerse relativamente cerca. Se registra el alcance amplio; medida exacta y reglas de alcance siguen pendientes. Se propone reposicionamiento para mantener el apoyo durante el recorrido.
- **0.15 — 4 de octubre de 2026:** Edgar elige una relación que empieza por necesidad y mejora durante la aventura, con conversaciones iniciadas por el jugador que permiten acercarse a Ai'ka. Se incorpora a historia, compañía y primera prueba; sistema de confianza y consecuencias concretas siguen abiertos.
- **0.16 — 4 de octubre de 2026:** Edgar confirma las respuestas elegibles de Nebu e indica que ha mejorado el diálogo para ello. Se localiza la base de ramificaciones en datos y editor; selección durante la partida y efectos sobre la confianza siguen por comprobar. Consecuencias de las respuestas pendientes de diseñar.
- **0.17 — 4 de octubre de 2026:** Edgar permite que las respuestas empeoren la relación y limita sus efectos al diálogo, miradas y comportamiento expresivo. Combate, poderes y prestaciones del apoyo mantienen las mismas reglas. Se corrigen las preguntas abiertas que contemplaban beneficios o penalizaciones jugables por confianza.
- **0.18 — 4 de octubre de 2026:** Edgar define que Ai'ka dice directamente lo que piensa cuando se molesta. Se revisan los ejemplos de confianza para reflejar esa expresión del desacuerdo; motivos, tono y resolución de conflictos siguen pendientes.
- **0.19 — 4 de octubre de 2026:** Edgar elige que las mentiras de Nebu sean un motivo importante de conflicto para Ai'ka. Se incorpora la sinceridad a la relación; descubrimiento de la mentira y recuperación de confianza siguen pendientes. Los efectos conservan el límite de expresión y diálogo acordado.
- **0.20 — 4 de octubre de 2026:** Edgar descarta la propuesta de descubrir más adelante una mentira que parecía funcionar. Se conserva la importancia de la sinceridad y la expresión directa de Ai'ka; la escritura de cada conversación sigue pendiente.
