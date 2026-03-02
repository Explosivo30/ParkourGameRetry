# 🚀 Sistema Avanzado de Localización - Guía Completa

## 📦 Nuevas Características Implementadas

### ✅ 1. Sistema de IDs Centralizado
- Base de datos única para todas las traducciones
- Reutilización de textos comunes
- Búsqueda y organización por categorías

### ✅ 2. Validación Automática
- Detecta traducciones faltantes
- Identifica textos que exceden límites de caracteres
- Reportes de progreso por idioma

### ✅ 3. Variables en Textos
- Soporte para textos dinámicos
- Ejemplo: "Hola {playerName}, tienes {gold} monedas"
- Seguridad contra errores de formato

### ✅ 4. Integración con Google Sheets
- Sincronización bidireccional
- Traductores trabajan en tiempo real
- Sin enviar archivos de ida y vuelta

---

## 📚 Índice de Contenidos

1. [Configuración Inicial](#configuración-inicial)
2. [Sistema de IDs](#sistema-de-ids)
3. [Validación de Traducciones](#validación)
4. [Variables en Textos](#variables)
5. [Google Sheets](#google-sheets)
6. [Flujos de Trabajo](#flujos-de-trabajo)
7. [Resolución de Problemas](#problemas)

---

## 🔧 Configuración Inicial

### Archivos Nuevos a Añadir

**Scripts Core:**
- `LocalizationDatabase.cs` → `/Scripts/Dialogo/`
- `LocalizationValidator.cs` → `/Scripts/Dialogo/`
- `GoogleSheetsIntegration.cs` → `/Scripts/Dialogo/`
- `LocalizedString.cs` → **REEMPLAZA** el existente

**Scripts del Editor:**
- `LocalizationDatabaseEditor.cs` → `/Scripts/Dialogo/Editor/`
- `LocalizationValidatorWindow.cs` → `/Scripts/Dialogo/Editor/`
- `GoogleSheetsWindow.cs` → `/Scripts/Dialogo/Editor/`

### Crear la Base de Datos de Localización

1. Clic derecho en Project → `Create → Torbellino Studio → Localization Database`
2. Nómbrala `MainLocalizationDB`
3. Guárdala en `/Assets/Data/Localization/`

### Configurar LocalizationManager

Ya lo tienes configurado, solo asegúrate de que tenga todos los idiomas que vas a usar.

---

## 🆔 Sistema de IDs

### ¿Qué es un ID de Localización?

En lugar de almacenar el texto directamente en cada nodo de diálogo, ahora puedes usar **IDs reutilizables**.

#### Ejemplo Antiguo (Sin IDs):
```
Nodo 1: "¡Bienvenido a la tienda!"
Nodo 2: "¡Bienvenido a la tienda!"
Nodo 3: "¡Bienvenido a la tienda!"
```
❌ Problema: Cambiar el saludo = editar 3 nodos

#### Ejemplo Nuevo (Con IDs):
```
ID: SHOP_GREETING = "¡Bienvenido a la tienda!"
Nodo 1 → SHOP_GREETING
Nodo 2 → SHOP_GREETING
Nodo 3 → SHOP_GREETING
```
✅ Ventaja: Cambiar el saludo = editar 1 ID

### Crear un ID de Localización

#### Opción A: Desde la Base de Datos

1. Abre `MainLocalizationDB` en el Inspector
2. Baja hasta "Añadir Nueva Entrada"
3. Escribe un ID (ej: `QUEST_BLACKSMITH_START`)
4. Clic en "➕ Crear"
5. Escribe el texto en todos los idiomas

#### Opción B: Generar ID Automático

1. En el campo ID, escribe un prefijo (ej: `QUEST_BLACKSMITH`)
2. Clic en "Generar ID"
3. Te dará algo como `QUEST_BLACKSMITH_1`, `QUEST_BLACKSMITH_2`, etc.

### Convención de Nombres de IDs

```
CATEGORÍA_SUBCATEGORÍA_DESCRIPCIÓN

Ejemplos:
UI_BUTTON_ACCEPT      → Botón "Aceptar" en UI
UI_BUTTON_CANCEL      → Botón "Cancelar" en UI
QUEST_SHOP_START      → Inicio de misión de tienda
QUEST_SHOP_COMPLETE   → Completar misión de tienda
NPC_GUARD_GREETING    → Saludo del guardia
TUTORIAL_MOVEMENT     → Tutorial de movimiento
ERROR_SAVE_FAILED     → Error al guardar
```

### Usar IDs en Código

```csharp
using Dialogo;

public class MyScript : MonoBehaviour
{
    [SerializeField] LocalizationDatabase database;
    
    void Start()
    {
        // Obtener texto simple
        string greeting = database.GetText("SHOP_GREETING");
        Debug.Log(greeting); // "¡Bienvenido a la tienda!"
        
        // Obtener texto con variables
        string message = database.GetText("PLAYER_GOLD", playerName, goldAmount);
        Debug.Log(message); // "Juan, tienes 150 monedas"
    }
}
```

### Organizar con Categorías

Añade categorías para organizar mejor:

```
Categoría: "UI"
- UI_BUTTON_ACCEPT
- UI_BUTTON_CANCEL
- UI_MENU_OPTIONS

Categoría: "Quests"
- QUEST_SHOP_START
- QUEST_SHOP_PROGRESS
- QUEST_SHOP_COMPLETE

Categoría: "NPCs"
- NPC_GUARD_GREETING
- NPC_MERCHANT_GREETING
```

En el Inspector de la base de datos puedes filtrar por categoría.

---

## ✔️ Validación de Traducciones

### Abrir el Validador

`Window → Localization Validator`

### Pestaña "Validación"

Aquí verás todos los problemas encontrados:

#### Tipos de Problemas:

**⛔ CRÍTICOS (deben corregirse):**
- Traducción faltante en idioma principal
- IDs duplicados
- IDs vacíos

**⚠️ ADVERTENCIAS (deberían revisarse):**
- Traducciones faltantes en idiomas secundarios
- Textos que exceden límite de caracteres
- Formato de variables inválido

**ℹ️ INFORMACIÓN:**
- Falta contexto para traductores

### Filtros Útiles

- **Solo con problemas**: Oculta entradas perfectas
- **Solo críticos**: Muestra solo errores graves

### Pestaña "Estadísticas"

Muestra el progreso de traducción por idioma:

```
🇪🇸 Español
████████████████████ 100% (250/250)
Palabras: 5,432 | Caracteres: 28,901

🇬🇧 English
███████████████░░░░░ 87% (218/250)
Palabras: 4,721 | Caracteres: 25,103

🇫🇷 Français
████████░░░░░░░░░░░░ 45% (113/250)
Palabras: 2,156 | Caracteres: 11,234
```

### Pestaña "Diálogos"

Valida todos los archivos de diálogo del proyecto.

Botón: **"Validar Todos los Diálogos"**

Muestra:
- Diálogos con problemas
- Nodos específicos que faltan traducir
- Botón para abrir cada diálogo directamente

### Automatizar Validación

Puedes ejecutar validación desde código:

```csharp
var results = LocalizationValidator.ValidateDatabase(database);

foreach (var result in results)
{
    if (result.HasCriticalIssues)
    {
        Debug.LogError($"Entrada '{result.entry.localizationID}' tiene errores críticos");
    }
}
```

---

## 🔤 Variables en Textos

### ¿Qué son las Variables?

Permiten insertar valores dinámicos en textos localizados.

### Activar Soporte de Variables

1. En la entrada de localización, marca "Variables"
2. Usa `{0}`, `{1}`, `{2}` en el texto

### Ejemplo Básico

**Entrada ID:** `PLAYER_GOLD`

**Texto (ES):** `Tienes {0} monedas`  
**Texto (EN):** `You have {0} coins`

**Código:**
```csharp
int goldAmount = 150;
string message = database.GetText("PLAYER_GOLD", goldAmount);
// Resultado: "Tienes 150 monedas"
```

### Múltiples Variables

**Entrada ID:** `QUEST_PROGRESS`

**Texto (ES):** `{0}, has completado {1} de {2} objetivos`  
**Texto (EN):** `{0}, you've completed {1} of {2} objectives`

**Código:**
```csharp
string message = database.GetText("QUEST_PROGRESS", playerName, completed, total);
// Resultado: "Juan, has completado 3 de 5 objetivos"
```

### Variables con Nombres (Avanzado)

Si usas .NET 4.x o superior, puedes usar nombres:

**Texto (ES):** `Hola {playerName}, nivel {level}`

**Código:**
```csharp
// Requiere interpolación personalizada
string message = database.GetText("GREETING")
    .Replace("{playerName}", player.name)
    .Replace("{level}", player.level.ToString());
```

### Validación de Variables

El validador detecta:

```
✅ Correcto:
ES: "Tienes {0} monedas"
EN: "You have {0} coins"

❌ Error (llaves no cerradas):
ES: "Tienes {0 monedas"
       
⚠️ Advertencia (variables inconsistentes):
ES: "Tienes {0} monedas y {1} gemas"
EN: "You have {0} coins"  ← Falta {1}
```

### Ejemplos Comunes

```csharp
// Saludo personalizado
"GREETING" = "¡Hola {0}!"
database.GetText("GREETING", playerName);

// Información de combate
"DAMAGE_DEALT" = "Has causado {0} de daño a {1}"
database.GetText("DAMAGE_DEALT", damage, enemyName);

// Economía
"PURCHASE_SUCCESS" = "Has comprado {0} por {1} monedas"
database.GetText("PURCHASE_SUCCESS", itemName, price);

// Progreso
"LEVEL_UP" = "¡Nivel {0} alcanzado! Siguiente nivel: {1} XP"
database.GetText("LEVEL_UP", level, xpNeeded);

// Tiempo
"TIME_REMAINING" = "Quedan {0} minutos y {1} segundos"
database.GetText("TIME_REMAINING", minutes, seconds);
```

---

## 📊 Google Sheets Integration

### Ventajas

✅ Traductores trabajan online  
✅ Colaboración en tiempo real  
✅ Historial automático de cambios  
✅ Sin enviar archivos CSV de ida y vuelta  
✅ Compatible con Google Translate addon  

### Configuración (Una sola vez)

#### Paso 1: Crear Hoja de Google

1. Ve a [Google Sheets](https://sheets.google.com)
2. Crea una nueva hoja
3. Nómbrala: `[TuJuego] - Localization`
4. Copia la URL completa

#### Paso 2: Obtener API Key

1. Ve a [Google Cloud Console](https://console.cloud.google.com)
2. Crea un proyecto nuevo (ej: "MyGameLocalization")
3. Ve a `APIs y servicios → Biblioteca`
4. Busca "Google Sheets API" y haz clic en "Habilitar"
5. Ve a `APIs y servicios → Credenciales`
6. Clic en "Crear credenciales" → "Clave de API"
7. Copia la API Key (algo como `AIzaSyD...`)

**💡 TIP:** Restringe la API Key para mayor seguridad:
- Clic en la key → "Editar"
- Restricciones de API → Selecciona "Google Sheets API"

#### Paso 3: Configurar Permisos

1. En tu hoja de Google Sheets, clic en "Compartir"
2. Cambia a: "Cualquiera con el enlace puede **editar**"
3. Esto permite que Unity lea y escriba

#### Paso 4: Configurar en Unity

1. `Window → Google Sheets Integration`
2. Clic en "Crear Nueva Configuración"
3. Guarda el asset como `GoogleSheetsConfig`
4. Pega la **URL completa** de tu hoja
5. Pega la **API Key**
6. Selecciona tu `LocalizationDatabase`

### Uso Diario

#### Exportar a Google Sheets

**Cuándo:** La primera vez, o cuando añadas nuevas entradas

1. `Window → Google Sheets Integration`
2. Clic en "⬆️ Exportar a Google Sheets"
3. Confirma (esto sobrescribirá la hoja)
4. Espera unos segundos
5. ¡Listo! Tus traductores ya pueden trabajar

#### Importar desde Google Sheets

**Cuándo:** Después de que los traductores hayan añadido traducciones

1. `Window → Google Sheets Integration`
2. Clic en "⬇️ Importar desde Google Sheets"
3. Confirma (esto sobrescribirá tus datos en Unity)
4. ¡Traducciones actualizadas!

### Flujo de Trabajo Recomendado

```
DÍA 1 (Tú):
1. Crear IDs y textos en español
2. Exportar a Google Sheets
3. Enviar enlace a traductores

DÍA 2-7 (Traductores):
- Trabajan directamente en Google Sheets
- Rellenan columnas English, French, etc.

DÍA 8 (Tú):
1. Importar desde Google Sheets
2. Validar con Localization Validator
3. Probar en el juego
4. Si hay correcciones, volver a exportar
```

### Columnas de la Hoja

La hoja exportada tendrá:

| ID | Categoría | Contexto | Max Caracteres | Variables | Spanish | English | Catalan | ... |
|----|-----------|----------|----------------|-----------|---------|---------|---------|-----|
| SHOP_GREETING | UI | Saludo al entrar a tienda | 50 | NO | ¡Bienvenido! | Welcome! | Benvingut! | ... |
| PLAYER_GOLD | UI | Muestra oro del jugador | 0 | SÍ | Tienes {0} monedas | You have {0} coins | Tens {0} monedes | ... |

**Instrucciones para traductores:**
- ✅ **EDITAR:** Columnas de idiomas (Spanish, English, etc.)
- ✅ Respetar variables `{0}`, `{1}`, etc.
- ❌ **NO TOCAR:** ID, Categoría, Contexto, Max Caracteres, Variables

### Auto-actualización (Opcional)

En la configuración:

```
☑ Actualizar al iniciar Unity
Intervalo auto-actualización: 30 minutos
```

Esto descargará traducciones automáticamente cada 30 minutos.

**⚠️ Cuidado:** Si varios programadores trabajan, pueden pisarse cambios.

### Solución de Problemas

**"Error 403: The caller does not have permission"**
→ Asegúrate de que la hoja está en "Cualquiera con el enlace puede editar"

**"Error 400: Unable to parse range"**
→ Verifica que el nombre de la pestaña sea correcto (por defecto "Traducciones")

**"No se importan algunos textos"**
→ Los traductores deben guardar después de editar (Ctrl+S en Google Sheets)

**"Las variables {0} no funcionan"**
→ Asegúrate de marcar "Variables" en la entrada de localización

---

## 🔄 Flujos de Trabajo

### Flujo 1: Juego Pequeño (Solo tú)

```
1. Crear IDs en LocalizationDatabase
2. Escribir español
3. Cambiar idioma del editor a English
4. Escribir inglés
5. Repetir para cada idioma
6. Validar con Localization Validator
7. Probar en el juego
```

**Tiempo estimado:** 1-2 horas para ~100 entradas

### Flujo 2: Con Traductor Externo (CSV)

```
TÚ:
1. Crear IDs y español en Unity
2. Exportar CSV desde Dialog Editor o LocalizationDatabase
3. Enviar CSV al traductor

TRADUCTOR:
4. Abrir CSV en Excel/Google Sheets
5. Rellenar columnas de idiomas
6. Devolver CSV

TÚ:
7. Importar CSV en Unity
8. Validar
9. Probar
```

**Tiempo estimado:** 
- Preparación: 30 min
- Traducción: 1-5 días (según traductor)
- Revisión: 1 hora

### Flujo 3: Con Google Sheets (Recomendado)

```
TÚ (Setup inicial - solo 1 vez):
1. Configurar Google Cloud API
2. Crear hoja de Google Sheets
3. Configurar en Unity

TÚ (Por cada actualización):
1. Crear IDs y español
2. Exportar a Google Sheets (1 clic)
3. Enviar enlace a traductores

TRADUCTORES (Trabajan en paralelo):
4. Abren la hoja
5. Traducen en tiempo real
6. Tú ves el progreso en vivo

TÚ (Cuando terminen):
7. Importar desde Google Sheets (1 clic)
8. Validar
9. Probar
```

**Tiempo estimado:**
- Setup inicial: 15-30 min
- Por actualización: 5 min
- Traducción: Continua
- Sin esperas ni emails

### Flujo 4: Proyecto Grande (Equipo)

```
DISEÑADOR DE NIVELES:
1. Crea diálogos en español en Dialog Editor
2. Usa IDs de LocalizationDatabase cuando sea posible

ESCRITOR:
3. Revisa textos en español
4. Crea IDs nuevos según convención
5. Añade contexto para traductores

PROGRAMADOR:
6. Exporta a Google Sheets semanalmente
7. Ejecuta validación antes de builds

TRADUCTORES (3-5 personas):
8. Trabajan en Google Sheets
9. Usan filtros para dividirse el trabajo
10. Marcan filas completadas

QA TESTER:
11. Cambia idiomas en el juego
12. Reporta textos cortados o errores
```

---

## ⚠️ Problemas Comunes

### "No veo mis IDs en los diálogos"

**Problema:** Los nodos todavía usan el sistema antiguo (LocalizedString directo)

**Solución:** 
- Opción A: Migrar manualmente cada nodo
- Opción B: Seguir usando el sistema antiguo para diálogos existentes
- Opción C: Usar IDs solo para textos de UI reutilizables

### "El validador dice que falta traducción pero yo la veo"

**Posible causa:** Espacios en blanco o saltos de línea

**Solución:**
```csharp
// Antes
text = "   "; // Se considera vacío

// Después
text = "Texto real"; // OK
```

### "Las variables {0} no se reemplazan"

**Checklist:**
- [ ] ¿Marcaste "Variables" en la entrada?
- [ ] ¿Estás pasando el número correcto de argumentos?
- [ ] ¿El código llama a `GetText(id, var1, var2)`?

```csharp
// ❌ Mal
string text = database.GetText("PLAYER_GOLD");

// ✅ Bien
string text = database.GetText("PLAYER_GOLD", goldAmount);
```

### "Google Sheets no sincroniza"

**Checklist:**
- [ ] ¿La hoja está pública o con enlace compartido?
- [ ] ¿La API Key es correcta?
- [ ] ¿Habilitaste "Google Sheets API" en Google Cloud?
- [ ] ¿El ID del spreadsheet es correcto?

**Probar API manualmente:**
```
https://sheets.googleapis.com/v4/spreadsheets/TU_SPREADSHEET_ID/values/Traducciones!A1:B2?key=TU_API_KEY
```

Pega esto en el navegador. Debería devolver JSON con datos de la hoja.

### "Algunos textos se ven raros en el juego"

**Causa común:** Codificación UTF-8

**Solución:**
1. Asegúrate de que Unity use UTF-8
2. Al exportar/importar CSV, usa UTF-8
3. En Google Sheets, automáticamente usa UTF-8

### "Perdí traducciones al importar"

**Prevención:**
- Siempre haz backup antes de importar
- Usa control de versiones (Git) para los assets
- La primera vez, exporta → importa inmediatamente para probar

**Recuperación:**
- Si usas Git: `git checkout -- MainLocalizationDB.asset`
- Si no: Busca en `Assets/.Trash/` (Unity guarda algunos backups)

---

## 📊 Estadísticas del Sistema

### Capacidad

- **IDs:** Ilimitados (práctica: hasta ~10,000 sin problemas)
- **Idiomas:** Ilimitados (recomendado: 3-10)
- **Caracteres por entrada:** Ilimitado
- **Variables por texto:** Hasta 20 (recomendado: 1-5)

### Performance

- **Búsqueda de ID:** O(1) - Instantánea (usa diccionario)
- **Carga inicial:** ~50ms para 1,000 entradas
- **Memoria:** ~1KB por entrada completa con 5 idiomas

### Tamaños Típicos

```
Proyecto pequeño (indie):
- 200-500 IDs
- 3 idiomas
- ~100KB asset

Proyecto mediano:
- 1,000-3,000 IDs
- 5 idiomas
- ~500KB asset

Proyecto grande (AAA):
- 10,000+ IDs
- 10+ idiomas
- ~5-10MB asset
- Considerar dividir en múltiples bases de datos
```

---

## 🎓 Mejores Prácticas

### Convenciones de Nombrado

```csharp
// ✅ BIEN
UI_BUTTON_ACCEPT
NPC_BLACKSMITH_GREETING
QUEST_DRAGON_START

// ❌ MAL
accept_button  // Minúsculas
Blacksmith greeting  // Espacios
q_dragon  // Abreviación poco clara
```

### Organización

```
UI/
├── Buttons
│   ├── UI_BUTTON_ACCEPT
│   ├── UI_BUTTON_CANCEL
│   └── UI_BUTTON_CONFIRM
├── Menus
│   ├── UI_MENU_OPTIONS
│   └── UI_MENU_PAUSE
└── Tooltips
    └── UI_TOOLTIP_HEALTH

Quests/
├── Main
│   ├── QUEST_MAIN_01_START
│   └── QUEST_MAIN_01_COMPLETE
└── Side
    ├── QUEST_SIDE_BLACKSMITH_START
    └── QUEST_SIDE_BLACKSMITH_COMPLETE
```

### Contexto para Traductores

```
❌ Sin contexto:
ID: TRUNK
Texto: "Trunk"
→ ¿Tronco de árbol? ¿Baúl? ¿Maletero de coche?

✅ Con contexto:
ID: TRUNK
Contexto: "Container where player stores items. Like a treasure chest."
Texto: "Trunk" → "Baúl"
```

### Límites de Caracteres

```
Establece límites para UI:

UI_BUTTON_TEXT: 15 chars
UI_NOTIFICATION: 50 chars
UI_MENU_DESCRIPTION: 100 chars
DIALOG_LINE: Sin límite
```

El validador te avisará si algún idioma excede el límite.

### Variables Claras

```
❌ Poco claro:
"Damage: {0} {1}" → ¿Qué es {0}? ¿Y {1}?

✅ Documentado:
Contexto: "{0} = damage amount (number), {1} = damage type (Fire/Ice/etc)"
"Damage: {0} {1}"
```

---

## 🚀 Siguientes Pasos

### Corto Plazo (Esta semana)

1. [ ] Crear LocalizationDatabase
2. [ ] Migrar textos comunes de UI a IDs
3. [ ] Configurar Google Sheets
4. [ ] Primera exportación

### Medio Plazo (Este mes)

1. [ ] Validar todas las traducciones
2. [ ] Establecer convención de IDs en equipo
3. [ ] Documentar proceso para traductores
4. [ ] Crear categorías organizadas

### Largo Plazo (Próximos meses)

1. [ ] Audio localizado (voces)
2. [ ] Imágenes localizadas (UI con texto)
3. [ ] Pluralización inteligente
4. [ ] Machine translation para borradores

---

## 🆘 Soporte

### Recursos

- **Documentación Unity:** [Localization Package](https://docs.unity3d.com/Packages/com.unity.localization@latest)
- **Google Sheets API:** [Guía oficial](https://developers.google.com/sheets/api/guides/concepts)
- **Validador de JSON:** [jsonlint.com](https://jsonlint.com/)

### Contacto

Si encuentras bugs o tienes sugerencias:
1. Revisa esta documentación primero
2. Prueba el validador
3. Verifica la configuración de Google Sheets
4. Consulta la sección de problemas comunes

---

## ✅ Checklist Pre-Release

Antes de lanzar el juego:

- [ ] Todas las entradas validadas sin errores críticos
- [ ] Al menos 95% de traducciones completas en idiomas principales
- [ ] Todos los textos probados en UI (no se cortan)
- [ ] Variables funcionan correctamente en runtime
- [ ] Google Sheets backup guardado
- [ ] Exportar CSV final como backup
- [ ] Testeo en todos los idiomas soportados
- [ ] Caracteres especiales (ñ, ç, ü) se muestran correctamente

---

¡Buena suerte con tu proyecto localizado! 🌍🎮
