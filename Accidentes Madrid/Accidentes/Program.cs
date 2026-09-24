using Accidentes.Repository;

var repositorio = new AccidenteRepository();
var directorioDatos = repositorio.ResolverDirectorioDatos(); 
var accidentes = await repositorio.CargarDatosAsync(directorioDatos);

Console.WriteLine($"Total de registros cargados: {accidentes.Count()}");
Console.WriteLine($"Total de accidentes únicos: {accidentes.Select(a => a.NumExpediente).Distinct().Count()}");

