/*
 * Script của các trang UC03.
 * Các nút bật/tắt cờ mưa, ngừng nhận đơn và xóa bếp gọi REST API (fetch), không gửi form.
 */
(function () {
    'use strict';

    /** Hiện thông báo ngắn ở vùng #sb-flash. kind là "success" hoặc "danger". */
    function flash(kind, text) {
        var box = document.getElementById('sb-flash');
        if (!box) return;
        box.className = 'alert alert-' + kind;
        box.textContent = text;
    }

    /** Gọi API và trả về { ok, status, data }. Body (nếu có) được gửi dạng JSON. */
    function callApi(method, url, body) {
        var options = { method: method, credentials: 'same-origin', headers: { 'Accept': 'application/json' } };
        if (body !== undefined) {
            options.headers['Content-Type'] = 'application/json';
            options.body = JSON.stringify(body);
        }
        return fetch(url, options).then(function (res) {
            // Mã 204 không có nội dung.
            var parse = res.status === 204 ? Promise.resolve(null) : res.json().catch(function () { return null; });
            return parse.then(function (data) { return { ok: res.ok, status: res.status, data: data }; });
        });
    }

    /** Đổi kết quả lỗi của API thành câu tiếng Việt cho người dùng. */
    function errorText(result) {
        if (result.status === 409) return 'Dữ liệu vừa được người khác sửa. Hãy tải lại trang rồi thử lại.';
        if (result.data && result.data.errors && result.data.errors.length) {
            return result.data.errors.map(function (e) { return e.message; }).join(' ');
        }
        if (result.data && result.data.message) return result.data.message;
        return 'Có lỗi xảy ra (mã ' + result.status + ').';
    }

    // ---- Cờ mưa (FR16): PUT /api/rain-flag ----
    var rainButton = document.getElementById('rain-toggle');
    if (rainButton) {
        rainButton.addEventListener('click', function () {
            var turnOn = rainButton.getAttribute('data-on') !== 'true';
            callApi('PUT', rainButton.getAttribute('data-url'), {
                isHeavyRainOn: turnOn,
                rowVersion: rainButton.getAttribute('data-rowversion')
            }).then(function (result) {
                if (!result.ok) { flash('danger', errorText(result)); return; }
                rainButton.setAttribute('data-on', result.data.isHeavyRainOn ? 'true' : 'false');
                rainButton.setAttribute('data-rowversion', result.data.rowVersion);
                document.getElementById('rain-state').textContent = result.data.isHeavyRainOn ? 'Đang bật' : 'Đang tắt';
                flash('success', result.data.isHeavyRainOn
                    ? 'Đã bật cờ mưa. Chỉ các đơn đặt sau đó bị tính phí mưa.'
                    : 'Đã tắt cờ mưa.');
            });
        });
    }

    // ---- Trang chi tiết bếp: PATCH (ngừng/nhận lại đơn) và DELETE (xóa mềm) ----
    var detail = document.getElementById('hub-detail');
    if (detail) {
        var apiUrl = detail.getAttribute('data-api');

        document.querySelectorAll('[data-hub-action]').forEach(function (button) {
            var armed = false; // dùng cho nút xóa: bấm lần 1 để xác nhận, lần 2 mới xóa

            button.addEventListener('click', function () {
                var action = button.getAttribute('data-hub-action');
                var rowVersion = detail.getAttribute('data-rowversion');

                if (action === 'toggle-active') {
                    var isActive = detail.getAttribute('data-active') === 'true';
                    callApi('PATCH', apiUrl, { isActive: !isActive, rowVersion: rowVersion }).then(function (result) {
                        if (result.ok) { window.location.reload(); } else { flash('danger', errorText(result)); }
                    });
                } else if (action === 'delete') {
                    if (!armed) {
                        armed = true;
                        button.textContent = 'Bấm lần nữa để xác nhận xóa';
                        return;
                    }
                    callApi('DELETE', apiUrl + '?rowVersion=' + encodeURIComponent(rowVersion)).then(function (result) {
                        if (result.ok) { window.location.href = detail.getAttribute('data-list-url'); }
                        else { flash('danger', errorText(result)); }
                    });
                }
            });
        });
    }
})();
