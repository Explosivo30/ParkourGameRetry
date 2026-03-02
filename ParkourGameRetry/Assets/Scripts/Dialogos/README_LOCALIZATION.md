# Sistema de Localización para Diálogos - Guía de Uso

## 📋 Resumen

Este sistema permite traducir todos los diálogos del juego a múltiples idiomas de forma fácil, sin necesidad de programar.

---

## 🎯 Características

✅ **Editar traducciones directamente en Unity** - Cambia de idioma en el editor y escribe las traducciones  
✅ **Exportar/Importar CSV** - Comparte archivos con traductores externos  
✅ **Sin programación** - Cualquier persona puede traducir los textos  
✅ **Cambio de idioma en tiempo real** - Los jugadores pueden cambiar el idioma en el menú  
✅ **Idiomas ilimitados** - Añade tantos idiomas como necesites  

---

## 🚀 Configuración Inicial (Solo una vez)

### 1. Crear el Gestor de Localización

1. En la escena principal (o una escena que siempre se cargue):
   - Clic derecho en la Jerarquía → Create Empty
   - Nómbralo `LocalizationManager`
   - Añade el componente `LocalizationManager.cs`

2. Configurar idiomas soportados:
   - En el Inspector, verás una lista `Supported Languages`
   - Por defecto tiene: Spanish, English, Catalan, French, German
   - Puedes añadir o quitar idiomas según necesites

3. Seleccionar idioma inicial:
   - En `Current Language`, elige el idioma por defecto (Spanish)

---

## ✍️ Cómo Traducir Diálogos

### Método 1: Editar Directamente en Unity (Recomendado)

1. **Abrir el Editor de Diálogos**:
   - Ve a `Window → Editor Dialogo`
   - Selecciona tu archivo de diálogo en el Project

2. **Cambiar el idioma de edición**:
   - En la barra superior, verás `Idioma de Edición: [Dropdown]`
   - Selecciona el idioma que quieres traducir (ej: English)

3. **Escribir la traducción**:
   - Ahora todos los nodos mostrarán el texto en ese idioma
   - Escribe las traducciones directamente en las cajas de texto
   - En la esquina de cada nodo verás `[EN]`, `[ES]`, etc. indicando el idioma actual

4. **Verificar que todos los nodos tienen todos los idiomas**:
   - Clic en el botón `Verificar Idiomas` en la barra superior
   - Esto crea campos vacíos para cualquier idioma que falte

### Método 2: Usando CSV (Para Traductores Externos)

#### Exportar

1. En el Editor de Diálogos, clic en `Exportar CSV`
2. Guarda el archivo (ej: `Dialogo_NPCTienda_localization.csv`)
3. Envía este archivo a tu traductor

#### Formato del CSV

```csv
NodeID,Field,IsPlayerSpeaking,Spanish,English,Catalan
abc123,SpeakerName,False,"Comerciante","Merchant","Comerciant"
abc123,Dialogue,False,"¡Bienvenido a mi tienda!","Welcome to my shop!","Benvingut a la meva botiga!"
```

- **NodeID**: Identificador único del nodo (no tocar)
- **Field**: Si es el nombre del personaje o el diálogo (no tocar)
- **IsPlayerSpeaking**: Si habla el jugador o NPC (no tocar)
- **Spanish, English, etc.**: Las columnas de cada idioma (EDITAR AQUÍ)

#### Importar

1. Una vez traducido el CSV, vuelve a Unity
2. En el Editor de Diálogos, clic en `Importar CSV`
3. Selecciona el archivo traducido
4. ¡Listo! Todas las traducciones se aplicarán automáticamente

---

## 🎮 Permitir al Jugador Cambiar el Idioma

### Opción A: Dropdown en el Menú

1. Crea un Canvas UI (si no tienes)
2. Añade un `Dropdown (TMP)` al Canvas
3. En el GameObject con el Dropdown:
   - Añade el componente `LanguageSelectorUI.cs`
   - Arrastra el Dropdown al campo `Language Dropdown`

¡Ya está! El dropdown se llenará automáticamente con los idiomas disponibles.

### Opción B: Botones Individuales

1. Crea botones en tu menú: `[Español]` `[English]` `[Català]`
2. En un GameObject:
   - Añade el componente `LanguageSelectorUI.cs`
   - Arrastra cada botón a su campo correspondiente:
     - `Spanish Button` → botón de Español
     - `English Button` → botón de English
     - Etc.

