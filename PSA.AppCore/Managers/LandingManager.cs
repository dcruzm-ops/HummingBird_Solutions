using PSA.DataAccess.DAO;
using PSA.EntidadesDTO.DTOs.Landing;

namespace PSA.AppCore.Managers
{
    public class LandingManager
    {
        private readonly LandingDAO _landingDao;

        public LandingManager(LandingDAO landingDao)
        {
            _landingDao = landingDao;
        }

        public async Task<LandingContenidoDTO> ObtenerEquipoAsync()
        {
            var data = await _landingDao.ObtenerContenidoEquipoAsync();
            return new LandingContenidoDTO
            {
                Titulo = data.Titulo,
                Descripcion = data.Descripcion
            };
        }

        public async Task<LandingContenidoDTO> ObtenerProductoAsync()
        {
            var data = await _landingDao.ObtenerContenidoProductoAsync();
            return new LandingContenidoDTO
            {
                Titulo = data.Titulo,
                Descripcion = data.Descripcion
            };
        }
    }
}
