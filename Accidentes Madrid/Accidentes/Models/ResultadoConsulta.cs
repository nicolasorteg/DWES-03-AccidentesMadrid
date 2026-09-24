namespace Accidentes.Models;

/// <summary>
/// Resultado de una consulta
/// Separa el cálculo de la presentación
/// </summary>
public record ResultadoConsulta(
    int Numero,
    string Descripcion,
    string Resultado, 
    TimeSpan Tiempo);