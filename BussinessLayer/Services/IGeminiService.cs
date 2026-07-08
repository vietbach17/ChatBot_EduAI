using System.Threading.Tasks;

namespace BussinessLayer.Services
{
    public interface IGeminiService
    {
        Task<string> GenerateJsonContentAsync(string prompt, string? responseSchemaJson = null);
    }
}
