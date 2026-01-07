// Vulnerability Import Metrics Dashboard JavaScript

// Chart.js color schemes matching Bootstrap
const ChartColors = {
    critical: '#dc3545',
    high: '#ffc107',
    medium: '#0dcaf0',
    low: '#0d6efd',
    info: '#6c757d',
    success: '#198754',
    danger: '#dc3545',
    warning: '#ffc107',
    primary: '#0d6efd',
    secondary: '#6c757d',
    // STIG status colors
    open: '#dc3545',
    notAFinding: '#198754',
    notApplicable: '#6c757d',
    notReviewed: '#adb5bd',
    // CAT severity colors
    catI: '#dc3545',
    catII: '#ffc107',
    catIII: '#0dcaf0'
};

// Chart instances storage for cleanup
let chartInstances = {};

// Initialize all charts on page load
function initializeCharts(data) {
    // Destroy existing charts before recreating
    Object.values(chartInstances).forEach(chart => {
        if (chart) chart.destroy();
    });
    chartInstances = {};

    // Nessus Severity Doughnut Chart
    if (document.getElementById('nessusSeverityChart') && data.nessusMetrics) {
        chartInstances.nessusSeverity = createDoughnutChart('nessusSeverityChart', {
            labels: ['Critical', 'High', 'Medium', 'Low', 'Info'],
            data: [
                data.nessusMetrics.critical,
                data.nessusMetrics.high,
                data.nessusMetrics.medium,
                data.nessusMetrics.low,
                data.nessusMetrics.info
            ],
            colors: [ChartColors.critical, ChartColors.high, ChartColors.medium, ChartColors.low, ChartColors.info]
        });
    }

    // STIG Status Doughnut Chart
    if (document.getElementById('stigStatusChart') && data.stigMetrics) {
        chartInstances.stigStatus = createDoughnutChart('stigStatusChart', {
            labels: ['Open', 'Not a Finding', 'Not Applicable', 'Not Reviewed'],
            data: [
                data.stigMetrics.open,
                data.stigMetrics.notAFinding,
                data.stigMetrics.notApplicable,
                data.stigMetrics.notReviewed
            ],
            colors: [ChartColors.open, ChartColors.notAFinding, ChartColors.notApplicable, ChartColors.notReviewed]
        });
    }

    // STIG CAT Distribution Bar Chart
    if (document.getElementById('stigCatChart') && data.stigMetrics && data.stigMetrics.bySeverity) {
        const catData = data.stigMetrics.bySeverity;
        chartInstances.stigCat = createCatBarChart('stigCatChart', {
            catOpenData: [
                catData.find(c => c.severity === 'CAT_I')?.open || 0,
                catData.find(c => c.severity === 'CAT_II')?.open || 0,
                catData.find(c => c.severity === 'CAT_III')?.open || 0
            ],
            catTotalData: [
                catData.find(c => c.severity === 'CAT_I')?.total || 0,
                catData.find(c => c.severity === 'CAT_II')?.total || 0,
                catData.find(c => c.severity === 'CAT_III')?.total || 0
            ]
        });
    }

    // Update compliance gauge if present
    if (data.stigMetrics && data.stigMetrics.complianceRate !== undefined) {
        updateComplianceGauge(data.stigMetrics.complianceRate);
    }
}

