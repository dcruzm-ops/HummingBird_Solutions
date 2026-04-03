using PSA.DataAccess.DAO;
using PSA.EntidadesDTO.DTOs.Fincas;

namespace PSA.AppCore
{
    public class FincaService
    {
        private readonly FincaDAO _fincaDAO;

        public FincaService(FincaDAO fincaDAO)
        {
            _fincaDAO = fincaDAO ?? throw new ArgumentNullException(nameof(fincaDAO));
        }

        public List<FincaDTO> RetrieveAll()
        {
            return _fincaDAO.RetrieveAll();
        }

        public FincaDTO? RetrieveById(int id)
        {
            if (id <= 0)
                throw new Exception("El id de la finca debe ser mayor a 0.");

            return _fincaDAO.RetrieveById(id);
        }

        public void Create(FincaDTO finca)
        {
            ValidarFinca(finca);
            var now = DateTime.UtcNow;
            finca.FechaRegistro = now;
            finca.FechaActualizacion = now;
            _fincaDAO.Create(finca);
        }

        public void Update(FincaDTO finca)
        {
            if (finca.IdFinca <= 0)
                throw new Exception("El id de la finca es obligatorio para actualizar.");

            ValidarFinca(finca);
            finca.FechaActualizacion = DateTime.UtcNow;
            _fincaDAO.Update(finca);
        }

        public void Delete(int id)
        {
            if (id <= 0)
                throw new Exception("El id de la finca debe ser mayor a 0.");

            _fincaDAO.Delete(id);
        }

        private static void ValidarFinca(FincaDTO finca)
        {
            if (finca == null)
                throw new Exception("La finca es requerida.");

            finca.NombreFinca = finca.NombreFinca?.Trim() ?? string.Empty;
            finca.Provincia = finca.Provincia?.Trim() ?? string.Empty;
            finca.Canton = finca.Canton?.Trim() ?? string.Empty;
            finca.Distrito = finca.Distrito?.Trim() ?? string.Empty;
            finca.Vegetacion = finca.Vegetacion?.Trim() ?? string.Empty;
            finca.UsoSuelo = finca.UsoSuelo?.Trim() ?? string.Empty;
            finca.Pendiente = finca.Pendiente?.Trim() ?? string.Empty;
            finca.EstadoFinca = finca.EstadoFinca?.Trim() ?? string.Empty;

            if (finca.IdPropietario <= 0)
                throw new Exception("El propietario es obligatorio.");

            if (string.IsNullOrWhiteSpace(finca.NombreFinca))
                throw new Exception("El nombre de la finca es obligatorio.");

            if (string.IsNullOrWhiteSpace(finca.Provincia))
                throw new Exception("La provincia es obligatoria.");

            if (string.IsNullOrWhiteSpace(finca.Canton))
                throw new Exception("El cantón es obligatorio.");

            if (string.IsNullOrWhiteSpace(finca.Distrito))
                throw new Exception("El distrito es obligatorio.");

            if (string.IsNullOrWhiteSpace(finca.Vegetacion))
                throw new Exception("La vegetación es obligatoria.");

            if (string.IsNullOrWhiteSpace(finca.UsoSuelo))
                throw new Exception("El uso de suelo es obligatorio.");

            if (string.IsNullOrWhiteSpace(finca.Pendiente))
                throw new Exception("La pendiente es obligatoria.");

            if (string.IsNullOrWhiteSpace(finca.EstadoFinca))
                throw new Exception("El estado de la finca es obligatorio.");

            if (finca.Hectareas <= 0)
                throw new Exception("Las hectáreas deben ser mayores a 0.");

            if (finca.Latitud < -90 || finca.Latitud > 90)
                throw new Exception("La latitud debe estar entre -90 y 90.");

            if (finca.Longitud < -180 || finca.Longitud > 180)
                throw new Exception("La longitud debe estar entre -180 y 180.");

            if (finca.FechaActualizacion != default && finca.FechaRegistro != default &&
                finca.FechaActualizacion < finca.FechaRegistro)
            {
                throw new Exception("La fecha de actualización no puede ser menor a la fecha de registro.");
            }
        }
    }
}
