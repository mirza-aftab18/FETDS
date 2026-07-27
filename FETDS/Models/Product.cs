using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using FETDS.Data;

namespace FETDS.Models
{
    public class Product
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        [Required]
        public double Quantity { get; set; }

        [Required]
        [StringLength(20)]
        public string Unit { get; set; } = string.Empty; // e.g. "kg", "pieces", "liters"

        [Required]
        [DataType(DataType.Date)]
        public DateTime ExpiryDate { get; set; }

        [Required]
        [StringLength(20)]
        public string Status { get; set; } = "Available"; // Available, Pending, Reserved, Donated, Expired

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Foreign key to the Donor who listed this product
        public string DonorId { get; set; } = string.Empty;

        [ForeignKey(nameof(DonorId))]
        public ApplicationUser? Donor { get; set; }
        [Required]
        public ProductCategory Category { get; set; }
        public string? ImagePath { get; set; }
    }
}