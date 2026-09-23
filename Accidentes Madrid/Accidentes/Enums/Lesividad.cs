namespace Accidentes.Enums;

/// <summary>
/// Enum que almacena los tipos de Lesividad para
/// el informe del accidente.
/// </summary>
public enum Lesividad {
    AtencionUrgenciasSinIngreso = 1,
    IngresoInferiorUnDia = 2,
    IngresoSuperiorUnDia = 3,
    FallecidoUnDia = 4,
    AsistenciaAmbulatoriaPosterior = 5,
    AsistenciaSanitariaInmediataCentroSalud = 6,
    AsistenciaSanitariaAccidente = 7,
    SinAsistenciaSanitaria = 14, // en blanco sin asistencia sanitaria
    SeDesconoce = 77
}