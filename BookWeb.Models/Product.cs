using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookWeb.Models
{
    public class Product
    {
        [Key]
        public int Id { get; set; }
        [Required(ErrorMessage = "This field is required")]
        public string Title { get; set; }
        public string Description { get; set; }
        [Required(ErrorMessage = "This field is required")]
        public string ISBN { get; set; }
        [Required(ErrorMessage = "This field is required")]
        public string Author { get; set; }
        [Required(ErrorMessage = "This field is required")]
        [Display(Name = "List Price")]
        [Range(1, 1000, ErrorMessage = "Price must be between 1-1000")]
        public double ListPrice { get; set; }

        [Required(ErrorMessage = "This field is required")]
        [Display(Name = "Price for 1-50")]
        [Range(1, 1000, ErrorMessage = "Price for 1-50 must be between 1-1000")]
        public double Price { get; set; }

        [Required(ErrorMessage = "This field is required")]
        [Display(Name = "Price for 50-100")]
        [Range(1, 1000, ErrorMessage = "Price for 50-100 must be between 1-1000")]
        public double Price50 { get; set; }

        [Required(ErrorMessage = "This field is required")]
        [Display(Name = "Price for 100+")]
        [Range(1, 1000, ErrorMessage = "Price for 100+ must be between 1-1000")]
        public double Price100 { get; set; }

        [Display(Name = "Category")]
        public int CategoryId { get; set; }
        [ForeignKey("CategoryId")]
        [ValidateNever]
        public Category Category { get; set; }
        [ValidateNever]
        public string ImageUrl { get; set; }
    }
}
