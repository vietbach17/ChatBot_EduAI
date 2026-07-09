using System.Collections.Generic;
using System.Linq;
using BussinessLayer.IServices;

namespace BussinessLayer.Services
{
    // STUB: This file is a stub for TV1 to implement later.
    public class PaymentGatewayFactory
    {
        private readonly IEnumerable<IPaymentGateway> _gateways;

        public PaymentGatewayFactory(IEnumerable<IPaymentGateway> gateways)
        {
            _gateways = gateways;
        }

        public IPaymentGateway GetGateway(string method)
        {
            return _gateways.FirstOrDefault(g => g.GetGatewayName() == method);
        }
    }
}
