using PSA.DataAccess.DAO;
using PSA.EntidadesDTO.DTOs.Fincas;
using PSA.EntidadesDTO.Entidades.Fincas;

namespace PSA.AppCore
{
    public class FincaEvidenciaService
    {
        private readonly FincaEvidenciaDAO _fincaEvidenciaDAO;

        private static readonly string[] ExtensionesPermitidas =
        {
            ".jpg", ".jpeg", ".png", ".pdf"
        };

        private const long TamanoMaximo = 10 * 1024 * 1024;

        public FincaEvidenciaService(FincaEvidenciaDAO fincaEvidenciaDAO)
        {
            _fincaEvidenciaDAO = fincaEvidenciaDAO;
        }

        public void ValidarArchivo(string nombreArchivo, long tamano)
        {
            var extension = Path.GetExtension(nombreArchivo).ToLowerInvariant();

            if (!ExtensionesPermitidas.Contains(extension))
            {
                throw new Exception("Solo se permiten archivos JPG, JPEG, PNG y PDF.");
            }

            if (tamano <= 0)
            {
                throw new Exception("El archivo está vacío.");
            }

            if (tamano > TamanoMaximo)
            {
                throw new Exception("El archivo supera el tamaño máximo permitido de 10 MB.");
            }
        }

        public async Task<int> CrearAsync(FincaEvidencia evidencia)
        {
            return await _fincaEvidenciaDAO.CrearAsync(evidencia);
        }

        public async Task<List<FincaEvidenciaDTO>> ObtenerPorFincaAsync(int idFinca)
        {
            return await _fincaEvidenciaDAO.ObtenerPorFincaAsync(idFinca);
        }

        public async Task<FincaEvidenciaDTO?> ObtenerPorIdAsync(int idEvidencia)
        {
            return await _fincaEvidenciaDAO.ObtenerPorIdAsync(idEvidencia);
        }
    }
}