using Commerce.DataAccess.Data;
using Commerce.DataAccess.Repository.IRepository;
using Commerce.Models;

namespace Commerce.DataAccess.Repository
{
    public class OrderHeaderRepository : Repository<OrderHeader>, IOrderHeaderRepository
	{
		private readonly ApplicationDbContext _context;

		public OrderHeaderRepository(ApplicationDbContext context) : base(context)
		{
			_context = context;
		}

		public void Update(OrderHeader orderHeader)
		{
			_context.OrderHeaders.Update(orderHeader);
		}

		public void UpdateStatus(int id, string orderStatus, string? paymentStatus = null)
		{
			var order = _context.OrderHeaders.FirstOrDefault(x => x.Id == id);
			if (order != null)
			{
				order.OrderStatus = orderStatus;
				if (!string.IsNullOrEmpty(paymentStatus))
				{
					order.PaymentStatus = paymentStatus;
				}
			}
		}

		public void UpdateStripePaymentId(int id, string sessionId, string paymentIntentId)
		{
			var order = _context.OrderHeaders.FirstOrDefault(x => x.Id == id);
			if (!string.IsNullOrEmpty(sessionId))
			{
				order!.SessionId = sessionId;
			}
			if (!string.IsNullOrEmpty(paymentIntentId))
			{
				order!.PaymentIntentId = paymentIntentId;
				order.PaymentDate = DateTime.Now;
			}
		}
	}
}
