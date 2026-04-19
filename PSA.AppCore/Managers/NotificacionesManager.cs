using PSA.DataAccess.DAO;
using PSA.EntidadesDTO.DTOs.Notificaciones;

namespace PSA.AppCore.Managers;

public class NotificacionesManager
{
    private readonly NotificacionDAO _notificacionDao;

    public NotificacionesManager(NotificacionDAO notificacionDao)
    {
        _notificacionDao = notificacionDao;
    }

    public Task<List<NotificacionDTO>> ObtenerPorUsuarioAsync(int idUsuario, int maximo = 30)
    {
        if (idUsuario <= 0)
        {
            throw new InvalidOperationException("Debe indicar un usuario válido.");
        }

        return _notificacionDao.ObtenerPorUsuarioAsync(idUsuario, maximo);
    }

    public Task<int> MarcarLeidasAsync(int idUsuario)
    {
        if (idUsuario <= 0)
        {
            throw new InvalidOperationException("Debe indicar un usuario válido.");
        }

        return _notificacionDao.MarcarLeidasAsync(idUsuario);
    }
}
