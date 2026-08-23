namespace Homecenter.Microservice.Api.LabelPrinting.Entities.Enums;

/// <summary>
/// Roles operativos. Se exponen como constantes porque viajan como claim en el JWT
/// y se usan en los atributos de autorizacion.
/// </summary>
public static class RoleName
{
    public const string Operario = "Operario";
    public const string Supervisor = "Supervisor";
    public const string Admin = "Admin";

    /// <summary>Roles autorizados para ejecutar una reimpresion.</summary>
    public const string ReprintAuthorized = $"{Supervisor},{Admin}";
}
