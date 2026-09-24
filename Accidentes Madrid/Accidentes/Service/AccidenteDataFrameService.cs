using System.Diagnostics;
using Accidentes.Models;
using Microsoft.Data.Analysis;

namespace Accidentes.Service;

public sealed class AccidenteDataFrameService : IAccidenteService {
    
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

        // construccion dataframe
        var swConstruccion = Stopwatch.StartNew();
        var df = ConstruirDataFrame(lista);
        swConstruccion.Stop();
        Console.WriteLine($"[DataFrame] Construcción del DataFrame: {swConstruccion.Elapsed.TotalMilliseconds:F2} ms\n");

        var distrito = (StringDataFrameColumn)df.Columns["distrito"];
        var tipoAccidente = (StringDataFrameColumn)df.Columns["tipo_accidente"];
        var estadoMeteo = (StringDataFrameColumn)df.Columns["estado_meteorologico"];
        var sexo = (StringDataFrameColumn)df.Columns["sexo"];
        var rangoEdad = (StringDataFrameColumn)df.Columns["rango_edad"];
        var alcohol = (PrimitiveDataFrameColumn<bool>)df.Columns["alcohol"];
        var droga = (PrimitiveDataFrameColumn<bool>)df.Columns["droga"];
        var diaSemana = (StringDataFrameColumn)df.Columns["dia_semana"];
        var mes = (PrimitiveDataFrameColumn<int>)df.Columns["mes"];
        var hora = (PrimitiveDataFrameColumn<int>)df.Columns["hora"];
        var lesividad = (StringDataFrameColumn)df.Columns["lesividad"];
        var tipoVehiculo = (StringDataFrameColumn)df.Columns["tipo_vehiculo"];
        var tipoPersona = (StringDataFrameColumn)df.Columns["tipo_persona"];
        var codDistrito = (PrimitiveDataFrameColumn<int>)df.Columns["cod_distrito"];
        var anio = (PrimitiveDataFrameColumn<int>)df.Columns["anio"];
        var finDeSemana = (PrimitiveDataFrameColumn<bool>)df.Columns["fin_semana"];
        var fecha = (StringDataFrameColumn)df.Columns["fecha"];
        var numExpediente = (StringDataFrameColumn)df.Columns["num_expediente"];

        // total de accidentes 
        Medir(1, "Total de accidentes", () => {
            var totalAccidentes = numExpediente.ValueCounts().Rows.Count; // nº de valores distintos
            return $"{totalAccidentes:N0} accidentes ({df.Rows.Count:N0} personas implicadas)";
        });

        // accidentes por distrito (top 5)
        Medir(2, "Accidentes por distrito (top 5)", () => TopNTexto(df, "distrito", 5));

        // accidentes por tipo 
        Medir(3, "Accidentes por tipo", () => TopNTexto(df, "tipo_accidente", int.MaxValue));

        // accidentes por estado meteorológico 
        Medir(4, "Accidentes por estado meteorológico", () => TopNTexto(df, "estado_meteorologico", int.MaxValue));

        // accidentes por sexo 
        Medir(5, "Accidentes por sexo", () => TopNTexto(df, "sexo", int.MaxValue));

        // accidentes por rango de edad 
        Medir(6, "Accidentes por rango de edad", () => TopNTexto(df, "rango_edad", int.MaxValue));

        // positivos en alcohol 
        Medir(7, "Positivos en alcohol", () =>
            df.Filter(alcohol.ElementwiseEquals(true)).Rows.Count.ToString("N0"));

        // positivos en drogas 
        Medir(8, "Positivos en drogas", () =>
            df.Filter(droga.ElementwiseEquals(true)).Rows.Count.ToString("N0"));

        // accidentes por día de la semana 
        Medir(9, "Accidentes por día de la semana", () => TopNTexto(df, "dia_semana", int.MaxValue));

        // accidentes por mes 
        Medir(10, "Accidentes por mes", () => {
            var conteo = mes.ValueCounts();
            var col0 = (PrimitiveDataFrameColumn<int>)conteo.Columns[0];
            var col1 = (PrimitiveDataFrameColumn<long>)conteo.Columns[1];
            var pares = Enumerable.Range(0, (int)conteo.Rows.Count)
                .Select(i => (Mes: col0[i], Cuenta: col1[i]))
                .OrderBy(p => p.Mes)
                .Select(p => $"{p.Mes:00} ({p.Cuenta})");
            return string.Join(", ", pares);
        });

