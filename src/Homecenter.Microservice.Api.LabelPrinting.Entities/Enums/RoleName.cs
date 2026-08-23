namespace Homecenter.Microservice.Api.LabelPrinting.Entities.Enums;

/// <summary>
/// Roles operativos. Se exponen como constantes porque viajan como claim en el JWT
/// y se usan en los atributos de autorizacion.
/// </summary>
public static class RoleName
{
    /// <summary>Imprime y consulta unicamente su propio historial.</summary>
    public const string Operario = "Operario";

    /// <summary>Autoriza reimpresiones con motivo y consulta el historial completo.</summary>
    public const string Supervisor = "Supervisor";

    /// <summary>Administra la operacion y consulta indicadores.</summary>
    public const string Admin = "Admin";

    /// <summary>Roles autorizados para ejecutar una reimpresion.</summary>
    public const string ReprintAuthorized = $"{Supervisor},{Admin}";
}
