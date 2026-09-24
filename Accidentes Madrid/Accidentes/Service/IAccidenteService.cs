using Accidentes.Models;

namespace Accidentes.Service;

public interface IAccidenteService {
    IEnumerable<ResultadoConsulta> EjecutarConsultas(IEnumerable<Accidente> datos);
}