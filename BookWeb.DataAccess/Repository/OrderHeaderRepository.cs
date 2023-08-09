using BookWeb.DataAccess.Data;
using BookWeb.DataAccess.Repository.IRepository;
using BookWeb.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookWeb.DataAccess.Repository
{
    public class OrderHeaderRepository : Repository<OrderHeader>, IOrderHeaderRepository
    {
        private readonly ApplicationDbContext _context;
        public OrderHeaderRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }

        public void Update(OrderHeader orderHeader) => _context.OrderHeaders.Update(orderHeader);

		public void UpdateStatus(int orderHeaderId, string orderStatus, string? paymentStatus = null)
		{
			var orderHeader = _context.OrderHeaders.FirstOrDefault(oh => oh.Id == orderHeaderId);
			if (orderHeader != null) 
			{
				orderHeader.OrderStatus = orderStatus;

				if (!string.IsNullOrEmpty(paymentStatus))
					orderHeader.PaymentStatus = paymentStatus;
			}
		}

		public void UpdateStripePaymentId(int orderHeaderId, string sessionId, string paymentIntentId)
		{
			var orderHeader = _context.OrderHeaders.FirstOrDefault(oh => oh.Id == orderHeaderId);

			if (orderHeader != null)
			{
				//sessionId generates when the user tries to make a payment, and if it successfull - generates paymentIntentId
				if (!string.IsNullOrEmpty(sessionId))
					orderHeader.SessionId = sessionId;

				if (!string.IsNullOrEmpty(paymentIntentId))
				{
					orderHeader.PaymentIntentId = paymentIntentId;
					orderHeader.PaymentDate = DateTime.Now;
				}

			}
		}
	}
}
