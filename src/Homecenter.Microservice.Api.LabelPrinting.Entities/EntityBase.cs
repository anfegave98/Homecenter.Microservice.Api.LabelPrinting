namespace Homecenter.Microservice.Api.LabelPrinting.Entities;

/// <summary>
/// Campos de control comunes a las tablas funcionales.
/// State habilita la eliminacion logica: en una operacion logistica los registros
/// no se borran, se desactivan.
/// </summary>
public abstract class EntityBase
{
    /// <summary>Identificador tecnico del registro.</summary>
    public int Id { get; set; }

    /// <summary>Indicador de vigencia. False equivale a eliminacion logica.</summary>
    public bool State { get; set; } = true;

    /// <summary>Fecha de creacion del registro, en UTC.</summary>
    public DateTimeOffset DateCreated { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>Fecha de la ultima modificacion, en UTC. Null si nunca se modifico.</summary>
    public DateTimeOffset? DateModified { get; set; }
}
