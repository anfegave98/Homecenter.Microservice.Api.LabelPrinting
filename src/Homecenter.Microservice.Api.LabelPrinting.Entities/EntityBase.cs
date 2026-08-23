namespace Homecenter.Microservice.Api.LabelPrinting.Entities;

/// <summary>
/// Campos de control comunes a las tablas funcionales.
/// State habilita la eliminacion logica: en una operacion logistica los registros
/// no se borran, se desactivan.
/// </summary>
public abstract class EntityBase
{
    public int Id { get; set; }

    public bool State { get; set; } = true;

    public DateTimeOffset DateCreated { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? DateModified { get; set; }
}
