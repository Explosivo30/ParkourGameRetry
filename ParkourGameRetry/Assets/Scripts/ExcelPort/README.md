# ExcelPort — Variables Unity ↔ Google Sheets

Sistema que lee automáticamente los campos marcados con `[ExcelPort]` en tus MonoBehaviours y sincroniza los valores con Google Sheets. Sin tocar el sheet a mano, sin API keys, completamente gratuito.

---

## Instalación

1. Copia `Assets/` en tu proyecto Unity.
2. Instala **Editor Coroutines** en Package Manager (`com.unity.editorcoroutines`).
3. Abre **Tools → ExcelPort**.

---

## Setup inicial (una sola vez)

### 1. Marcar variables en tus scripts

```csharp
public class PlayerMovement : MonoBehaviour
{
    // Sin [ExcelPort] → no aparece en el sheet (LayerMasks, directions, etc.)
    [SerializeField] LayerMask _groundMask;
    float _castRadius = 0.49f;

    // Con [ExcelPort] → aparece en el sheet
    [ExcelPort("Aceleración del personaje")]
    [SerializeField] float _acceleration = 12f;

    [ExcelPort("Velocidad máxima")]
    [SerializeField] float _targetVelocity = 10f;

    [ExcelPort("Fuerza al girar en dirección contraria (0-1)")]
    [SerializeField, Range(0f, 1f)] float _turnaroundStrength;
}

public class PlayerJump : MonoBehaviour
{
    [ExcelPort("Fuerza de salto")]
    [SerializeField] float _jumpForce = 8f;

    [ExcelPort("Tiempo de coyote (s)")]
    [SerializeField] float _coyoteTime = 0.15f;

    [ExcelPort("Buffer de salto (s)")]
    [SerializeField] float _jumpBufferTime = 0.15f;
}

public class PlayerVault : MonoBehaviour
{
    [ExcelPort("Distancia de detección del vault")]
    [SerializeField] float _vaultCheckDistance = 1.2f;

    [ExcelPort("Duración del vault (s)")]
    [SerializeField] float _vaultDuration = 0.35f;

    [ExcelPort("Altura del arco del vault")]
    [SerializeField] float _vaultArcHeight = 0.4f;

    [ExcelPort("Velocidad de salida del vault")]
    [SerializeField] float _vaultExitSpeed = 4f;
}
```

### 2. Export (genera los CSVs)

**Tools → ExcelPort → Export → genera CSVs locales**

Escanea todos tus scripts por reflection, encuentra los `[ExcelPort]` y genera un CSV por script en `Assets/Data/ExcelPort/`:

```
Assets/Data/ExcelPort/
  PlayerMovement.csv
  PlayerJump.csv
  PlayerVault.csv
```

Estructura de cada CSV:
```
field,               type,  description,                   Player
_acceleration,       float, Aceleración del personaje,     12
_targetVelocity,     float, Velocidad máxima,              10
_turnaroundStrength, float, Fuerza al girar (0-1),         0.5
```

Si hay varias instancias del mismo MonoBehaviour en escena → una columna por cada GameObject.

### 3. Subir los CSVs a Google Sheets

- Crea un Google Spreadsheet.
- Para cada CSV generado, crea una pestaña con el **mismo nombre exacto que el script** (ej: `PlayerMovement`).
- Importa o pega el contenido del CSV en cada pestaña.

### 4. Publicar el sheet

Archivo → Compartir → Publicar en la web → selecciona cada pestaña → CSV → Publicar.

### 5. Poner el Sheet ID en la ventana

El Sheet ID está en la URL del spreadsheet:
```
https://docs.google.com/spreadsheets/d/ESTE_ES_EL_ID/edit
```

Pégalo en **Tools → ExcelPort → Google Sheet ID**. Se guarda automáticamente.

---

## Flujo diario

El designer edita valores en el sheet → tú abres Unity → **Import ← Google Sheets (auto)**

La tool:
1. Descarga la lista de pestañas del sheet automáticamente
2. Busca pestañas cuyo nombre coincida con un script que tenga `[ExcelPort]`
3. Descarga el CSV de cada una y aplica los valores a los MonoBehaviours en escena
4. Guarda una copia local de cada CSV como backup

**Nada es automático — todo requiere pulsar el botón.**

---

## Botones de la ventana

| Botón | Qué hace |
|---|---|
| 📤 Export → genera CSVs locales | Lee los `[ExcelPort]` de tus scripts y genera/actualiza los CSVs en `Assets/Data/ExcelPort/` |
| 📥 Import ← Google Sheets (auto) | Descubre las pestañas del sheet por nombre, descarga los CSVs y aplica los valores en escena |
| 📁 Import ← CSVs locales (fallback) | Aplica los CSVs locales sin conectarse a internet |
| 🔍 Preview | Muestra en la Console qué scripts y campos se exportarían, sin hacer nada |

---

## Añadir soporte a un tipo nuevo

1. Abre `ExcelPortHandlers.cs`
2. Crea tu handler implementando `IExcelPortHandler`:

```csharp
public class Vector3Handler : IExcelPortHandler
{
    public Type SupportedType => typeof(Vector3);

    // Tipos simples → un suffix vacío: new[] { "" }
    // Tipos compuestos → un suffix por sub-columna
    public string[] ColumnSuffixes => new[] { "_x", "_y", "_z" };

    public string[] ToColumns(object value)
    {
        var v = (Vector3)value;
        return new[]
        {
            v.x.ToString("G", System.Globalization.CultureInfo.InvariantCulture),
            v.y.ToString("G", System.Globalization.CultureInfo.InvariantCulture),
            v.z.ToString("G", System.Globalization.CultureInfo.InvariantCulture)
        };
    }

    public object FromColumns(string[] values)
    {
        float.TryParse(values[0], System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture, out float x);
        float.TryParse(values[1], System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture, out float y);
        float.TryParse(values[2], System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture, out float z);
        return new Vector3(x, y, z);
    }
}
```

3. Regístralo en `ExcelPortHandlerRegistry.Init()`:

```csharp
Register(new Vector3Handler());
```

Listo. Todos los campos `Vector3` con `[ExcelPort]` funcionan automáticamente.

---

## Reglas importantes

- **El sheet siempre manda** — Import sobreescribe los valores de Unity, nunca al revés.
- Si **quitas** `[ExcelPort]` de un campo → desaparece del CSV en el próximo Export, y del sheet al reimportar la pestaña.
- Si **renombras** un campo en código → Export genera una fila nueva, la antigua queda huérfana en el sheet (bórrala manualmente).
- Si una pestaña del sheet no coincide con ningún script → se ignora.
- Si un script tiene `[ExcelPort]` pero no hay pestaña en el sheet → warning en Console, no peta.
- Funciona con campos `private [SerializeField]` y `public`.
- Tipos sin handler registrado se ignoran con warning en Console.
