# Práctica 5: Análisis de Accidentes de Madrid con LINQ, PLINQ y DataFrames

Programa de consola en C# (.NET 10) que lee los CSV de accidentes de tráfico de Madrid (2024, 2025 y 2026), realiza **30 consultas con LINQ/PLINQ** y las **mismas 30 con DataFrame** (`Microsoft.Data.Analysis`), y compara los tiempos.

Datos: [datos abiertos del Ayuntamiento de Madrid](https://datos.madrid.es/dataset/300228-0-accidentes-trafico-detalle/information)

## Ejecución

```bash
# Con .NET
dotnet run --project Accidentes

# Con Docker
docker compose up --build
```

Los CSV deben estar en la carpeta `data/`. El programa la busca subiendo desde el directorio actual.

## Estructura del proyecto

```
Accidentes/
├── Program.cs                        # Carga los datos, lanza las consultas y muestra el resumen
├── data/                             # CSV de 2024, 2025 y 2026
├── Enums/                            # Sexo, Lesividad, TipoAccidente
├── Models/
│   ├── Accidente.cs                  # Record con los datos de cada fila
│   └── ResultadoConsulta.cs          # Número, descripción, resultado y tiempo
├── Mapper/
│   └── AccidenteMapper.cs            # Convierte el CSV al modelo (CsvHelper)
├── Repository/
│   └── AccidenteRepository.cs        # Lectura de los CSV
└── Service/
    ├── IAccidenteService.cs
    ├── AccidentesLinqService.cs      # 30 consultas con LINQ / PLINQ
    └── AccidenteDataFrameService.cs  # 30 consultas con DataFrame
```

## Justificación del diseño

**Lectura de ficheros**
- Uso **CsvHelper** porque el CSV tiene campos vacíos, valores no numéricos (`numero`) y "S"/"N" para alcohol y droga. Un `ClassMap` (`AccidenteMapper`) se encarga de convertirlos, y así la lectura no se llena de `Split(';')` y `Parse`.
- Leo los **3 ficheros en paralelo** con `Task.Run` + `Task.WhenAll`, porque son independientes entre sí y la lectura de ficheros grandes tarda. Así se aprovechan varios núcleos y el tiempo total se acerca al del fichero más grande.
- Los resultados se juntan en una única lista con `SelectMany`.

**Modelo**
- `Accidente` es un `record` con propiedades `init`: son datos que solo se leen y no cambian.
- Los campos que pueden venir vacíos son nullable (`CodDistrito`, `CodLesividad`, `NumCalle`).
- Cada fila del CSV es una **persona implicada**, no un accidente. Por eso hay 130.864 registros pero solo 55.530 accidentes únicos (`NumExpediente` distintos).

**Servicios**
- Los dos servicios implementan la misma interfaz `IAccidenteService`, así `Program.cs` los trata igual.
- `ResultadoConsulta` separa el cálculo de la presentación: los servicios devuelven datos y `Program.cs` los imprime.
- La función `Medir(...)` mide cada consulta con un `Stopwatch`, y así todas se miden igual.

**LINQ y PLINQ**
- Uso LINQ normal en las consultas simples: recorrer 130.000 elementos es rápido y paralelizar tendría más coste de coordinación que beneficio.
- Uso **PLINQ** (`AsParallel()`) en las consultas **25, 28 y 29**, que agrupan por año y dentro de cada año vuelven a agrupar (por distrito, hora o lesión). Es el trabajo más pesado y cada año se puede calcular por separado.

**DataFrame**
- Construyo el DataFrame una vez a partir de la lista y reutilizo las columnas en todas las consultas.
- Para los conteos uso `ValueCounts()`, y para filtrar `Filter` con máscaras (`ElementwiseEquals`).
- Las consultas por año filtran el DataFrame una vez por cada año.

## Tiempos de ejecución

| Fase | Tiempo |
|------|--------|
| Lectura de los 3 CSV (en paralelo) | _XXX ms_ |
| Construcción del DataFrame | 754,02 ms |
| Total 30 consultas LINQ / PLINQ | 891,89 ms |
| Total 30 consultas DataFrame | 2.903,24 ms |
| Total DataFrame + construcción | 3.657,26 ms |

Algunas consultas (en ms):

| Consulta | LINQ | DataFrame |
|----------|-----:|----------:|
| 9. Accidentes por día de la semana | 33,12 | 5,25 |
| 12. Lesiones más frecuentes | 35,90 | 5,93 |
| 22. Accidentes por código de distrito | 31,77 | 3,80 |
| 23. Accidentes por año | 24,43 | 3,87 |
| 7. Positivos en alcohol | 6,17 | 266,06 |
| 15. Proporción hombre/mujer | 13,54 | 534,35 |
| 25. Distrito con más accidentes por año | 78,49 | 280,02 |
| 27. Fin de semana vs entre semana por año | 47,00 | 272,10 |

## Análisis de resultados

**Por qué el DataFrame gana en unas consultas...**
Las consultas de tipo "contar cuántas veces aparece cada valor" (9, 12, 22, 23...) son mucho más rápidas con DataFrame. Los datos están guardados **por columnas**, así que `ValueCounts()` solo recorre la columna que le interesa, y LINQ tiene que crear objetos de grupo (`GroupBy`) para cada consulta.

**...y por qué pierde en otras.**
- `Filter` **es muy caro**: no solo cuenta las filas, sino que crea un DataFrame nuevo copiando las 19 columnas de las filas que pasan el filtro. En LINQ, `Count(a => a.PositivaAlcohol)` solo recorre y cuenta, sin copiar nada (6 ms frente a 266 ms en la consulta 7).
- La consulta 15 hace dos `Filter` (hombres y mujeres) y es la más lenta (534 ms).
- Las consultas 25, 27, 28 y 29 hacen un `Filter` por cada año, es decir, varias copias completas del DataFrame.
- **Construir el DataFrame cuesta 754 ms** (hay que convertir 130.000 objetos a columnas y crear muchos strings, como la fecha o el año-mes) y LINQ no tiene ese coste porque trabaja directamente sobre la lista.
- Los datos ya estaban cargados como objetos en memoria, así que LINQ parte con ventaja.
- El dataset es pequeño (130.000 filas). Las ventajas del DataFrame se notan más con millones de filas y cálculos numéricos.

**Sobre PLINQ**
Solo hay 3 años, por lo que el paralelismo se reparte como mucho en 3 tareas. Aun así, en las consultas anidadas cada año hace un trabajo grande, por eso tiene sentido usarlo ahí.

**Diferencias en los resultados**
En "accidentes con peatones" LINQ da 4.273 y DataFrame 4.271. La diferencia viene de que LINQ usa `Contains("Peatón", IgnoreCase)` y el DataFrame usa igualdad exacta (`ElementwiseEquals("Peatón")`), así que hay 2 registros con variantes en el texto que el DataFrame no cuenta.

## Conclusiones

- Para este volumen de datos, ya cargados en objetos, **LINQ es más práctico y más rápido en total**.
- El DataFrame compensa cuando el dato ya está en formato columnar y se hacen muchas agregaciones sobre columnas, no cuando hay que filtrar y copiar filas.
- Lo que aprendo para la próxima vez: leer los ficheros en paralelo, elegir la herramienta según el tipo de consulta, evitar operaciones que copian datos si solo necesito un número, y medir siempre todas las fases (incluida la construcción de las estructuras).
