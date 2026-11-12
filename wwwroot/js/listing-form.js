(function () {
    var attrs = document.getElementById('attrs');
    if (!attrs) return;

    function reindex() {
        attrs.querySelectorAll('.attr-row').forEach(function (row, i) {
            var ins = row.querySelectorAll('input');
            ins[0].name = 'Attributes[' + i + '].Name';
            ins[1].name = 'Attributes[' + i + '].Value';
        });
    }

    document.getElementById('addAttr').addEventListener('click', function () {
        if (attrs.querySelectorAll('.attr-row').length >= 15) return;
        var div = document.createElement('div');
        div.className = 'row g-2 mb-2 attr-row';
        div.innerHTML =
            '<div class="col-5"><input class="form-control" placeholder="الاسم" maxlength="60" /></div>' +
            '<div class="col-5"><input class="form-control" placeholder="القيمة" maxlength="200" /></div>' +
            '<div class="col-2"><button type="button" class="btn btn-outline-danger w-100 remove-attr">حذف</button></div>';
        attrs.appendChild(div);
        reindex();
    });

    attrs.addEventListener('click', function (e) {
        if (e.target.classList.contains('remove-attr')) {
            e.target.closest('.attr-row').remove();
            reindex();
        }
    });

    attrs.closest('form').addEventListener('submit', reindex);

    var pt = document.getElementById('pricingType'), pl = document.getElementById('priceLabel');
    function label() { pl.textContent = pt.value === '2' ? 'سعر الكيلو (ج.م)' : 'سعر الحيوان الواحد (ج.م)'; }
    pt.addEventListener('change', label);
    label();
})();
