using Accidentes.Enums;

namespace Accidentes.Models;

/// <summary>
/// Record que representa un Accidente
/// </summary>
public record Accidente {
    
    public string NumExpediente { get; init; } = string.Empty;

    public DateOnly Fecha { get; init; }
    public TimeOnly Hora { get; init; }

    public string Localizacion { get; init; } = string.Empty;
    public string? NumCalle { get; init; } // nullable
    public int? CodDistrito { get; init; }
    public string Distrito { get; init; } = string.Empty;

    public string TipoAccidente { get; init; } = string.Empty;

    public string EstadoMeteorologico { get; init; } = string.Empty;

    public string TipoVehiculo { get; init; } = string.Empty;
    
    public string TipoPersona { get; init; } = string.Empty;
    public string RangoEdad { get; init; } = string.Empty;
    public Sexo Sexo { get; init; }

    public TipoAccidente CodigoAccidente { get; init; }

    public Lesividad CodLesividad { get; init; }

    public double CoordenadaXUtm { get; init; }
    public double CoordenadaYUtm { get; init; }

    public bool PositivaAlcohol { get; init; }
    public bool PositivaDroga { get; init; }
}