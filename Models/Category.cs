using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace BookWeb.Models
{
    public class Category
    {
        [Key]
        public int Id { get; set; }
        [Required(ErrorMessage = "This field is required")]
        [MaxLength(30)]
        [DisplayName("Category Name")]
        public string Name { get; set; }
        [DisplayName("Display Order")]
        [Required(ErrorMessage = "This field is required")]
        [Range(1, 100, ErrorMessage="Display Order must be between 1-100")]
        public int DisplayOrder { get; set; }
    }
}