// Create a doughnut chart
function createDoughnutChart(canvasId, config) {
    const canvas = document.getElementById(canvasId);
    if (!canvas) return null;

    const ctx = canvas.getContext('2d');
    return new Chart(ctx, {
        type: 'doughnut',
        data: {
            labels: config.labels,
            datasets: [{
                data: config.data,
                backgroundColor: config.colors,
                borderWidth: 2,
                borderColor: '#ffffff'
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            plugins: {
                legend: {
                    position: 'bottom',
                    labels: {
                        padding: 15,
                        usePointStyle: true,
                        font: {
                            size: 12
                        }
                    }
                },
                tooltip: {
                    callbacks: {
                        label: function(context) {
                            const total = context.dataset.data.reduce((a, b) => a + b, 0);
                            const value = context.raw;
                            const percentage = total > 0 ? ((value / total) * 100).toFixed(1) : 0;
                            return `${context.label}: ${value.toLocaleString()} (${percentage}%)`;
                        }
                    }
                }
            },
            cutout: '60%'
        }
    });
}

// Create a horizontal bar chart
function createHorizontalBarChart(canvasId, config) {
    const canvas = document.getElementById(canvasId);
    if (!canvas) return null;

    const ctx = canvas.getContext('2d');
    return new Chart(ctx, {
        type: 'bar',
        data: {
            labels: config.labels,
            datasets: [{
                data: config.data,
                backgroundColor: config.colors,
                borderWidth: 0,
                borderRadius: 4
            }]
        },
        options: {
            indexAxis: 'y',
            responsive: true,
            maintainAspectRatio: false,
            plugins: {
                legend: {
                    display: false
                },
                tooltip: {
                    callbacks: {
                        label: function(context) {
                            return context.raw.toLocaleString() + ' findings';
                        }
                    }
                }
            },
            scales: {
                x: {
                    beginAtZero: true,
                    grid: {
                        display: true,
                        color: 'rgba(0,0,0,0.05)'
                    },
                    ticks: {
                        callback: function(value) {
                            return value.toLocaleString();
                        }
                    }
                },
                y: {
                    grid: {
                        display: false
                    }
                }
            }
        }
    });
}

// Create CAT distribution grouped bar chart
function createCatBarChart(canvasId, config) {
    const canvas = document.getElementById(canvasId);
    if (!canvas) return null;

    const ctx = canvas.getContext('2d');
    return new Chart(ctx, {
        type: 'bar',
        data: {
            labels: ['CAT I (High)', 'CAT II (Medium)', 'CAT III (Low)'],
            datasets: [
                {
                    label: 'Open Findings',
                    data: config.catOpenData,
                    backgroundColor: ChartColors.danger,
                    borderRadius: 4
                },
                {
                    label: 'Total Evaluated',
                    data: config.catTotalData,
                    backgroundColor: 'rgba(108, 117, 125, 0.3)',
                    borderRadius: 4
                }
            ]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            plugins: {
                legend: {
                    position: 'bottom',
                    labels: {
                        padding: 15,
                        usePointStyle: true
                    }
                },
                tooltip: {
                    callbacks: {
                        label: function(context) {
                            return `${context.dataset.label}: ${context.raw.toLocaleString()}`;
                        }
                    }
                }
            },
            scales: {
                x: {
                    grid: {
                        display: false
                    }
                },
                y: {
                    beginAtZero: true,
                    grid: {
                        color: 'rgba(0,0,0,0.05)'
                    },
                    ticks: {
                        callback: function(value) {
                            return value.toLocaleString();
                        }
                    }
                }
            }
        }
    });
}

// Update compliance gauge SVG
function updateComplianceGauge(percentage) {
    const gauge = document.querySelector('.compliance-gauge-progress');
    const text = document.querySelector('.compliance-gauge-percentage');

    if (gauge && text) {
        // Calculate stroke-dashoffset for the percentage
        const circumference = 2 * Math.PI * 70; // radius = 70
        const offset = circumference - (percentage / 100) * circumference;

        gauge.style.strokeDasharray = circumference;
        gauge.style.strokeDashoffset = offset;

        // Set color based on percentage
        let color = ChartColors.danger;
        if (percentage >= 90) color = ChartColors.success;
        else if (percentage >= 70) color = ChartColors.primary;
        else if (percentage >= 50) color = ChartColors.warning;

        gauge.style.stroke = color;
        text.textContent = percentage.toFixed(1) + '%';
        text.style.color = color;
    }
}

// Load site breakdown data
function loadSiteBreakdown(sessionId) {
    const container = document.getElementById('siteBreakdownContainer');
    if (!container) return;

    container.innerHTML = '<div class="metrics-loading"><div class="spinner-border"></div><p>Loading site breakdown...</p></div>';

    fetch(`/VulnImport/GetSiteBreakdown?id=${sessionId}`)
        .then(response => response.json())
        .then(sites => {
            container.innerHTML = createSiteBreakdownTable(sites);
            // Add click handlers for expandable rows
            attachSiteRowHandlers(sessionId);
        })
        .catch(error => {
            console.error('Error loading site breakdown:', error);
            container.innerHTML = '<div class="alert alert-danger">Error loading site breakdown data.</div>';
        });
}

// Create site breakdown table HTML
function createSiteBreakdownTable(sites) {
    if (!sites || sites.length === 0) {
        return '<div class="alert alert-info">No site data available.</div>';
    }

    let html = `
        <div class="table-responsive">
            <table class="table table-hover site-breakdown-table">
                <thead>
                    <tr>
                        <th></th>
                        <th>Site</th>
                        <th class="text-center">Hosts</th>
                        <th class="text-center">Critical</th>
                        <th class="text-center">High</th>
                        <th class="text-center">Medium</th>
                        <th class="text-center">CAT I</th>
                        <th class="text-center">CAT II</th>
                        <th class="text-center">CAT III</th>
                    </tr>
                </thead>
                <tbody id="siteBreakdownBody">
    `;

    sites.forEach((site, index) => {
        html += `
            <tr class="site-row-clickable" data-site-name="${escapeHtml(site.siteName)}" data-expanded="false">
                <td class="text-center" style="width: 30px;">
                    <i class="bi bi-chevron-right expand-icon"></i>
                </td>
                <td>
                    <strong>${escapeHtml(site.siteName)}</strong>
                </td>
                <td class="text-center">${site.hostCount}</td>
                <td class="text-center severity-cell">
                    ${site.nessusCritical > 0 ? `<span class="badge bg-danger">${site.nessusCritical}</span>` : '<span class="text-muted">0</span>'}
                </td>
                <td class="text-center severity-cell">
                    ${site.nessusHigh > 0 ? `<span class="badge bg-warning text-dark">${site.nessusHigh}</span>` : '<span class="text-muted">0</span>'}
                </td>
                <td class="text-center severity-cell">
                    ${site.nessusMedium > 0 ? `<span class="badge bg-info">${site.nessusMedium}</span>` : '<span class="text-muted">0</span>'}
                </td>
                <td class="text-center severity-cell">
                    ${site.stigCatI > 0 ? `<span class="badge cat-badge cat-i">${site.stigCatI}</span>` : '<span class="text-muted">0</span>'}
                </td>
                <td class="text-center severity-cell">
                    ${site.stigCatII > 0 ? `<span class="badge cat-badge cat-ii">${site.stigCatII}</span>` : '<span class="text-muted">0</span>'}
                </td>
                <td class="text-center severity-cell">
                    ${site.stigCatIII > 0 ? `<span class="badge cat-badge cat-iii">${site.stigCatIII}</span>` : '<span class="text-muted">0</span>'}
                </td>
            </tr>
            <tr class="host-detail-row" id="hosts-${index}" style="display: none;">
                <td colspan="9" class="p-0">
                    <div class="host-detail-panel p-3 m-2" id="hostContainer-${index}">
                        <div class="metrics-loading"><div class="spinner-border spinner-border-sm"></div> Loading hosts...</div>
                    </div>
                </td>
            </tr>
        `;
    });

    html += '</tbody></table></div>';
    return html;
}

// Attach click handlers for site rows
function attachSiteRowHandlers(sessionId) {
    document.querySelectorAll('.site-row-clickable').forEach((row, index) => {
        row.addEventListener('click', function() {
            const siteName = this.dataset.siteName;
            const isExpanded = this.dataset.expanded === 'true';
            const detailRow = document.getElementById(`hosts-${index}`);
            const icon = this.querySelector('.expand-icon');
            const container = document.getElementById(`hostContainer-${index}`);

            if (isExpanded) {
                // Collapse
                detailRow.style.display = 'none';
                icon.classList.remove('rotated');
                this.dataset.expanded = 'false';
            } else {
                // Expand and load hosts if not already loaded
                detailRow.style.display = 'table-row';
                icon.classList.add('rotated');
                this.dataset.expanded = 'true';

                if (!container.dataset.loaded) {
                    loadHostsForSite(sessionId, siteName, container);
                }
            }
        });
    });
}

// Load hosts for a specific site
function loadHostsForSite(sessionId, siteName, container) {
    fetch(`/VulnImport/GetHostBreakdown?id=${sessionId}&siteName=${encodeURIComponent(siteName)}`)
        .then(response => response.json())
        .then(hosts => {
            container.innerHTML = createHostsTable(hosts);
            container.dataset.loaded = 'true';
        })
        .catch(error => {
            console.error('Error loading hosts:', error);
            container.innerHTML = '<div class="alert alert-danger">Error loading host data.</div>';
        });
}

// Create hosts table HTML
function createHostsTable(hosts) {
    if (!hosts || hosts.length === 0) {
        return '<div class="alert alert-info">No hosts found for this site.</div>';
    }

    let html = `
        <table class="table table-sm mb-0">
            <thead class="table-light">
                <tr>
                    <th>Host</th>
                    <th>IP Address</th>
                    <th>OS</th>
                    <th class="text-center">Nessus Vulns</th>
                    <th class="text-center">STIG Open</th>
                </tr>
            </thead>
            <tbody>
    `;

    hosts.forEach(host => {
        html += `
            <tr class="host-list-item ${host.nessusCritical > 0 ? 'risk-critical-border' : host.nessusHigh > 0 ? 'risk-high-border' : ''}">
                <td>
                    <strong>${escapeHtml(host.dnsName || host.hostName || 'Unknown')}</strong>
                </td>
                <td><small class="text-muted">${escapeHtml(host.ipAddress || 'N/A')}</small></td>
                <td><small class="text-muted">${escapeHtml(host.operatingSystem || 'Unknown')}</small></td>
                <td class="text-center">
                    ${host.nessusTotal > 0 ? `
                        <span class="badge bg-danger me-1" title="Critical">${host.nessusCritical}</span>
                        <span class="badge bg-warning text-dark me-1" title="High">${host.nessusHigh}</span>
                        <span class="badge bg-info" title="Medium">${host.nessusMedium}</span>
                    ` : '<span class="text-muted">-</span>'}
                </td>
                <td class="text-center">
                    ${host.stigOpen > 0 ? `
                        <span class="badge bg-danger">${host.stigOpen}</span>
                    ` : '<span class="text-success">0</span>'}
                </td>
            </tr>
        `;
    });

    html += '</tbody></table>';
    return html;
}

// Escape HTML to prevent XSS
function escapeHtml(text) {
    if (!text) return '';
    const div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
}

// Get risk CSS class
function getRiskClass(riskLevel) {
    const classes = {
        'Critical': 'risk-critical',
        'High': 'risk-high',
        'Medium': 'risk-medium',
        'Low': 'risk-low',
        'Minimal': 'risk-minimal'
    };
    return classes[riskLevel] || 'risk-minimal';
}

// Get compliance color class based on percentage
function getComplianceColorClass(percentage) {
    if (percentage >= 90) return 'bg-success';
    if (percentage >= 70) return 'bg-primary';
    if (percentage >= 50) return 'bg-warning';
    return 'bg-danger';
}

// Export functions for global access
window.VulnMetrics = {
    initializeCharts,
    updateComplianceGauge,
    loadSiteBreakdown,
    ChartColors
};
