using System;

/// <summary>
/// Marca un campo serializado para ser exportado/importado desde Google Sheets.
/// Uso: [ExcelPort] o [ExcelPort("Descripción para el designer")]
/// </summary>
[AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
public class ExcelPortAttribute : Attribute
{
    public string Description { get; }

    public ExcelPortAttribute(string description = "")
    {
        Description = description;
    }
}
