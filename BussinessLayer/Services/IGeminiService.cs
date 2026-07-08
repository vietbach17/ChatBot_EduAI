using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BussinessLayer.Services
{
    public interface IGeminiService
    {
        Task<float[]> GetEmbeddingAsync(string text);
    }
}
