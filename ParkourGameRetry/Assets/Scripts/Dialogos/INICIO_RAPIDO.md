# 🌍 Sistema de Localización para Diálogos - Inicio Rápido

## ¿Qué hace este sistema?

Permite que cualquier persona (sin saber programar) traduzca todos los diálogos del juego a múltiples idiomas.

---

## 📥 Instalación (5 pasos)

### 1. Copiar los archivos a tu proyecto

Coloca estos archivos en tu carpeta de Scripts:

**Scripts principales:**
- `LocalizedString.cs` → Carpeta: `/Scripts/Dialogo/`
- `LocalizationManager.cs` → Carpeta: `/Scripts/Dialogo/`
- `DialogoNode.cs` → **REEMPLAZA** el que ya tienes
- `DialoguesAssetMenu.cs` → **REEMPLAZA** el que ya tienes

**Editor:**
- `DialogEditor.cs` → Carpeta: `/Scripts/Dialogo/Editor/` (reemplaza el existente)
- `DialogEditorSettings.cs` → Carpeta: `/Scripts/Dialogo/Editor/`

**UI (opcional):**
- `LanguageSelectorUI.cs` → Carpeta: `/Scripts/UI/`
- `DialogueUI.cs` → Carpeta: `/Scripts/UI/` (reemplaza si ya tienes uno)

### 2. Crear el Gestor de Idiomas en tu escena

1. En la escena principal → Clic derecho → Create Empty
2. Nómbralo `LocalizationManager`
3. Añade el componente `LocalizationManager`
4. Configura los idiomas que quieres (ya tiene ES, EN, CA, FR, DE por defecto)

### 3. Verificar tus diálogos existentes

1. Abre `Window → Editor Dialogo`
2. Selecciona un diálogo existente
3. Verás un nuevo selector de idioma en la parte superior
4. **IMPORTANTE**: Clic en `Verificar Idiomas` para crear campos para todos los idiomas

### 4. Traducir

**Opción A - Directo en Unity:**
- Cambia el `Idioma de Edición` en la barra superior
- Escribe las traducciones en las cajas de texto

**Opción B - Exportar CSV:**
- Clic en `Exportar CSV`
- Envía el archivo a tu traductor
- Cuando termine, clic en `Importar CSV`

### 5. Permitir cambiar idioma en el juego

**Método fácil con Dropdown:**
1. Añade un `Dropdown (TMP)` a tu menú de opciones
2. Añade el componente `LanguageSelectorUI` a un GameObject
3. Arrastra el Dropdown al campo correspondiente
4. ¡Listo!

---

## 🎯 Uso Diario

### Como Escritor/Diseñador:

1. Escribe el diálogo en español (tu idioma base)
2. Cambia a inglés en el selector superior
3. Escribe la traducción inglesa
4. Repite para otros idiomas
5. O exporta CSV y envía a traductores

### Como Traductor Externo:

1. Recibes un archivo CSV
2. Solo editas las columnas de idiomas (ej: "English", "French")
3. NO toques NodeID, Field, IsPlayerSpeaking
4. Devuelves el archivo
5. El desarrollador lo importa → ¡Listo!

---

## 📋 Ejemplo de CSV

```csv
NodeID,Field,IsPlayerSpeaking,Spanish,English
abc-123,SpeakerName,False,"Guardia","Guard"
abc-123,Dialogue,False,"Alto ahí","Halt"
def-456,SpeakerName,True,"Jugador","Player"
def-456,Dialogue,True,"¿Qué ocurre?","What's going on?"
```

**Traducir solo las columnas Spanish, English, etc.**

---

## ✅ Checklist de Verificación

Antes de dar el juego a testers:

- [ ] LocalizationManager en la escena principal
- [ ] Todos los diálogos tienen `Verificar Idiomas` ejecutado
- [ ] Traducciones completadas (o campos vacíos marcados)
- [ ] Selector de idioma funcional en el menú
- [ ] Probado cambiar de idioma en runtime

---

## 🚨 Problemas Comunes

**"No veo el selector de idioma en el editor"**
→ Asegúrate de haber reemplazado `DialogEditor.cs`

**"Los diálogos no cambian cuando cambio idioma en el juego"**
→ Verifica que tienes `LocalizationManager` en la escena activa

**"Algunos nodos están vacíos en inglés"**
→ Normal si aún no has traducido. Usa `Verificar Idiomas` y traduce

**"El CSV no se importa"**
→ Guárdalo como UTF-8 y no modifiques las primeras 3 columnas

---

## 📖 Más Info

Lee `README_LOCALIZATION.md` para la guía completa con todos los detalles.

---

## 🎉 ¡Eso es todo!

Ahora puedes:
✅ Escribir diálogos en múltiples idiomas sin código
✅ Exportar/Importar para trabajar con traductores
✅ Cambiar idioma en tiempo real en el juego
✅ Escalar a tantos idiomas como necesites

**¡Buena suerte con tu proyecto!** 🚀
