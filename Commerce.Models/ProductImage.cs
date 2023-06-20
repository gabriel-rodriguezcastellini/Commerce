using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Commerce.Models
{
    public class ProductImage
    {
        public int Id { get; set; }

        [Required]
        public string ImageUrl { get; set; } = null!;

        public int ProductId { get; set; }
        
        [ForeignKey("ProductId")]
        public Product Product { get; set; } = null!;
    }
}
