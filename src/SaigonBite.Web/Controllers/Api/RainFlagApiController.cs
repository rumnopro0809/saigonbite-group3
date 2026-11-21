using System.Web.Http;
using SaigonBite.Application;

namespace SaigonBite.Web.Controllers.Api
{
    /// <summary>
    /// REST API cờ mưa lớn toàn hệ thống (FR16). Chỉ có một resource duy nhất tại /api/rain-flag.
    /// </summary>
    [RoutePrefix("api/rain-flag")]
    public class RainFlagApiController : Uc03ApiController
    {
        private readonly RainSettingService _rain;

        public RainFlagApiController()
        {
            _rain = new RainSettingService(Db);
        }

        /// <summary>GET /api/rain-flag — xem cờ mưa hiện tại.</summary>
        [HttpGet, Route("")]
        public RainFlagDto Get()
        {
            return _rain.Get();
        }

        /// <summary>PUT /api/rain-flag — bật hoặc tắt cờ mưa. Chỉ đơn mới bị ảnh hưởng (BR5, BR17).</summary>
        [HttpPut, Route("")]
        public IHttpActionResult Put([FromBody] RainFlagInput input)
        {
            if (input == null) return BadRequestMessage("Thiếu dữ liệu trong nội dung request.");
            return FromResult(_rain.Set(input.IsHeavyRainOn, input.RowVersion));
        }
    }
}
