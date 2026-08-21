(function () {
    const root = document.querySelector('[data-dashboard-root]');
    if (!root) {
        return;
    }

    const role = root.dataset.dashboardRole;
    const dataUrl = root.dataset.dataUrl;
    const preset = root.querySelector('.dashboard-preset');
    const customRange = root.querySelector('.dashboard-custom-range');
    const desde = root.querySelector('#dashboard-desde');
    const hasta = root.querySelector('#dashboard-hasta');
    const initialElement = root.parentElement.querySelector('[data-dashboard-initial]');
    const charts = {};
    let customTimer;

    const currency = new Intl.NumberFormat('es-AR', {
        style: 'currency',
        currency: 'ARS',
        maximumFractionDigits: 0
    });

    const number = new Intl.NumberFormat('es-AR', { maximumFractionDigits: 0 });

    function formatCurrency(value) {
        return currency.format(Number(value || 0));
    }

    function escapeHtml(value) {
        return String(value ?? '')
            .replaceAll('&', '&amp;')
            .replaceAll('<', '&lt;')
            .replaceAll('>', '&gt;')
            .replaceAll('"', '&quot;')
            .replaceAll("'", '&#039;');
    }

    function toggleCustomRange() {
        const isCustom = preset.value === 'personalizado';
        customRange.hidden = !isCustom;
        customRange.querySelectorAll('input').forEach(input => input.disabled = !isCustom);
    }

    function setMetric(name, value, isCurrency) {
        const element = root.querySelector(`[data-metric="${name}"]`);
        if (element) {
            element.textContent = isCurrency ? formatCurrency(value) : number.format(Number(value || 0));
        }
    }

    function updateLabels() {
        const label = root.querySelector('#sales-label');
        if (!label) return;
        const suffix = preset.value === 'este-mes' ? 'este mes' : 'del período';
        label.textContent = role === 'admin' ? `Ventas ${suffix}` : `Mis ventas ${suffix}`;
    }

    function chartOptions(horizontal) {
        return {
            responsive: true,
            maintainAspectRatio: false,
            animation: { duration: 250 },
            plugins: {
                legend: { display: false },
                tooltip: {
                    callbacks: {
                        label: context => {
                            const value = context.parsed.y ?? context.parsed.x ?? context.parsed;
                            return `${context.dataset.label}: ${typeof value === 'number' ? (context.dataset.label.includes('Comision') ? formatCurrency(value) : number.format(value)) : value}`;
                        }
                    }
                }
            },
            scales: {
                x: {
                    beginAtZero: true,
                    grid: { display: !horizontal, color: '#e5e8ed' },
                    ticks: { color: '#536681' }
                },
                y: {
                    beginAtZero: true,
                    grid: { display: horizontal, color: '#e5e8ed' },
                    ticks: { color: '#536681' }
                }
            }
        };
    }

    function drawChart(id, type, chart, options, colors) {
        const canvas = document.getElementById(id);
        if (!canvas || !chart || typeof Chart === 'undefined') return;

        const dataset = chart.datasets?.[0] || { label: '', data: [] };
        const data = {
            labels: chart.labels || [],
            datasets: [{
                label: dataset.label,
                data: dataset.data || [],
                backgroundColor: colors,
                borderColor: '#172238',
                borderWidth: type === 'line' ? 2 : 1,
                borderRadius: type === 'bar' ? 4 : 0,
                fill: type === 'line',
                tension: type === 'line' ? .35 : 0,
                pointBackgroundColor: '#16824f',
                pointRadius: type === 'line' ? 4 : 0
            }]
        };

        if (charts[id]) {
            charts[id].data = data;
            charts[id].update();
            return;
        }

        charts[id] = new Chart(canvas, { type, data, options });
    }

    function renderCharts(data) {
        const monthlyType = role === 'seller' ? 'line' : 'bar';
        drawChart('monthly-sales-chart', monthlyType, data.monthlySales, chartOptions(false), '#cbdcf8');

        if (role !== 'admin') return;

        drawChart('seller-sales-chart', 'bar', data.salesBySeller, {
            ...chartOptions(true),
            indexAxis: 'y'
        }, '#b9d0f6');
        drawChart('seller-commission-chart', 'bar', data.commissionsBySeller, chartOptions(false), '#9edfc5');
        drawChart('vehicle-type-chart', 'doughnut', data.salesByVehicleType, {
            responsive: true,
            maintainAspectRatio: false,
            plugins: {
                legend: { position: 'bottom', labels: { color: '#536681', padding: 16 } },
                tooltip: {
                    callbacks: { label: context => `${context.label}: ${number.format(context.raw)}` }
                }
            }
        }, ['#b9d0f6', '#9edfc5', '#f4d28d', '#f5a6a6']);
    }

    function renderSales(data) {
        const rows = root.querySelector('[data-sales-rows]');
        if (!rows) return;
        const colspan = role === 'admin' ? 7 : 6;
        if (!data.latestSales?.length) {
            rows.innerHTML = `<tr><td colspan="${colspan}" class="empty-state">No hay ventas para el rango seleccionado.</td></tr>`;
            return;
        }

        rows.innerHTML = data.latestSales.map(sale => {
            const stateClass = sale.estado === 'Activa' ? 'status-activo' : 'status-anulada';
            const sellerCell = role === 'admin' ? `<td>${escapeHtml(sale.vendedor)}</td>` : '';
            return `<tr class="${sale.estado === 'Anulada' ? 'row-inactive' : ''}">
                <td>${new Date(sale.fecha).toLocaleDateString('es-AR', { day: '2-digit', month: 'short', year: 'numeric' })}</td>
                <td><strong>${escapeHtml(sale.cliente)}</strong></td>
                <td><a class="sale-table-link" href="/Ventas/Detalle/${Number(sale.id)}">${escapeHtml(sale.vehiculo)}</a></td>
                ${sellerCell}
                <td class="money">${formatCurrency(sale.precioFinal)}</td>
                <td class="money commission-value">${formatCurrency(sale.comision)}</td>
                <td><span class="status-badge ${stateClass}">${escapeHtml(sale.estado)}</span></td>
            </tr>`;
        }).join('');
    }

    function render(data) {
        const cards = role === 'admin' ? data.adminCards : data.sellerCards;
        if (!cards) return;
        setMetric('sales', cards.sales, false);
        setMetric('total-facturado', cards.totalFacturado, true);
        setMetric('total-vendido', cards.totalVendido, true);
        setMetric('vehiculos-vendidos', cards.vehiculosVendidos, false);
        setMetric('comisiones', cards.comisiones, true);
        setMetric('vendedores-activos', cards.vendedoresActivos, false);
        renderCharts(data);
        renderSales(data);
    }

    function showError(message) {
        let error = root.querySelector('.dashboard-error');
        if (!error) {
            error = document.createElement('div');
            error.className = 'alert alert-danger dashboard-error';
            root.querySelector('.dashboard-heading').after(error);
        }
        error.textContent = message;
    }

    async function loadData() {
        if (preset.value === 'personalizado' && (!desde.value || !hasta.value || desde.value > hasta.value)) {
            return;
        }

        const url = new URL(dataUrl, window.location.origin);
        url.searchParams.set('preset', preset.value);
        if (preset.value === 'personalizado') {
            url.searchParams.set('desde', desde.value);
            url.searchParams.set('hasta', hasta.value);
        }

        root.classList.add('is-loading');
        try {
            const response = await fetch(url, { headers: { Accept: 'application/json' } });
            const data = await response.json();
            if (!response.ok) throw new Error(data.error || 'No se pudieron cargar los datos del dashboard.');
            render(data);
        } catch (error) {
            showError(error.message);
        } finally {
            root.classList.remove('is-loading');
        }
    }

    preset.addEventListener('change', () => {
        toggleCustomRange();
        updateLabels();
        loadData();
    });

    [desde, hasta].forEach(input => input.addEventListener('change', () => {
        clearTimeout(customTimer);
        customTimer = setTimeout(loadData, 150);
    }));

    toggleCustomRange();
    updateLabels();
    if (initialElement?.textContent) {
        try {
            render(JSON.parse(initialElement.textContent));
        } catch {
            loadData();
        }
    } else {
        loadData();
    }
})();
