using System;
using System.Collections.Generic;

namespace SaigonBite.Application.Menu
{
    /// <summary>
    /// Input payload for creating or updating a virtual brand (UC02, FR08, FR10).
    /// </summary>
    public sealed class BrandInput
    {
        public BrandInput()
        {
            HubIds = new List<int>();
        }

        public string Name { get; set; }
        public string Category { get; set; }
        public string Description { get; set; }
        public bool IsActive { get; set; }
        public List<int> HubIds { get; set; }
    }

    /// <summary>
    /// Input payload for creating or updating a menu item (UC02, FR08, FR10, BR12).
    /// </summary>
    public sealed class MenuItemInput
    {
        public int BrandId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public decimal Price { get; set; }
        public string ImageUrl { get; set; }
        public bool IsAvailable { get; set; }
    }

    /// <summary>
    /// Filter options for Admin brand/menu catalog listing (UC02, FR09).
    /// </summary>
    public sealed class MenuFilter
    {
        public string Search { get; set; }
        public int? BrandId { get; set; }
        public string Category { get; set; }
        public bool? IsAvailable { get; set; }
        public bool IncludeDeleted { get; set; }
    }

    /// <summary>
    /// Structured result carrying field-level validation errors for MVC ModelState binding.
    /// </summary>
    public sealed class MenuResult<T>
    {
        private MenuResult()
        {
            FieldErrors = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        public bool Succeeded { get; private set; }
        public T Data { get; private set; }
        public string ErrorCode { get; private set; }
        public IDictionary<string, string> FieldErrors { get; private set; }

        public static MenuResult<T> Ok(T data)
        {
            return new MenuResult<T>
            {
                Succeeded = true,
                Data = data
            };
        }

        public static MenuResult<T> Fail(string errorCode, string field, string message)
        {
            var result = new MenuResult<T>
            {
                Succeeded = false,
                ErrorCode = errorCode
            };
            result.FieldErrors[field ?? string.Empty] = message;
            return result;
        }

        public static MenuResult<T> Fail(string errorCode, IDictionary<string, string> errors)
        {
            var result = new MenuResult<T>
            {
                Succeeded = false,
                ErrorCode = errorCode
            };
            if (errors != null)
            {
                foreach (var pair in errors)
                {
                    result.FieldErrors[pair.Key] = pair.Value;
                }
            }
            return result;
        }
    }
}
