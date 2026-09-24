using System.Diagnostics;
using Accidentes.Models;

namespace Accidentes.Service;

public sealed class AccidentesLinqService : IAccidenteService {

    public IEnumerable<ResultadoConsulta> EjecutarConsultas(IEnumerable<Accidente> datos) {
        
        var lista = datos.ToList();
        var resultados = new List<ResultadoConsulta>();
        var sw = new Stopwatch();

        void Medir(int numero, string descripcion, Func<string> calcular) {
            sw.Restart();
            var resultado = calcular();
            sw.Stop();
            resultados.Add(new ResultadoConsulta(numero, descripcion, resultado, sw.Elapsed));
        }

        // total accidentes
        Medir(1, "Total de accidentes", () => {
            var totalAccidentes = lista.Select(a => a.NumExpediente).Distinct().Count();
            return $"{totalAccidentes:N0} accidentes ({lista.Count:N0} personas implicadas)";
        });

        // accidentes por distrito (top 5)
        Medir(2, "Accidentes por distrito (top 5)", () => {
            var top5 = lista
                .GroupBy(a => a.Distrito)
                .OrderByDescending(g => g.Count())
                .Take(5)
                .Select(g => $"{g.Key} ({g.Count()})");
            return string.Join(", ", top5);
        });

        // accidentes por tipo
        Medir(3, "Accidentes por tipo", () => {
            var porTipo = lista
                .GroupBy(a => a.TipoAccidente)
                .OrderByDescending(g => g.Count())
                .Select(g => $"{g.Key} ({g.Count()})");
            return string.Join(", ", porTipo);
        });

        // accidentes por estado meteorológico 
        Medir(4, "Accidentes por estado meteorológico", () => {
            var porEstado = lista
                .GroupBy(a => string.IsNullOrWhiteSpace(a.EstadoMeteorologico) ? "(sin dato)" : a.EstadoMeteorologico)
                .OrderByDescending(g => g.Count())
                .Select(g => $"{g.Key} ({g.Count()})");
            return string.Join(", ", porEstado);
        });

        // accidentes por sexo
        Medir(5, "Accidentes por sexo", () => {
            var porSexo = lista
                .GroupBy(a => a.Sexo)
                .OrderByDescending(g => g.Count())
                .Select(g => $"{g.Key} ({g.Count()})");
            return string.Join(", ", porSexo);
        });

        // accidentes por rango de edad 
        Medir(6, "Accidentes por rango de edad", () => {
            var porEdad = lista
                .GroupBy(a => a.RangoEdad)
                .OrderByDescending(g => g.Count())
                .Select(g => $"{g.Key} ({g.Count()})");
            return string.Join(", ", porEdad);
        });

        // positivos en alcohol
        Medir(7, "Positivos en alcohol", () =>
            lista.Count(a => a.PositivaAlcohol).ToString("N0"));

        // positivos en drogas
        Medir(8, "Positivos en drogas", () =>
            lista.Count(a => a.PositivaDroga).ToString("N0"));

        // accidentes por día de la semana
        Medir(9, "Accidentes por día de la semana", () => {
            var porDia = lista
                .GroupBy(a => a.Fecha.DayOfWeek)
                .OrderByDescending(g => g.Count())
                .Select(g => $"{g.Key} ({g.Count()})");
            return string.Join(", ", porDia);
        });

        // accidentes por mes
        Medir(10, "Accidentes por mes", () => {
            var porMes = lista
                .GroupBy(a => a.Fecha.Month)
                .OrderBy(g => g.Key)
                .Select(g => $"{g.Key:00} ({g.Count()})");
            return string.Join(", ", porMes);
        });

        // hora con más accidentes 
        Medir(11, "Hora con más accidentes", () => {
            var top = lista
                .GroupBy(a => a.Hora.Hour)
                .OrderByDescending(g => g.Count())
                .First();
            return $"{top.Key:00}:00 ({top.Count()} accidentes)";
        });

        // lesiones más frecuentes
        Medir(12, "Lesiones más frecuentes", () => {
            var porLesion = lista
                .GroupBy(a => a.CodLesividad?.ToString() ?? "(sin asistencia / sin dato)")
                .OrderByDescending(g => g.Count())
                .Select(g => $"{g.Key} ({g.Count()})");
            return string.Join(", ", porLesion);
        });

        // tipo de vehículo más implicado
        Medir(13, "Tipo de vehículo más implicado", () => {
            var top = lista
                .Where(a => !string.IsNullOrWhiteSpace(a.TipoVehiculo))
                .GroupBy(a => a.TipoVehiculo)
                .OrderByDescending(g => g.Count())
                .First();
            return $"{top.Key} ({top.Count()})";
        });

        // accidentes con peatones 
        Medir(14, "Accidentes con peatones", () =>
            lista.Count(EsPeaton).ToString("N0"));

        // proporción hombre/mujer 
        Medir(15, "Proporción hombre/mujer", () => {
            var hombres = lista.Count(a => a.Sexo == Enums.Sexo.Hombre);
            var mujeres = lista.Count(a => a.Sexo == Enums.Sexo.Mujer);
            var total = hombres + mujeres;
            return total == 0
                ? "Sin datos"
                : $"Hombres: {hombres} ({hombres * 100.0 / total:F2}%), Mujeres: {mujeres} ({mujeres * 100.0 / total:F2}%)";
        });

        // distritos con más peatones
        Medir(16, "Distritos con más peatones", () => {
            var top5 = lista
                .Where(EsPeaton)
                .GroupBy(a => a.Distrito)
                .OrderByDescending(g => g.Count())
                .Take(5)
                .Select(g => $"{g.Key} ({g.Count()})");
            return string.Join(", ", top5);
        });

        // fin de semana vs entre semana
        Medir(17, "Fin de semana vs entre semana", () => {
            var fds = lista.Count(EsFinDeSemana);
            return $"Fin de semana: {fds} | Entre semana: {lista.Count - fds}";
        });

        // media de accidentes por día
        Medir(18, "Media de accidentes por día", () => {
            var diasConDatos = lista.Select(a => a.Fecha).Distinct().Count();
            var media = diasConDatos == 0 ? 0 : lista.Count / (double)diasConDatos;
            return $"{media:F2} registros/día ({diasConDatos} días distintos)";
        });

        // accidentes con alcohol + droga
        Medir(19, "Accidentes con alcohol + droga", () => lista.Count(a => a.PositivaAlcohol && a.PositivaDroga).ToString("N0"));

        // rangos de edad más vulnerables (peatones)
        Medir(20, "Rangos de edad más vulnerables (peatones)", () => {
            var top5 = lista
                .Where(EsPeaton)
                .GroupBy(a => a.RangoEdad)
                .OrderByDescending(g => g.Count())
                .Take(5)
                .Select(g => $"{g.Key} ({g.Count()})");
            return string.Join(", ", top5);
        });

        // distritos con más positivos en alcohol 
        Medir(21, "Distritos con más positivos en alcohol", () => {
            var top5 = lista
                .Where(a => a.PositivaAlcohol)
                .GroupBy(a => a.Distrito)
                .OrderByDescending(g => g.Count())
                .Take(5)
                .Select(g => $"{g.Key} ({g.Count()})");
            return string.Join(", ", top5);
        });

        // accidentes por código de distrito
        Medir(22, "Accidentes por código de distrito", () => {
            var porCodigo = lista
                .GroupBy(a => a.CodDistrito)
                .OrderBy(g => g.Key)
                .Select(g => $"{g.Key?.ToString() ?? "(sin dato)"}: {g.Count()}");
            return string.Join(", ", porCodigo);
        });

        // accidentes por año
        Medir(23, "Accidentes por año", () => {
            var porAnio = lista
                .GroupBy(a => a.Fecha.Year)
                .OrderBy(g => g.Key)
                .Select(g => $"{g.Key}: {g.Count()}");
            return string.Join(", ", porAnio);
        });

        // evolución mensual por año
        Medir(24, "Evolución mensual por año", () => {
            var evolucion = lista
                .GroupBy(a => (a.Fecha.Year, a.Fecha.Month))
                .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month)
                .Select(g => $"{g.Key.Year}-{g.Key.Month:00}: {g.Count()}");
            return string.Join(", ", evolucion);
        });

