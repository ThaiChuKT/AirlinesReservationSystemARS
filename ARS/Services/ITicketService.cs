using System.Threading.Tasks;

namespace ARS.Services
{
    public interface ITicketService
    {
        Task<(bool ok, string? error)> BlockAsync(int reservationId);
        Task<(bool ok, string? error)> ConfirmAsync(int reservationId);
    }
}
