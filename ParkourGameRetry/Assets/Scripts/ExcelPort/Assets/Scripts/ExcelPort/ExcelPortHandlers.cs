using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Interfaz base para convertir un tipo a/desde string CSV.
/// Para añadir soporte a un tipo nuevo:
///   1. Crea una clase que implemente IExcelPortHandler
///   2. Regístrala en ExcelPortHandlerRegistry.Register()
/// </summary>
public interface IExcelPortHandler
{
    Type SupportedType { get; }

    // Convierte el valor del campo a string para el sheet
    // Un campo puede ocupar más de una columna (ej: Vector3 → x, y, z)
    string[] ToColumns(object value);

    // Nombres de las sub-columnas (ej: "x", "y", "z" para Vector3)
    // Si es un tipo simple, devuelve array de un solo elemento vacío
    string[] ColumnSuffixes { get; }

    // Reconstruye el valor desde los strings del sheet
    object FromColumns(string[] values);
}

// ── Handlers concretos ───────────────────────────────────────────────────────

public class FloatHandler : IExcelPortHandler
{
    public Type SupportedType => typeof(float);
    public string[] ColumnSuffixes => new[] { "" };
    public string[] ToColumns(object value) =>
        new[] { ((float)value).ToString("G", System.Globalization.CultureInfo.InvariantCulture) };
    public object FromColumns(string[] values)
    {
        float.TryParse(values[0], System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture, out float result);
        return result;
    }
}

public class IntHandler : IExcelPortHandler
{
    public Type SupportedType => typeof(int);
    public string[] ColumnSuffixes => new[] { "" };
    public string[] ToColumns(object value) => new[] { value.ToString() };
    public object FromColumns(string[] values)
    {
        int.TryParse(values[0], out int result);
        return result;
    }
}

public class BoolHandler : IExcelPortHandler
{
    public Type SupportedType => typeof(bool);
    public string[] ColumnSuffixes => new[] { "" };
    public string[] ToColumns(object value) => new[] { (bool)value ? "true" : "false" };
    public object FromColumns(string[] values)
    {
        string v = values[0].ToLower().Trim();
        return v == "true" || v == "1" || v == "yes" || v == "si";
    }
}

public class StringHandler : IExcelPortHandler
{
    public Type SupportedType => typeof(string);
    public string[] ColumnSuffixes => new[] { "" };
    public string[] ToColumns(object value) => new[] { value?.ToString() ?? "" };
    public object FromColumns(string[] values) => values[0];
}

// ── Ejemplo de tipo compuesto: Vector2 ───────────────────────────────────────
// Ocupa DOS columnas en el sheet: field_x, field_y
public class Vector2Handler : IExcelPortHandler
{
    public Type SupportedType => typeof(Vector2);
    public string[] ColumnSuffixes => new[] { "_x", "_y" };
    public string[] ToColumns(object value)
    {
        var v = (Vector2)value;
        return new[]
        {
            v.x.ToString("G", System.Globalization.CultureInfo.InvariantCulture),
            v.y.ToString("G", System.Globalization.CultureInfo.InvariantCulture)
        };
    }
    public object FromColumns(string[] values)
    {
        float.TryParse(values[0], System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture, out float x);
        float.TryParse(values[1], System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture, out float y);
        return new Vector2(x, y);
    }
}

// ── Registro central ─────────────────────────────────────────────────────────

public static class ExcelPortHandlerRegistry
{
    private static Dictionary<Type, IExcelPortHandler> _handlers;

    public static Dictionary<Type, IExcelPortHandler> All
    {
        get
        {
            if (_handlers == null) Init();
            return _handlers;
        }
    }

    static void Init()
    {
        _handlers = new Dictionary<Type, IExcelPortHandler>();

        // ── Registra aquí todos los handlers ──
        // Para añadir un tipo nuevo: crea tu handler arriba y añádelo aquí
        Register(new FloatHandler());
        Register(new IntHandler());
        Register(new BoolHandler());
        Register(new StringHandler());
        Register(new Vector2Handler());
        // Register(new Vector3Handler());  // ← ejemplo de extensión futura
        // Register(new MiTipoHandler());
    }

    public static void Register(IExcelPortHandler handler)
    {
        _handlers[handler.SupportedType] = handler;
    }

    public static bool TryGet(Type type, out IExcelPortHandler handler)
    {
        return All.TryGetValue(type, out handler);
    }
}
