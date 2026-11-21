using System.Linq;
using System.Web.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace SaigonBite.Web
{
    /// <summary>Cấu hình REST API (Web API 2). Địa chỉ API do attribute [Route] trên từng controller quyết định.</summary>
    public static class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
            config.MapHttpAttributeRoutes();

            // API chỉ trả JSON. Tên trường viết kiểu camelCase, enum hiển thị bằng chữ (ví dụ "Quiet").
            config.Formatters.Remove(config.Formatters.XmlFormatter);
            var settings = config.Formatters.JsonFormatter.SerializerSettings;
            settings.ContractResolver = new CamelCasePropertyNamesContractResolver();
            settings.Converters.Add(new StringEnumConverter());
            settings.NullValueHandling = NullValueHandling.Include;
            settings.DateFormatHandling = DateFormatHandling.IsoDateFormat;
        }
    }
}
