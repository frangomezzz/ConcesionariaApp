(function () {
    const root = document.querySelector('[data-reports-root]');
    if (!root) return;

    const dataUrl = root.dataset.dataUrl;
    const filter = root.querySelector('[data-reports-filter]');
    const preset = root.querySelector('#reports-preset');
    const customRange = root.querySelector('.reports-custom-range');
    const desde = root.querySelector('#reports-desde');
    const hasta = root.querySelector('#reports-hasta');
    const seller = root.querySelector('#reports-seller');
    const brandModel = root.querySelector('#reports-brand-model');
    const type = root.querySelector('#reports-type');
    const initial = document.querySelector('[data-reports-initial]');
    const charts = {};
    let timer;

    const currency = new Intl.NumberFormat('es-AR', { style: 'currency', currency: 'ARS', maximumFractionDigits: 0 });
    const number = new Intl.NumberFormat('es-AR', { maximumFractionDigits: 0 });

    function formatCurrency(value) { return currency.format(Number(value || 0)); }

    function toggleCustomRange() {
        const custom = preset.value === 'personalizado';
        customRange.hidden = !custom;
        desde.disabled = !custom;
        hasta.disabled = !custom;
    }

    function options(currencyValues) {
        return {
            responsive: true,
            maintainAspectRatio: false,
            animation: { duration: 250 },
            plugins: {
                legend: { display: false },
                tooltip: { callbacks: { label: context => `${context.dataset.label}: ${currencyValues ? formatCurrency(context.raw) : number.format(context.raw)}` } }
            },
            scales: {
                x: { beginAtZero: true, grid: { color: '#e5e8ed' }, ticks: { color: '#536681' } },
                y: { beginAtZero: true, grid: { color: '#e5e8ed' }, ticks: { color: '#536681' } }
            }
        };
    }

    function draw(id, typeName, chart, color, chartOptions) {
        const canvas = document.getElementById(id);
        if (!canvas || !chart || typeof Chart === 'undefined') return;
        const dataset = chart.datasets?.[0] || { label: '', data: [] };
        const data = {
            labels: chart.labels || [],
            datasets: [{
                label: dataset.label,
                data: dataset.data || [],
                backgroundColor: color,
                borderColor: '#172238',
                borderWidth: typeName === 'line' ? 2 : 1,
                borderRadius: typeName === 'bar' ? 4 : 0,
                fill: typeName === 'line',
                tension: typeName === 'line' ? .3 : 0,
                pointBackgroundColor: '#16824f',
                pointRadius: typeName === 'line' ? 3 : 0
            }]
        };
        if (charts[id]) {
            charts[id].data = data;
            charts[id].update();
        } else {
            charts[id] = new Chart(canvas, { type: typeName, data, options: chartOptions });
        }
    }

    function render(data) {
        draw('reports-monthly-sales-chart', 'bar', data.monthlySales, '#b9d0f6', options(false));
        draw('reports-monthly-revenue-chart', 'line', data.monthlyRevenue, '#9edfc5', options(true));
        draw('reports-commission-chart', 'bar', data.commissionsBySeller, '#f4d28d', { ...options(true), indexAxis: 'y' });
        draw('reports-ranking-chart', 'bar', data.sellerRanking, '#cbdcf8', { ...options(false), indexAxis: 'y' });
    }

    function showError(message) {
        let error = root.querySelector('.reports-error');
        if (!error) {
            error = document.createElement('div');
            error.className = 'alert alert-danger reports-error';
            filter.after(error);
        }
        error.textContent = message;
    }

    async function loadData() {
        if (preset.value === 'personalizado' && (!desde.value || !hasta.value || desde.value > hasta.value)) return;
        const url = new URL(dataUrl, window.location.origin);
        url.searchParams.set('preset', preset.value);
        if (preset.value === 'personalizado') {
            url.searchParams.set('desde', desde.value);
            url.searchParams.set('hasta', hasta.value);
        }
        if (seller.value) url.searchParams.set('vendedorId', seller.value);
        if (brandModel.value.trim()) url.searchParams.set('marcaModelo', brandModel.value.trim());
        if (type.value) url.searchParams.set('tipo', type.value);
        root.classList.add('is-loading');
        try {
            const response = await fetch(url, { headers: { Accept: 'application/json' } });
            const data = await response.json();
            if (!response.ok) throw new Error(data.error || 'No se pudieron cargar los reportes.');
            render(data);
        } catch (error) {
            showError(error.message);
        } finally {
            root.classList.remove('is-loading');
        }
    }

    [preset, seller, type].forEach(control => control.addEventListener('change', () => {
        toggleCustomRange();
        loadData();
    }));
    [desde, hasta].forEach(control => control.addEventListener('change', loadData));
    brandModel.addEventListener('input', () => {
        clearTimeout(timer);
        timer = setTimeout(loadData, 300);
    });

    toggleCustomRange();
    if (initial?.textContent) {
        try { render(JSON.parse(initial.textContent)); } catch { loadData(); }
    } else {
        loadData();
    }
})();