        // distrito con más accidentes por año
        // plinq pq agrupar por distrito en cada año es muy costoso
        Medir(25, "Distrito con más accidentes por año (PLINQ)", () => {
            var porAnio = lista.AsParallel()
                .GroupBy(a => a.Fecha.Year)
                .Select(g => new {
                    Anio = g.Key,
                    Top = g.GroupBy(a => a.Distrito).OrderByDescending(x => x.Count()).First()
                })
                .OrderBy(x => x.Anio)
                .Select(x => $"{x.Anio}: {x.Top.Key} ({x.Top.Count()})");
            return string.Join(", ", porAnio);
        });

        // tendencia de alcohol por año
        Medir(26, "Tendencia de alcohol por año", () => {
            var tendencia = lista
                .Where(a => a.PositivaAlcohol)
                .GroupBy(a => a.Fecha.Year)
                .OrderBy(g => g.Key)
                .Select(g => $"{g.Key}: {g.Count()}");
            return string.Join(", ", tendencia);
        });

        // comparativa fin de semana vs entre semana por año
        Medir(27, "Fin de semana vs entre semana por año", () => {
            var comparativa = lista
                .GroupBy(a => a.Fecha.Year)
                .OrderBy(g => g.Key)
                .Select(g => $"{g.Key}: FDS {g.Count(EsFinDeSemana)} / entre semana {g.Count() - g.Count(EsFinDeSemana)}");
            return string.Join(", ", comparativa);
        });

