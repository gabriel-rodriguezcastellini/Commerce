namespace Commerce.Models.ViewModels
{
    public class OrderVm
	{
        public OrderHeader OrderHeader { get; set; } = null!;
        public IEnumerable<OrderDetail> OrderDetails { get; set; } = null!;
    }
}