        // hora con más accidentes 
        Medir(11, "Hora con más accidentes", () => {
            var conteo = hora.ValueCounts();
            var col0 = (PrimitiveDataFrameColumn<int>)conteo.Columns[0];
            var col1 = (PrimitiveDataFrameColumn<long>)conteo.Columns[1];
            var top = Enumerable.Range(0, (int)conteo.Rows.Count)
                .Select(i => (Hora: col0[i], Cuenta: col1[i]))
                .OrderByDescending(p => p.Cuenta)
                .First();
            return $"{top.Hora:00}:00 ({top.Cuenta} accidentes)";
        });

        // lesiones más frecuentes 
        Medir(12, "Lesiones más frecuentes", () => TopNTexto(df, "lesividad", int.MaxValue));

        // tipo de vehículo más implicado 
        Medir(13, "Tipo de vehículo más implicado", () => TopNTexto(df, "tipo_vehiculo", 1));

        // accidentes con peatones 
        Medir(14, "Accidentes con peatones", () =>
            df.Filter(tipoPersona.ElementwiseEquals("Peatón")).Rows.Count.ToString("N0"));

        // proporción hombre/mujer 
        Medir(15, "Proporción hombre/mujer", () => {
            var totalHombres = df.Filter(sexo.ElementwiseEquals("Hombre")).Rows.Count;
            var totalMujeres = df.Filter(sexo.ElementwiseEquals("Mujer")).Rows.Count;
            var total = totalHombres + totalMujeres;
            return total == 0
                ? "Sin datos"
                : $"Hombres: {totalHombres} ({totalHombres * 100.0 / total:F2}%), Mujeres: {totalMujeres} ({totalMujeres * 100.0 / total:F2}%)";
        });

        // distritos con más peatones 
        Medir(16, "Distritos con más peatones", () => {
            var dfPeatones = df.Filter(tipoPersona.ElementwiseEquals("Peatón"));
            return TopNTexto(dfPeatones, "distrito", 5);
        });

        // fin de semana vs entre semana 
        Medir(17, "Fin de semana vs entre semana", () => {
            var fds = df.Filter(finDeSemana.ElementwiseEquals(true)).Rows.Count;
            return $"Fin de semana: {fds} | Entre semana: {df.Rows.Count - fds}";
        });

        // media de accidentes por día 
        Medir(18, "Media de accidentes por día", () => {
            var diasDistintos = fecha.ValueCounts().Rows.Count;
            var media = diasDistintos == 0 ? 0 : df.Rows.Count / (double)diasDistintos;
            return $"{media:F2} registros/día ({diasDistintos} días distintos)";
        });

        // accidentes con alcohol + droga 
        Medir(19, "Accidentes con alcohol + droga", () => {
            var mascara = (PrimitiveDataFrameColumn<bool>)(alcohol.ElementwiseEquals(true) & droga.ElementwiseEquals(true));
            return df.Filter(mascara).Rows.Count.ToString("N0");
        });

        // rangos de edad más vulnerables (peatones) 
        Medir(20, "Rangos de edad más vulnerables (peatones)", () => {
            var dfPeatones = df.Filter(tipoPersona.ElementwiseEquals("Peatón"));
            return TopNTexto(dfPeatones, "rango_edad", 5);
        });

        // distritos con más positivos en alcohol 
        Medir(21, "Distritos con más positivos en alcohol", () => {
            var dfAlcohol = df.Filter(alcohol.ElementwiseEquals(true));
            return TopNTexto(dfAlcohol, "distrito", 5);
        });

        // accidentes por código de distrito 
        Medir(22, "Accidentes por código de distrito", () => {
            var conteo = codDistrito.ValueCounts();
            var col0 = (PrimitiveDataFrameColumn<int>)conteo.Columns[0];
            var col1 = (PrimitiveDataFrameColumn<long>)conteo.Columns[1];
            var pares = Enumerable.Range(0, (int)conteo.Rows.Count)
                .Select(i => (Codigo: col0[i], Cuenta: col1[i]))
                .OrderBy(p => p.Codigo)
                .Select(p => $"{p.Codigo}: {p.Cuenta}");
            return string.Join(", ", pares);
        });

        // accidentes por año
        Medir(23, "Accidentes por año", () => {
            var conteo = anio.ValueCounts();
            var col0 = (PrimitiveDataFrameColumn<int>)conteo.Columns[0];
            var col1 = (PrimitiveDataFrameColumn<long>)conteo.Columns[1];
            var pares = Enumerable.Range(0, (int)conteo.Rows.Count)
                .Select(i => (Anio: col0[i], Cuenta: col1[i]))
                .OrderBy(p => p.Anio)
                .Select(p => $"{p.Anio}: {p.Cuenta}");
            return string.Join(", ", pares);
        });

