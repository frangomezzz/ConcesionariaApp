(function () {
    const form = document.querySelector('[data-audit-filter]');
    if (!form) return;

    const preset = form.querySelector('#audit-preset');
    const customRange = form.querySelector('.audit-custom-range');
    const desde = form.querySelector('#audit-desde');
    const hasta = form.querySelector('#audit-hasta');
    const controls = form.querySelectorAll('select');

    function updateRange() {
        const custom = preset.value === 'personalizado';
        customRange.hidden = !custom;
        desde.disabled = !custom;
        hasta.disabled = !custom;
    }

    controls.forEach(control => control.addEventListener('change', () => {
        updateRange();
        if (control !== preset || preset.value !== 'personalizado') form.submit();
    }));

    [desde, hasta].forEach(input => input.addEventListener('change', () => {
        if (desde.value && hasta.value && desde.value <= hasta.value) form.submit();
    }));

    updateRange();
})();
