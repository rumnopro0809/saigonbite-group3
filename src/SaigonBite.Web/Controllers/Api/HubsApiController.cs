using System;
using System.Collections.Generic;
using System.Net;
using System.Web.Http;
using SaigonBite.Application;

namespace SaigonBite.Web.Controllers.Api
{
    /// <summary>
    /// REST API quản lý bếp (FR14). Mỗi bếp là một "resource" tại /api/hubs/{id}.
    /// GET xem · POST tạo · PUT sửa toàn bộ · PATCH đổi trạng thái hoạt động · DELETE xóa mềm.
    /// </summary>
    [RoutePrefix("api/hubs")]
    public class HubsApiController : Uc03ApiController
    {
        private readonly HubService _hubs;

        public HubsApiController()
        {
            _hubs = new HubService(Db);
        }

        /// <summary>GET /api/hubs — danh sách bếp kèm tải hiện tại. Trả 200.</summary>
        [HttpGet, Route("")]
        public IList<HubDto> GetAll()
        {
            return _hubs.List();
        }

        /// <summary>GET /api/hubs/5 — chi tiết một bếp. Trả 200, hoặc 404 nếu không có.</summary>
        [HttpGet, Route("{id:int}", Name = "GetHubById")]
        public IHttpActionResult GetById(int id)
        {
            return FromResult(_hubs.Get(id));
        }

        /// <summary>POST /api/hubs — tạo bếp. Trả 201 kèm địa chỉ bếp mới trong header Location, hoặc 400 nếu sai dữ liệu.</summary>
        [HttpPost, Route("")]
        public IHttpActionResult Create([FromBody] HubInput input)
        {
            if (input == null) return BadRequestMessage("Thiếu dữ liệu bếp trong nội dung request.");
            var result = _hubs.Create(input);
            if (result.Status != ResultStatus.Created) return FromResult(result);
            string location = Url.Link("GetHubById", new { id = result.Value.Id });
            return Created(new Uri(location), result.Value);
        }

        /// <summary>PUT /api/hubs/5 — sửa bếp. Trả 200, 400 (sai dữ liệu), 404, hoặc 409 (có người sửa trước).</summary>
        [HttpPut, Route("{id:int}")]
        public IHttpActionResult Update(int id, [FromBody] HubInput input)
        {
            if (input == null) return BadRequestMessage("Thiếu dữ liệu bếp trong nội dung request.");
            return FromResult(_hubs.Update(id, input));
        }

        /// <summary>PATCH /api/hubs/5 — bật hoặc ngừng nhận đơn mới (FR14.4). Body: { "isActive": false, "rowVersion": "..." }.</summary>
        [HttpPatch, Route("{id:int}")]
        public IHttpActionResult SetActive(int id, [FromBody] HubActiveInput input)
        {
            if (input == null) return BadRequestMessage("Thiếu dữ liệu trong nội dung request.");
            return FromResult(_hubs.SetActive(id, input.IsActive, input.RowVersion));
        }

        /// <summary>
        /// DELETE /api/hubs/5?rowVersion=... — xóa mềm bếp (FR14.4). Trả 204 khi thành công.
        /// rowVersion nằm trên query vì request DELETE không nên có body.
        /// </summary>
        [HttpDelete, Route("{id:int}")]
        public IHttpActionResult Delete(int id, string rowVersion = null)
        {
            var result = _hubs.Delete(id, rowVersion);
            if (result.IsSuccess) return StatusCode(HttpStatusCode.NoContent);
            return FromResult(result);
        }
    }
}
