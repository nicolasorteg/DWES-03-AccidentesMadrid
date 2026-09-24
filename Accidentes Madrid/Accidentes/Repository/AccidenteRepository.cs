using System.Globalization;
using Accidentes.Mapper;
using Accidentes.Models;
using CsvHelper;
using CsvHelper.Configuration;

namespace Accidentes.Repository;

public class AccidenteRepository : IAccidenteRepository {
    
    private static readonly CsvConfiguration Config = new(CultureInfo.InvariantCulture) {
        Delimiter = ";",
        HasHeaderRecord = true,
        MissingFieldFound = null,
        HeaderValidated = null
    };

    public async Task<IEnumerable<Accidente>> CargarDatosAsync(string directorioDatos) {
        if (!Directory.Exists(directorioDatos))
            throw new DirectoryNotFoundException($"No existe el directorio {directorioDatos}");

        var ficheros = Directory.GetFiles(directorioDatos, "*.csv").OrderBy(f => f).ToArray();

        if (ficheros.Length == 0)
            throw new FileNotFoundException($"No se encontraron ficheros CSV en {directorioDatos}");
        
        var tareasLectura = ficheros
            .Select(fichero => Task.Run(() => LeerFichero(fichero)))
            .ToArray();

        var resultadosPorFichero = await Task.WhenAll(tareasLectura);

        return resultadosPorFichero
            .SelectMany(accidentes => accidentes)
            .ToList();
    }

    private static List<Accidente> LeerFichero(string fichero) {
        using var reader = new StreamReader(fichero, System.Text.Encoding.UTF8);
        using var csv = new CsvReader(reader, Config);

        csv.Context.RegisterClassMap<AccidenteMapper>();

        return csv.GetRecords<Accidente>().ToList();
    }
    
    public string ResolverDirectorioDatos() {
        var directorio = Directory.GetCurrentDirectory();

        while (directorio != null) {
            var candidato = Path.Combine(directorio, "data");
            if (Directory.Exists(candidato))
                return candidato;

            directorio = Directory.GetParent(directorio)?.FullName;
        }

        throw new DirectoryNotFoundException("No se encontró el directorio data/ con los ficheros CSV.");
    }
}