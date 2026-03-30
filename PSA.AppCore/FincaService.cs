using PSA.DataAccess.DAO;
using PSA.EntidadesDTO.DTOs.Fincas;

namespace PSA.AppCore
{
    public class FincaService
    {
        private readonly FincaDAO _fincaDAO;

        public FincaService(FincaDAO fincaDAO)
        {
            _fincaDAO = fincaDAO;
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
            finca.FechaRegistro = DateTime.Now;
            finca.FechaActualizacion = DateTime.Now;
            _fincaDAO.Create(finca);
        }

        public void Update(FincaDTO finca)
        {
            if (finca.IdFinca <= 0)
                throw new Exception("El id de la finca es obligatorio para actualizar.");

            ValidarFinca(finca);
            finca.FechaActualizacion = DateTime.Now;
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
        }
    }
}
