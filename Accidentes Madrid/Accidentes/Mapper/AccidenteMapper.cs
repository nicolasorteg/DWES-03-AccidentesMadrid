using System.Globalization;
using Accidentes.Models;
using CsvHelper.Configuration;

namespace Accidentes.Mapper;

public sealed class AccidenteMapper : ClassMap<Accidente> {
    
    private static readonly CultureInfo Es = CultureInfo.GetCultureInfo("es-ES"); // para separador decimal con coma
    private static readonly string[] FormatosHora = ["H:mm:ss", "HH:mm:ss"];

    public AccidenteMapper() {
        
    }
    
}