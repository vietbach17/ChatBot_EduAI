using BussinessLayer.IServices;
using BussinessLayer.IGateways;
using BussinessLayer.Gateways;
using System;
using System.Collections.Generic;
using System.Linq;
using BussinessLayer.IServices;

namespace BussinessLayer.Gateways
{
    /// <summary>
    /// Factory Pattern tạo đối tượng Cổng thanh toán phù hợp (VNPay/PayOS/SePay) dựa trên tên phương thức thanh toán.
    /// </summary>
    public class PaymentGatewayFactory
    {
        private readonly IEnumerable<IPaymentGateway> _gateways;

        public PaymentGatewayFactory(IEnumerable<IPaymentGateway> gateways)
        {
            _gateways = gateways;
        }

        public IPaymentGateway GetGateway(string paymentMethod)
        {
            var gateway = _gateways.FirstOrDefault(g => g.GetGatewayName().Equals(paymentMethod, StringComparison.OrdinalIgnoreCase));
            if (gateway == null)
            {
                throw new NotSupportedException($"Cổng thanh toán {paymentMethod} chưa được hỗ trợ.");
            }
            return gateway;
        }
    }
}

