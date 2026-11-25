/*
 * Script của các trang UC03.
 * Các nút bật/tắt cờ mưa, ngừng nhận đơn và xóa bếp gọi REST API (fetch), không gửi form.
 */
(function () {
    'use strict';

    // Ngôn ngữ trang (NFR03): layout đặt <html lang="en"> hoặc "vi" theo cookie sb_lang.
    var en = document.documentElement.lang === 'en';
    function t(vi, english) { return en ? english : vi; }

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

    /** Đổi kết quả lỗi của API thành câu cho người dùng, theo ngôn ngữ đang chọn. */
    function errorText(result) {
        if (result.status === 409) return t('Dữ liệu vừa được người khác sửa. Hãy tải lại trang rồi thử lại.', 'Someone else just changed this data. Reload the page and try again.');
        if (result.data && result.data.errors && result.data.errors.length) {
            return result.data.errors.map(function (e) { return e.message; }).join(' ');
        }
        if (result.data && result.data.message) return result.data.message;
        return t('Có lỗi xảy ra (mã ', 'Something went wrong (code ') + result.status + ').';
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
                document.getElementById('rain-state').textContent = result.data.isHeavyRainOn ? t('Đang bật', 'On') : t('Đang tắt', 'Off');
                flash('success', result.data.isHeavyRainOn
                    ? t('Đã bật cờ mưa. Chỉ các đơn đặt sau đó bị tính phí mưa.', 'Heavy rain is on. Only orders placed from now on pay the rain fee.')
                    : t('Đã tắt cờ mưa.', 'Heavy rain is off.'));
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
                        button.textContent = t('Bấm lần nữa để xác nhận xóa', 'Select again to confirm deletion');
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
