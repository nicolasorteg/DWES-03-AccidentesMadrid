using Accidentes.Models;

namespace Accidentes.Service;

public interface IAccidenteService {
    IReadOnlyList<ResultadoConsulta> EjecutarConsultas(IEnumerable<Accidente> datos);
}