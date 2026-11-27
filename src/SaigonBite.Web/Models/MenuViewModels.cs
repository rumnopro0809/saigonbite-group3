using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using SaigonBite.Domain.Entities;

namespace SaigonBite.Web.Models
{
    public sealed class MenuIndexViewModel
    {
        public MenuIndexViewModel()
        {
            Items = new List<MenuItem>();
            Brands = new List<Brand>();
            Categories = new List<string>();
        }

        public string Lang { get; set; }
        public string Search { get; set; }
        public int? BrandId { get; set; }
        public string Category { get; set; }
        public bool? IsAvailable { get; set; }
        public bool IncludeDeleted { get; set; }

        public IList<MenuItem> Items { get; set; }
        public IList<Brand> Brands { get; set; }
        public IList<string> Categories { get; set; }
    }

    public sealed class MenuItemFormViewModel
    {
        public MenuItemFormViewModel()
        {
            Brands = new List<Brand>();
            IsAvailable = true;
            ImageUrl = "/Content/images/hero.jpg";
        }

        public int Id { get; set; }
        public string Lang { get; set; }

        [Required(ErrorMessage = "Please choose a brand.")]
        public int BrandId { get; set; }

        [Required(ErrorMessage = "Menu item name is required.")]
        [MaxLength(120)]
        public string Name { get; set; }

        [MaxLength(500)]
        public string Description { get; set; }

        [Range(typeof(decimal), "0", "999999999", ErrorMessage = "Unit price cannot be negative.")]
        public decimal Price { get; set; }

        [MaxLength(500)]
        public string ImageUrl { get; set; }

        public bool IsAvailable { get; set; }

        public IList<Brand> Brands { get; set; }
    }

    public sealed class CustomerHubMenuViewModel
    {
        public CustomerHubMenuViewModel()
        {
            Hubs = new List<Hub>();
            Items = new List<MenuItem>();
        }

        public string Lang { get; set; }
        public int SelectedHubId { get; set; }
        public Hub SelectedHub { get; set; }
        public IList<Hub> Hubs { get; set; }
        public IList<MenuItem> Items { get; set; }
    }

    public sealed class BrandIndexViewModel
    {
        public BrandIndexViewModel()
        {
            Brands = new List<Brand>();
            Hubs = new List<Hub>();
        }

        public string Lang { get; set; }
        public bool IncludeDeleted { get; set; }
        public IList<Brand> Brands { get; set; }
        public IList<Hub> Hubs { get; set; }
    }

    public sealed class BrandFormViewModel
    {
        public BrandFormViewModel()
        {
            SelectedHubIds = new List<int>();
            AllHubs = new List<Hub>();
            IsActive = true;
        }

        public int Id { get; set; }
        public string Lang { get; set; }

        [Required(ErrorMessage = "Brand name is required.")]
        [MaxLength(120)]
        public string Name { get; set; }

        [Required(ErrorMessage = "Brand category is required for R1 reporting.")]
        [MaxLength(80)]
        public string Category { get; set; }

        [MaxLength(500)]
        public string Description { get; set; }

        public bool IsActive { get; set; }

        public List<int> SelectedHubIds { get; set; }
        public IList<Hub> AllHubs { get; set; }
    }
}