---

## 📝 Consejos y Buenas Prácticas

### Para Diseñadores/Escritores:

1. **Escribe primero en tu idioma principal** (español)
2. **Luego cambia al siguiente idioma** y traduce
3. **Usa "Verificar Idiomas"** antes de exportar a CSV
4. **Exporta CSV regularmente** como backup de traducciones

### Para Traductores Externos:

1. **No modifiques** las columnas NodeID, Field, IsPlayerSpeaking
2. **Solo edita** las columnas de idiomas (Spanish, English, etc.)
3. **Respeta los saltos de línea** marcados como `\n`
4. **Usa comillas dobles** para textos con comas: `"Hola, mundo"`
5. **Salva el archivo como UTF-8** para caracteres especiales (ñ, ç, etc.)

### Atajos de Teclado en Excel/Google Sheets:

- **Alt + Enter** (Windows) / **Cmd + Enter** (Mac): Salto de línea dentro de una celda
- Esto es útil para diálogos largos con múltiples párrafos

---

## 🔧 Solución de Problemas

### "Los diálogos no cambian cuando cambio el idioma en el juego"

**Solución**: Asegúrate de que:
1. Tienes un `LocalizationManager` en la escena
2. El `DialogueUI` se actualiza cuando cambia el idioma:

```csharp
private void OnEnable()
{
    LocalizationManager.OnLanguageChanged += OnLanguageChanged;
}

private void OnDisable()
{
    LocalizationManager.OnLanguageChanged -= OnLanguageChanged;
}

private void OnLanguageChanged(SystemLanguage newLanguage)
{
    UpdateUI(); // Vuelve a mostrar el texto actual
}
```

### "Algunos nodos no tienen texto en ciertos idiomas"

**Solución**: 
1. Abre el Editor de Diálogos
2. Clic en `Verificar Idiomas`
3. Cambia al idioma que falta
4. Rellena los campos vacíos

### "El CSV no se importa correctamente"

**Solución**:
1. Verifica que el archivo está guardado como UTF-8
2. No uses Excel si tiene problemas - usa Google Sheets o LibreOffice
3. Asegúrate de no haber cambiado las primeras 3 columnas

---

## 🌍 Añadir Nuevos Idiomas

1. **En Unity**:
   - Selecciona el `LocalizationManager` en la escena
   - En `Supported Languages`, aumenta el tamaño
   - Añade el nuevo idioma (ej: SystemLanguage.Japanese)

2. **En el Editor**:
   - Abre cualquier diálogo
   - Clic en `Verificar Idiomas`
   - Todos los nodos ahora tendrán campos para el nuevo idioma

3. **Traducir**:
   - Cambia `Idioma de Edición` al nuevo idioma
   - Escribe las traducciones
   - O exporta CSV y envíalo a un traductor

---

## 📦 Archivos del Sistema

- `LocalizedString.cs` - Almacena texto en múltiples idiomas
- `LocalizationManager.cs` - Controla el idioma actual del juego
- `DialogEditorSettings.cs` - Guarda el idioma del editor
- `DialogoNode.cs` - Nodo de diálogo con soporte multi-idioma
- `DialogEditor.cs` - Editor visual con selector de idioma
- `DialoguesAssetMenu.cs` - Asset del diálogo con import/export CSV
- `LanguageSelectorUI.cs` - UI para cambiar idioma en el juego

---

## ✅ Checklist de Integración

- [ ] `LocalizationManager` añadido a la escena principal
- [ ] Idiomas configurados en `Supported Languages`
- [ ] Todos los diálogos verificados con `Verificar Idiomas`
- [ ] Traducciones escritas o importadas desde CSV
- [ ] UI de selección de idioma añadida al menú
- [ ] `DialogueUI` se actualiza al cambiar idioma
- [ ] Probado cambiando idiomas en tiempo de ejecución

---

## 🎉 ¡Listo!

Ahora tienes un sistema completo de localización que:
- ✅ Cualquier persona puede usar sin programar
- ✅ Funciona con traductores externos
- ✅ Permite cambiar idiomas en tiempo real
- ✅ Es escalable a cualquier número de idiomas

**¿Preguntas?** Consulta el código o busca `// TODO:` en los archivos para posibles mejoras futuras.