        // evolución mensual por año
        Medir(24, "Evolución mensual por año", () => {
            var anioMes = (StringDataFrameColumn)df.Columns["anio_mes"];
            var conteo = anioMes.ValueCounts();
            var col0 = (StringDataFrameColumn)conteo.Columns[0];
            var col1 = (PrimitiveDataFrameColumn<long>)conteo.Columns[1];
            var pares = Enumerable.Range(0, (int)conteo.Rows.Count)
                .Select(i => (Clave: col0[i], Cuenta: col1[i]))
                .OrderBy(p => p.Clave)
                .Select(p => $"{p.Clave}: {p.Cuenta}");
            return string.Join(", ", pares);
        });

        // 25, 28, 29 agrupaciones anidadas por año
        var aniosDistintos = ObtenerValoresUnicos(anio).OrderBy(a => a).ToList();

        Medir(25, "Distrito con más accidentes por año", () => {
            var partes = aniosDistintos.Select(a =>
            {
                var dfAnio = df.Filter(anio.ElementwiseEquals(a));
                var top = TopNTexto(dfAnio, "distrito", 1);
                return $"{a}: {top}";
            });
            return string.Join(", ", partes);
        });

        // tendencia de alcohol por año
        Medir(26, "Tendencia de alcohol por año", () => {
            var dfAlcohol = df.Filter(alcohol.ElementwiseEquals(true));
            var anioAlcohol = (PrimitiveDataFrameColumn<int>)dfAlcohol.Columns["anio"];
            var conteo = anioAlcohol.ValueCounts();
            var col0 = (PrimitiveDataFrameColumn<int>)conteo.Columns[0];
            var col1 = (PrimitiveDataFrameColumn<long>)conteo.Columns[1];
            var pares = Enumerable.Range(0, (int)conteo.Rows.Count)
                .Select(i => (Anio: col0[i], Cuenta: col1[i]))
                .OrderBy(p => p.Anio)
                .Select(p => $"{p.Anio}: {p.Cuenta}");
            return string.Join(", ", pares);
        });

        // fin de semana vs entre semana por año
        Medir(27, "Fin de semana vs entre semana por año", () => {
            var partes = aniosDistintos.Select(a =>
            {
                var dfAnio = df.Filter(anio.ElementwiseEquals(a));
                var finDeSemanaAnio = (PrimitiveDataFrameColumn<bool>)dfAnio.Columns["fin_semana"];
                var fds = dfAnio.Filter(finDeSemanaAnio.ElementwiseEquals(true)).Rows.Count;
                return $"{a}: FDS {fds} / entre semana {dfAnio.Rows.Count - fds}";
            });
            return string.Join(", ", partes);
        });

        Medir(28, "Hora pico por año", () => {
            var partes = aniosDistintos.Select(a =>
            {
                var dfAnio = df.Filter(anio.ElementwiseEquals(a));
                var horaAnio = (PrimitiveDataFrameColumn<int>)dfAnio.Columns["hora"];
                var conteo = horaAnio.ValueCounts();
                var col0 = (PrimitiveDataFrameColumn<int>)conteo.Columns[0];
                var col1 = (PrimitiveDataFrameColumn<long>)conteo.Columns[1];
                var top = Enumerable.Range(0, (int)conteo.Rows.Count)
                    .Select(i => (Hora: col0[i], Cuenta: col1[i]))
                    .OrderByDescending(p => p.Cuenta)
                    .First();
                return $"{a}: {top.Hora:00}:00 ({top.Cuenta})";
            });
            return string.Join(", ", partes);
        });

        Medir(29, "Lesión más frecuente por año", () => {
            var partes = aniosDistintos.Select(a =>
            {
                var dfAnio = df.Filter(anio.ElementwiseEquals(a));
                var top = TopNTexto(dfAnio, "lesividad", 1);
                return $"{a}: {top}";
            });
            return string.Join(", ", partes);
        });

        // evolución de peatones por año 
        Medir(30, "Evolución de peatones por año", () => {
            var dfPeatones = df.Filter(tipoPersona.ElementwiseEquals("Peatón"));
            var anioPeatones = (PrimitiveDataFrameColumn<int>)dfPeatones.Columns["anio"];
            var conteo = anioPeatones.ValueCounts();
            var col0 = (PrimitiveDataFrameColumn<int>)conteo.Columns[0];
            var col1 = (PrimitiveDataFrameColumn<long>)conteo.Columns[1];
            var pares = Enumerable.Range(0, (int)conteo.Rows.Count)
                .Select(i => (Anio: col0[i], Cuenta: col1[i]))
                .OrderBy(p => p.Anio)
                .Select(p => $"{p.Anio}: {p.Cuenta}");
            return string.Join(", ", pares);
        });

