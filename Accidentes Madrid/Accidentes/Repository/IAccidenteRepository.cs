using Accidentes.Models;

namespace Accidentes.Repository;

public interface IAccidenteRepository {
    Task<IEnumerable<Accidente>> CargarDatosAsync(string directorioDatos);
}