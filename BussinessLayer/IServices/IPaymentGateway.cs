using System.Collections.Generic;

namespace BussinessLayer.IServices
{
    // STUB: This file is a stub for TV1 to implement later.
    // Created so TV2's code can compile.
    public interface IPaymentGateway
    {
        string GetGatewayName();
        string CreatePaymentUrl(DTOs.PaymentRequest request);
        bool ValidateCallback(Dictionary<string, string> queryParams);
    }
}
