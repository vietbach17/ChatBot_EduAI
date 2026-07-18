using System.Threading.Tasks;
using DataAccessLayer.Entities;

namespace DataAccessLayer.IRepositories
{
    /// <summary>Repository nhật ký tiêu thụ token AI.</summary>
    public interface ITokenUsageLogRepository
    {
        Task AddAsync(TokenUsageLog log);
    }
}
