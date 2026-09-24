using System.Text;
using Accidentes.Repository;
using Accidentes.Service;

Console.OutputEncoding = Encoding.UTF8;

// buscar data
var directorioDatos = ResolverDirectorioDatos();
Console.WriteLine($"Directorio de datos: {directorioDatos}\n");

// carga csv
Console.WriteLine("Cargando ficheros CSV...");
var repositorio = new AccidenteRepository();
var accidentes = (await repositorio.CargarDatosAsync(directorioDatos)).ToList();

Console.WriteLine($"Registros cargados: {accidentes.Count:N0}");
Console.WriteLine($"Accidentes únicos: {accidentes.Select(a => a.NumExpediente).Distinct().Count():N0}\n");

// consultas
Console.WriteLine("--- CONSULTAS LINQ / PLINQ ---\n");
var service = new AccidentesLinqService();
var resultados = service.EjecutarConsultas(accidentes);

foreach (var r in resultados) {
    Console.WriteLine($"{r.Numero,2}. {r.Descripcion}");
    Console.WriteLine($"    🔎 {Truncar(r.Resultado, 500)}");
    Console.WriteLine($"    ⏰ {r.Tiempo.TotalMilliseconds:F2} ms\n");
}

Console.WriteLine("--- CONSULTAS DATAFRAME ---\n");
var serviceDf = new AccidenteDataFrameService();
var resultadosDf = serviceDf.EjecutarConsultas(accidentes).ToList();

foreach (var r in resultadosDf) {
    Console.WriteLine($"{r.Numero,2}. {r.Descripcion}");
    Console.WriteLine($"    🔎 {Truncar(r.Resultado, 500)}");
    Console.WriteLine($"    ⏰ {r.Tiempo.TotalMilliseconds:F2} ms\n");
}

// resumen final ordenardo por tiempo
Console.WriteLine("========================================");
Console.WriteLine("=== RESUMEN GLOBAL ===\n");

var tiempoTotalLinq = resultados.Sum(r => r.Tiempo.TotalMilliseconds);
var tiempoTotalDf = resultadosDf.Sum(r => r.Tiempo.TotalMilliseconds);

Console.WriteLine($"Tiempo total LINQ:            {tiempoTotalLinq,8:F2} ms");
Console.WriteLine($"Tiempo total DataFrame:       {tiempoTotalDf,8:F2} ms");

Console.WriteLine("\n=== TOP 5 CONSULTAS MÁS LENTAS (LINQ) ===");
foreach (var r in resultados.OrderByDescending(r => r.Tiempo).Take(5)) {
    Console.WriteLine($"{r.Numero,2}. {r.Descripcion,-40} {r.Tiempo.TotalMilliseconds,8:F2} ms");
}

Console.WriteLine("\n=== TOP 5 CONSULTAS MÁS LENTAS (DATAFRAME) ===");
foreach (var r in resultadosDf.OrderByDescending(r => r.Tiempo).Take(5)) {
    Console.WriteLine($"{r.Numero,2}. {r.Descripcion,-40} {r.Tiempo.TotalMilliseconds,8:F2} ms");
}

return;
static string ResolverDirectorioDatos() {
    var directorio = Directory.GetCurrentDirectory();

    while (directorio != null) {
        var candidato = Path.Combine(directorio, "data");
        if (Directory.Exists(candidato))
            return candidato;

        directorio = Directory.GetParent(directorio)?.FullName;
    }

    throw new DirectoryNotFoundException("No se encontró data/ con los CSV.");
}
static string Truncar(string texto, int max) =>
    texto.Length <= max ? texto : texto[..max] + "...";