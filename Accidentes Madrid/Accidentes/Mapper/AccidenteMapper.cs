using System.Globalization;
using Accidentes.Enums;
using Accidentes.Models;
using CsvHelper.Configuration;

namespace Accidentes.Mapper;

public sealed class AccidenteMapper : ClassMap<Accidente> {
    
    private static readonly CultureInfo Es = CultureInfo.GetCultureInfo("es-ES"); // para separador decimal con coma
    private static readonly string[] FormatosHora = ["H:mm:ss", "HH:mm:ss"];

    public AccidenteMapper() {
        
        Map(m => m.NumExpediente).Name("num_expediente");
        Map(m => m.Localizacion).Name("localizacion");
        Map(m => m.Distrito).Name("distrito");
        Map(m => m.TipoAccidente).Name("tipo_accidente"); 
        Map(m => m.EstadoMeteorologico).Name("estado_meteorológico");
        Map(m => m.TipoVehiculo).Name("tipo_vehiculo");
        Map(m => m.TipoPersona).Name("tipo_persona");
        Map(m => m.RangoEdad).Name("rango_edad");
        
        Map(m => m.Fecha).Name("fecha").Convert(args => ParseFecha(args.Row.GetField("fecha")));
        Map(m => m.Hora).Name("hora").Convert(args => ParseHora(args.Row.GetField("hora")));
        Map(m => m.NumCalle).Name("numero").Convert(args => { var raw = args.Row.GetField("numero")?.Trim(); return string.IsNullOrEmpty(raw) ? null : raw; });
        Map(m => m.CodDistrito).Name("cod_distrito").Convert(args => int.TryParse(args.Row.GetField("cod_distrito"), NumberStyles.Integer, Es, out var cd) ? cd : (int?)null);
        Map(m => m.Sexo).Name("sexo").Convert(args => ParseSexo(args.Row.GetField("sexo")));
        Map(m => m.CodLesividad).Name("cod_lesividad").Convert(args => ParseLesividad(args.Row.GetField("cod_lesividad")));       
        Map(m => m.CoordenadaXUtm).Name("coordenada_x_utm").Convert(args => double.TryParse(args.Row.GetField("coordenada_x_utm"), NumberStyles.Float, Es, out var x) ? x : 0);
        Map(m => m.CoordenadaYUtm).Name("coordenada_y_utm").Convert(args => double.TryParse(args.Row.GetField("coordenada_y_utm"), NumberStyles.Float, Es, out var y) ? y : 0);
        Map(m => m.PositivaAlcohol).Name("positiva_alcohol").Convert(args => string.Equals(args.Row.GetField("positiva_alcohol")?.Trim(), "S", StringComparison.OrdinalIgnoreCase));
        Map(m => m.PositivaDroga).Name("positiva_droga").Convert(args => ParsePositivaDroga(args.Row.GetField("positiva_droga")));
    }
    
    private static DateOnly ParseFecha(string? valor) =>
        DateOnly.TryParseExact(valor?.Trim(), "dd/MM/yyyy", Es, DateTimeStyles.None, out var f)
            ? f
            : DateOnly.MinValue;

    private static TimeOnly ParseHora(string? valor) =>
        TimeOnly.TryParseExact(valor?.Trim(), FormatosHora, Es, DateTimeStyles.None, out var h)
            ? h
            : TimeOnly.MinValue;

    private static Sexo ParseSexo(string? valor) =>
        valor?.Trim().ToLowerInvariant() switch {
            "hombre" => Sexo.Hombre,
            "mujer" => Sexo.Mujer,
            _ => Sexo.NoAsignado // desconocido
        };

    private static Lesividad? ParseLesividad(string? valor) {
        if (string.IsNullOrWhiteSpace(valor)) return null;
        return int.TryParse(valor, NumberStyles.Integer, Es, out var codigo)
               && System.Enum.IsDefined(typeof(Lesividad), codigo)
            ? (Lesividad)codigo
            : null; 
    }

    private static bool ParsePositivaDroga(string? valor) {
        var v = valor?.Trim();
        return string.Equals(v, "1", StringComparison.Ordinal)
               || string.Equals(v, "S", StringComparison.OrdinalIgnoreCase);
    }
    
}