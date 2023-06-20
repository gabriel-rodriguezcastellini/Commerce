using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Commerce.Models
{
    public class OrderDetail
    {
        public int Id { get; set; }

        [Required]
        public int OrderHeaderId { get; set; }

        [ValidateNever]
        [ForeignKey("OrderHeaderId")]
        public OrderHeader OrderHeader { get; set; } = null!;

        [Required]
        public int ProductId { get; set; }

        [ValidateNever]
        [ForeignKey("ProductId")]
        public Product Product { get; set; } = null!;

        public int Count { get; set; }
        public double Price { get; set; }
    }
}
