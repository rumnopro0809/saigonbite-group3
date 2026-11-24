using System.Collections.Generic;
using SaigonBite.Domain.Entities;

namespace SaigonBite.Web.Models
{
    public class HomeViewModel
    {
        public string Lang { get; set; }
        public bool IsHeavyRainOn { get; set; }
        public List<Hub> Hubs { get; set; }
        public List<MenuItem> MenuItems { get; set; }
    }
}