        return resultados;
    }

    // construye a partir de la lista de datoa
    private static DataFrame ConstruirDataFrame(List<Accidente> datos) {
        var numExpediente = new StringDataFrameColumn("num_expediente", datos.Select(a => a.NumExpediente));
        var fecha = new StringDataFrameColumn("fecha", datos.Select(a => a.Fecha.ToString("yyyy-MM-dd")));
        var anio = new PrimitiveDataFrameColumn<int>("anio", datos.Select(a => (int?)a.Fecha.Year));
        var mes = new PrimitiveDataFrameColumn<int>("mes", datos.Select(a => (int?)a.Fecha.Month));
        var diaSemana = new StringDataFrameColumn("dia_semana",
            datos.Select(a => a.Fecha.DayOfWeek.ToString()));
        var hora = new PrimitiveDataFrameColumn<int>("hora", datos.Select(a => (int?)a.Hora.Hour));
        var distrito = new StringDataFrameColumn("distrito", datos.Select(a => a.Distrito));
        var codDistrito = new PrimitiveDataFrameColumn<int>("cod_distrito", datos.Select(a => a.CodDistrito));
        var tipoAccidente = new StringDataFrameColumn("tipo_accidente", datos.Select(a => a.TipoAccidente));
        var estadoMeteo = new StringDataFrameColumn("estado_meteorologico", datos.Select(a => a.EstadoMeteorologico));
        var tipoVehiculo = new StringDataFrameColumn("tipo_vehiculo", datos.Select(a => a.TipoVehiculo));
        var tipoPersona = new StringDataFrameColumn("tipo_persona", datos.Select(a => a.TipoPersona));
        var rangoEdad = new StringDataFrameColumn("rango_edad", datos.Select(a => a.RangoEdad));
        var sexo = new StringDataFrameColumn("sexo", datos.Select(a => a.Sexo.ToString()));
        var lesividad = new StringDataFrameColumn("lesividad",
            datos.Select(a => a.CodLesividad?.ToString() ?? "(sin dato)"));
        var alcohol = new PrimitiveDataFrameColumn<bool>("alcohol", datos.Select(a => (bool?)a.PositivaAlcohol));
        var droga = new PrimitiveDataFrameColumn<bool>("droga", datos.Select(a => (bool?)a.PositivaDroga));
        var finDeSemana = new PrimitiveDataFrameColumn<bool>("fin_semana",
            datos.Select(a => (bool?)(a.Fecha.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)));
        var anioMes = new StringDataFrameColumn("anio_mes",
            datos.Select(a => $"{a.Fecha.Year}-{a.Fecha.Month:00}"));

        return new DataFrame(numExpediente, fecha, anio, mes, diaSemana, hora, distrito, codDistrito,
            tipoAccidente, estadoMeteo, tipoVehiculo, tipoPersona, rangoEdad, sexo, lesividad,
            alcohol, droga, finDeSemana, anioMes);
    }

    // agrupa por una columna de texto, ordena descendente y formatea el top N
    private static string TopNTexto(DataFrame df, string columna, int n) {
        var col = (StringDataFrameColumn)df.Columns[columna];
        var conteo = col.ValueCounts(); // valor, counts
        var valores = (StringDataFrameColumn)conteo.Columns[0];
        
        var cuentas = (PrimitiveDataFrameColumn<long>)conteo.Columns[1];

        var pares = Enumerable.Range(0, (int)conteo.Rows.Count)
            .Select(i => (Valor: valores[i] ?? "(sin dato)", Cuenta: cuentas[i] ?? 0))
            .OrderByDescending(p => p.Cuenta)
            .Take(n)
            .Select(p => $"{p.Valor} ({p.Cuenta})");

        return string.Join(", ", pares);
    }

    private static List<int> ObtenerValoresUnicos(PrimitiveDataFrameColumn<int> columna) {
        var conteo = columna.ValueCounts();
        var col0 = (PrimitiveDataFrameColumn<int>)conteo.Columns[0];
        return Enumerable.Range(0, (int)conteo.Rows.Count).Select(i => col0[i]!.Value).ToList();
    }
}