        // hora pico por año
        // plinq por igual que la 25
        Medir(28, "Hora pico por año (PLINQ)", () => {
            var porAnio = lista.AsParallel()
                .GroupBy(a => a.Fecha.Year)
                .Select(g => new
                {
                    Anio = g.Key,
                    Top = g.GroupBy(a => a.Hora.Hour).OrderByDescending(x => x.Count()).First()
                })
                .OrderBy(x => x.Anio)
                .Select(x => $"{x.Anio}: {x.Top.Key:00}:00 ({x.Top.Count()})");
            return string.Join(", ", porAnio);
        });

        // lesión más frecuente por año, plinq por mismo motivo
        Medir(29, "Lesión más frecuente por año (PLINQ)", () => {
            var porAnio = lista.AsParallel()
                .GroupBy(a => a.Fecha.Year)
                .Select(g => new {
                    Anio = g.Key,
                    Top = g.GroupBy(a => a.CodLesividad?.ToString() ?? "(sin dato)")
                        .OrderByDescending(x => x.Count()).First()
                })
                .OrderBy(x => x.Anio)
                .Select(x => $"{x.Anio}: {x.Top.Key} ({x.Top.Count()})");
            return string.Join(", ", porAnio);
        });

        // evolución de peatones por año
        Medir(30, "Evolución de peatones por año", () => {
            var evolucion = lista
                .Where(EsPeaton)
                .GroupBy(a => a.Fecha.Year)
                .OrderBy(g => g.Key)
                .Select(g => $"{g.Key}: {g.Count()}");
            return string.Join(", ", evolucion);
        });

        return resultados;
    }

    private static bool EsPeaton(Accidente a) =>
        a.TipoPersona.Contains("Peatón", StringComparison.OrdinalIgnoreCase);

    private static bool EsFinDeSemana(Accidente a) =>
        a.Fecha.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;
}