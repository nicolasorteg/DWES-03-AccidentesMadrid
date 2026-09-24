using System.Text;
using Accidentes.Repository;
using Accidentes.Service;

Console.OutputEncoding = Encoding.UTF8;

// --- 1. Resolver el directorio de datos (funciona igual en local y en Docker) ---
var directorioDatos = ResolverDirectorioDatos();
Console.WriteLine($"Directorio de datos: {directorioDatos}\n");

// --- 2. Cargar los CSV ---
Console.WriteLine("Cargando ficheros CSV...");
var repositorio = new AccidenteRepository();
var accidentes = (await repositorio.CargarDatosAsync(directorioDatos)).ToList();

Console.WriteLine($"Registros cargados: {accidentes.Count:N0}");
Console.WriteLine($"Accidentes únicos: {accidentes.Select(a => a.NumExpediente).Distinct().Count():N0}\n");

// --- 3. Ejecutar las 30 consultas LINQ ---
Console.WriteLine("=== CONSULTAS LINQ / PLINQ ===\n");
var analyzer = new AccidentesLinqService();
var resultados = analyzer.EjecutarConsultas(accidentes);

foreach (var r in resultados)
{
    Console.WriteLine($"{r.Numero,2}. {r.Descripcion}");
    Console.WriteLine($"    → {Truncar(r.Resultado, 150)}");
    Console.WriteLine($"    ⏱ {r.Tiempo.TotalMilliseconds:F2} ms\n");
}

// --- 4. Resumen final ordenado por tiempo (para detectar cuellos de botella) ---
Console.WriteLine("=== RESUMEN: consultas más lentas ===\n");
foreach (var r in resultados.OrderByDescending(r => r.Tiempo).Take(5))
{
    Console.WriteLine($"{r.Numero,2}. {r.Descripcion,-45} {r.Tiempo.TotalMilliseconds,8:F2} ms");
}

var tiempoTotal = resultados.Sum(r => r.Tiempo.TotalMilliseconds);
Console.WriteLine($"\nTiempo total de las 30 consultas LINQ: {tiempoTotal:F2} ms");

return;

// --- Función auxiliar: localizar data/ subiendo por el árbol de directorios ---
static string ResolverDirectorioDatos()
{
    var directorio = Directory.GetCurrentDirectory();

    while (directorio is not null)
    {
        var candidato = Path.Combine(directorio, "data");
        if (Directory.Exists(candidato))
            return candidato;

        directorio = Directory.GetParent(directorio)?.FullName;
    }

    throw new DirectoryNotFoundException("No se encontró el directorio data/ con los ficheros CSV.");
}

static string Truncar(string texto, int max) =>
    texto.Length <= max ? texto : texto[..max] + "...";