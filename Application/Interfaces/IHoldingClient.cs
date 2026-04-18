using Domain;

namespace Application.Interfaces
{
    public interface IHoldingClient
    {
        Task<HoldingSnapshot?> Fetch();
    }
}
