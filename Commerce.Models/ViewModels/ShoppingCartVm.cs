namespace Commerce.Models.ViewModels
{
    public class ShoppingCartVm
    {
        public IEnumerable<ShoppingCart> ShoppingCarts { get; set; } = null!;
        public OrderHeader OrderHeader { get; set; } = null!;
    }
}
