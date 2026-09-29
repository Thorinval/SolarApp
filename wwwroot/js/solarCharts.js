window.solarCharts = {
    _state: new WeakMap(),

    renderMonthlyProductionChart(canvas, payload) {
        return this._renderLineChart(canvas, {
            title: 'Production solaire mensuelle : théorique, réelle et projection',
            labels: payload.labels || [],
            ySuffix: ' kWh',
            suggestedMax: null,
            series: [
                { name: 'Production Théorique', values: payload.theoretical || [], color: '#2e75b6' },
                { name: 'Production réelle 2026', values: payload.real2026 || [], color: '#ed7d31' },
                { name: 'Projection 2026', values: payload.projection2026 || [], color: '#2f9e44', dashed: true, alpha: 0.75 },
                { name: 'Production réelle 2027', values: payload.real2027 || [], color: '#7030a0' },
                { name: 'Projection 2027', values: payload.projection2027 || [], color: '#00b0f0', dashed: true, alpha: 0.75 }
            ]
        });
    },

    renderProjectionVsRealIndexChart(canvas, payload) {
        return this._renderLineChart(canvas, {
            title: 'Production solaire : projection vs réel (indices 2026/2027)',
            labels: payload.labels || [],
            ySuffix: '',
            suggestedMax: 100,
            series: [
                { name: 'Projection production 2026 (indice)', values: payload.projectionIndex2026 || [], color: '#2e75b6', dashed: true, alpha: 0.75 },
                { name: 'Production réelle 2026 (indice)', values: payload.realIndex2026 || [], color: '#ed7d31' },
                { name: 'Projection production 2027 (indice)', values: payload.projectionIndex2027 || [], color: '#7030a0', dashed: true, alpha: 0.75 },
                { name: 'Production réelle 2027 (indice)', values: payload.realIndex2027 || [], color: '#00b0f0' }
            ]
        });
    },

    renderEnergyDistributionChart(canvas, payload) {
        return this._renderStackedAreaChart(canvas, {
            title: 'Répartition énergie consommée (%)',
            labels: payload.labels || [],
            ySuffix: '%',
            suggestedMax: 100,
            series: [
                { name: 'Conso réseau (%)', values: payload.consoReseau || [], color: '#dc143c' },
                { name: 'Soutirée batterie (%)', values: payload.soutirerBatterie || [], color: '#ffa500' },
                { name: 'Conso solaire (%)', values: payload.consoSolaire || [], color: '#9acd32' }
            ]
        });
    },

    renderGroupedBarChart(canvas, payload) {
        return this._renderGroupedBarChart(canvas, {
            title: payload.title || '',
            labels: payload.labels || [],
            valueSuffix: payload.valueSuffix || '',
            valueDecimals: typeof payload.valueDecimals === 'number' ? payload.valueDecimals : 0,
            series: payload.series || []
        });
    },

    renderBillEvolutionChart(canvas, payload) {
        return this._renderBillEvolutionChart(canvas, {
            title: payload.title || '',
            labels: payload.labels || [],
            barValueSuffix: payload.barValueSuffix || '',
            barValueDecimals: typeof payload.barValueDecimals === 'number' ? payload.barValueDecimals : 0,
            lineValueSuffix: payload.lineValueSuffix || '',
            lineValueDecimals: typeof payload.lineValueDecimals === 'number' ? payload.lineValueDecimals : 0,
            rightAxisMin: typeof payload.rightAxisMin === 'number' ? payload.rightAxisMin : 0,
            rightAxisMax: typeof payload.rightAxisMax === 'number' ? payload.rightAxisMax : 100,
            barSeries: payload.barSeries || [],
            lineSeries: payload.lineSeries || []
        });
    },

    _renderLineChart(canvas, config) {
        if (!canvas) return 'canvas-null';

        const draw = () => {
            const labelCount = (config.labels || []).length;
            const seriesCount = (config.series || []).length;
            const parent = canvas.parentElement;
            const width = Math.max(300, parent?.clientWidth || 900);
            const height = Math.max(320, parent?.clientHeight || 520);
            canvas.width = width;
            canvas.height = height;

            const ctx = canvas.getContext('2d');
            if (!ctx) return;

            ctx.clearRect(0, 0, width, height);
            ctx.fillStyle = '#ffffff';
            ctx.fillRect(0, 0, width, height);

            const margin = { top: 55, right: 24, bottom: 95, left: 70 };
            const plotW = width - margin.left - margin.right;
            const plotH = height - margin.top - margin.bottom;
            if (plotW <= 0 || plotH <= 0) return;

            const labels = config.labels || [];
            const series = (config.series || []).filter(s => Array.isArray(s.values));

            let maxY = 0;
            series.forEach(s => s.values.forEach(v => {
                if (typeof v === 'number' && isFinite(v)) maxY = Math.max(maxY, v);
            }));
            if (config.suggestedMax && maxY < config.suggestedMax) maxY = config.suggestedMax;
            if (maxY <= 0) maxY = 10;
            maxY = Math.ceil(maxY / 10) * 10;

            const xStep = labels.length > 1 ? plotW / (labels.length - 1) : plotW;
            const xAt = i => margin.left + (labels.length > 1 ? i * xStep : plotW / 2);
            const yAt = v => margin.top + plotH - (v / maxY) * plotH;

            // Title
            ctx.fillStyle = '#4a4a4a';
            const titleFontSize = Math.max(16, Math.min(22, Math.floor(width / 52)));
            ctx.font = `${titleFontSize}px Segoe UI, Arial`;
            ctx.textAlign = 'center';
            ctx.fillText(config.title || '', width / 2, 34);

            // Grid + Y ticks
            const gridCount = 6;
            ctx.font = '14px Segoe UI, Arial';
            ctx.textAlign = 'right';
            for (let i = 0; i <= gridCount; i++) {
                const v = (maxY / gridCount) * i;
                const y = yAt(v);
                ctx.strokeStyle = '#d9d9d9';
                ctx.lineWidth = 1;
                ctx.beginPath();
                ctx.moveTo(margin.left, y);
                ctx.lineTo(width - margin.right, y);
                ctx.stroke();

                ctx.fillStyle = '#666';
                const suffix = config.ySuffix || '';
                ctx.fillText(`${Math.round(v)}${suffix}`, margin.left - 8, y + 4);
            }

            // X labels
            ctx.textAlign = 'right';
            ctx.fillStyle = '#666';
            labels.forEach((lbl, i) => {
                const x = xAt(i);
                const y = margin.top + plotH + 20;
                ctx.save();
                ctx.translate(x, y);
                ctx.rotate(-Math.PI / 4);
                ctx.fillText(lbl || '', 0, 0);
                ctx.restore();
            });

            // Series
            series.forEach(s => {
                const alpha = typeof s.alpha === 'number' ? s.alpha : 1;
                ctx.strokeStyle = this._hexToRgba(s.color, alpha);
                ctx.fillStyle = this._hexToRgba(s.color, alpha);
                ctx.lineWidth = 3;
                ctx.setLineDash(s.dashed ? [10, 6] : []);
                ctx.beginPath();
                let started = false;
                s.values.forEach((v, i) => {
                    if (typeof v !== 'number' || !isFinite(v)) return;
                    const x = xAt(i);
                    const y = yAt(v);
                    if (!started) {
                        ctx.moveTo(x, y);
                        started = true;
                    } else {
                        ctx.lineTo(x, y);
                    }
                });
                ctx.stroke();
                ctx.setLineDash([]);

                s.values.forEach((v, i) => {
                    if (typeof v !== 'number' || !isFinite(v)) return;
                    const x = xAt(i);
                    const y = yAt(v);
                    ctx.beginPath();
                    ctx.arc(x, y, 3, 0, Math.PI * 2);
                    ctx.fill();
                });
            });

            // Legend
            let legendX = margin.left;
            const legendY = height - 24;
            ctx.textAlign = 'left';
            ctx.font = '14px Segoe UI, Arial';
            series.forEach(s => {
                const alpha = typeof s.alpha === 'number' ? s.alpha : 1;
                ctx.strokeStyle = this._hexToRgba(s.color, alpha);
                ctx.lineWidth = 3;
                ctx.setLineDash(s.dashed ? [10, 6] : []);
                ctx.beginPath();
                ctx.moveTo(legendX, legendY - 5);
                ctx.lineTo(legendX + 24, legendY - 5);
                ctx.stroke();
                ctx.setLineDash([]);
                ctx.fillStyle = '#4a4a4a';
                ctx.fillText(s.name, legendX + 28, legendY);
                legendX += 28 + ctx.measureText(s.name).width + 20;
            });
        };

        const previous = this._state.get(canvas);
        if (previous?.onResize) {
            window.removeEventListener('resize', previous.onResize);
        }

        const onResize = () => draw();
        window.addEventListener('resize', onResize);
        this._state.set(canvas, { onResize });

        draw();

        const points = (config.series || [])
            .flatMap(s => Array.isArray(s.values) ? s.values : [])
            .filter(v => typeof v === 'number' && isFinite(v)).length;

        return `ok labels=${(config.labels || []).length} series=${(config.series || []).length} points=${points}`;
    },

    _renderStackedAreaChart(canvas, config) {
        if (!canvas) return 'canvas-null';

        const draw = () => {
            const parent = canvas.parentElement;
            const width = Math.max(300, parent?.clientWidth || 900);
            const height = Math.max(320, parent?.clientHeight || 400);
            canvas.width = width;
            canvas.height = height;

            const ctx = canvas.getContext('2d');
            if (!ctx) return;

            ctx.clearRect(0, 0, width, height);
            ctx.fillStyle = '#ffffff';
            ctx.fillRect(0, 0, width, height);

            const margin = { top: 55, right: 24, bottom: 95, left: 70 };
            const plotW = width - margin.left - margin.right;
            const plotH = height - margin.top - margin.bottom;
            if (plotW <= 0 || plotH <= 0) return;

            const labels = config.labels || [];
            const series = (config.series || []).filter(s => Array.isArray(s.values));

            // Calculate stacked values
            const stackedData = [];
            labels.forEach((_, monthIdx) => {
                const point = { values: [0] };
                series.forEach((s, seriesIdx) => {
                    const val = typeof s.values[monthIdx] === 'number' && isFinite(s.values[monthIdx]) 
                        ? s.values[monthIdx] 
                        : 0;
                    point.values[seriesIdx + 1] = point.values[seriesIdx] + val;
                });
                stackedData.push(point);
            });

            const xStep = labels.length > 1 ? plotW / (labels.length - 1) : plotW;
            const xAt = i => margin.left + (labels.length > 1 ? i * xStep : plotW / 2);
            const yAt = v => margin.top + plotH - (v / 100) * plotH;

            // Title
            ctx.fillStyle = '#4a4a4a';
            const titleFontSize = Math.max(16, Math.min(22, Math.floor(width / 52)));
            ctx.font = `${titleFontSize}px Segoe UI, Arial`;
            ctx.textAlign = 'center';
            ctx.fillText(config.title || '', width / 2, 34);

            // Grid + Y ticks
            const gridCount = 5;
            ctx.font = '14px Segoe UI, Arial';
            ctx.textAlign = 'right';
            for (let i = 0; i <= gridCount; i++) {
                const v = (100 / gridCount) * i;
                const y = yAt(v);
                ctx.strokeStyle = '#d9d9d9';
                ctx.lineWidth = 1;
                ctx.beginPath();
                ctx.moveTo(margin.left, y);
                ctx.lineTo(width - margin.right, y);
                ctx.stroke();

                ctx.fillStyle = '#666';
                ctx.fillText(`${Math.round(v)}%`, margin.left - 8, y + 4);
            }

            // X labels
            ctx.textAlign = 'right';
            ctx.fillStyle = '#666';
            labels.forEach((lbl, i) => {
                const x = xAt(i);
                const y = margin.top + plotH + 20;
                ctx.save();
                ctx.translate(x, y);
                ctx.rotate(-Math.PI / 4);
                ctx.fillText(lbl || '', 0, 0);
                ctx.restore();
            });

            // Draw stacked areas (in reverse order for proper layering)
            for (let seriesIdx = series.length - 1; seriesIdx >= 0; seriesIdx--) {
                const s = series[seriesIdx];
                const alpha = 0.8;
                ctx.fillStyle = this._hexToRgba(s.color, alpha);
                ctx.beginPath();

                let started = false;
                stackedData.forEach((point, monthIdx) => {
                    const x = xAt(monthIdx);
                    const y = yAt(point.values[seriesIdx + 1]);
                    if (!started) {
                        ctx.moveTo(x, y);
                        started = true;
                    } else {
                        ctx.lineTo(x, y);
                    }
                });

                // Close the path by going back along the previous series level
                for (let monthIdx = stackedData.length - 1; monthIdx >= 0; monthIdx--) {
                    const point = stackedData[monthIdx];
                    const x = xAt(monthIdx);
                    const y = yAt(point.values[seriesIdx]);
                    ctx.lineTo(x, y);
                }

                ctx.closePath();
                ctx.fill();
            }

            // Legend
            let legendX = margin.left;
            const legendY = height - 24;
            ctx.textAlign = 'left';
            ctx.font = '14px Segoe UI, Arial';
            series.forEach(s => {
                ctx.fillStyle = this._hexToRgba(s.color, 0.8);
                ctx.fillRect(legendX, legendY - 12, 16, 12);
                ctx.fillStyle = '#4a4a4a';
                ctx.fillText(s.name, legendX + 20, legendY);
                legendX += 20 + ctx.measureText(s.name).width + 20;
            });
        };

        const previous = this._state.get(canvas);
        if (previous?.onResize) {
            window.removeEventListener('resize', previous.onResize);
        }

        const onResize = () => draw();
        window.addEventListener('resize', onResize);
        this._state.set(canvas, { onResize });

        draw();

        const points = (config.series || [])
            .flatMap(s => Array.isArray(s.values) ? s.values : [])
            .filter(v => typeof v === 'number' && isFinite(v)).length;

        return `ok labels=${(config.labels || []).length} series=${(config.series || []).length} points=${points}`;
    },

    _renderGroupedBarChart(canvas, config) {
        if (!canvas || typeof canvas.getContext !== 'function') return 'canvas-invalid';

        const draw = () => {
            const parent = canvas.parentElement;
            const width = Math.max(300, parent?.clientWidth || 900);
            const height = Math.max(320, parent?.clientHeight || 420);
            canvas.width = width;
            canvas.height = height;

            const ctx = canvas.getContext('2d');
            if (!ctx) return;

            ctx.clearRect(0, 0, width, height);
            ctx.fillStyle = '#ffffff';
            ctx.fillRect(0, 0, width, height);

            const margin = { top: 55, right: 24, bottom: 90, left: 70 };
            const plotW = width - margin.left - margin.right;
            const plotH = height - margin.top - margin.bottom;
            if (plotW <= 0 || plotH <= 0) return;

            const labels = config.labels || [];
            const series = (config.series || []).filter(s => Array.isArray(s.values));

            let maxY = 0;
            let minY = 0;
            series.forEach(s => s.values.forEach(v => {
                if (typeof v === 'number' && isFinite(v)) {
                    maxY = Math.max(maxY, v);
                    minY = Math.min(minY, v);
                }
            }));
            if (config.suggestedMax && maxY < config.suggestedMax) maxY = config.suggestedMax;
            if (maxY === 0 && minY === 0) maxY = 10;
            if (maxY > 0) maxY = Math.ceil(maxY / 10) * 10;
            if (minY < 0) minY = Math.floor(minY / 10) * 10;

            const groupWidth = labels.length > 0 ? plotW / labels.length : plotW;
            const barGap = 8;
            const barsCount = Math.max(1, series.length);
            const clusterWidth = Math.max(0, groupWidth * 0.68);
            const barWidth = Math.max(6, Math.min(30, (clusterWidth - (barGap * (barsCount - 1))) / barsCount));
            const totalBarsWidth = barsCount * barWidth + (barsCount - 1) * barGap;
            const xAt = i => margin.left + (groupWidth * i) + (groupWidth - totalBarsWidth) / 2;
            const valueRange = maxY - minY || 1;
            const yAt = v => margin.top + plotH - ((v - minY) / valueRange) * plotH;
            const zeroY = yAt(0);
            const formatValue = v => `${Number(v).toLocaleString('fr-FR', { minimumFractionDigits: config.valueDecimals ?? 0, maximumFractionDigits: config.valueDecimals ?? 0 })}${config.valueSuffix || ''}`;

            ctx.fillStyle = '#4a4a4a';
            const titleFontSize = Math.max(16, Math.min(22, Math.floor(width / 52)));
            ctx.font = `${titleFontSize}px Segoe UI, Arial`;
            ctx.textAlign = 'center';
            ctx.fillText(config.title || '', width / 2, 34);

            const gridCount = 5;
            ctx.font = '14px Segoe UI, Arial';
            ctx.textAlign = 'right';
            for (let i = 0; i <= gridCount; i++) {
                const v = minY + ((valueRange / gridCount) * i);
                const y = yAt(v);
                ctx.strokeStyle = '#d9d9d9';
                ctx.lineWidth = 1;
                ctx.beginPath();
                ctx.moveTo(margin.left, y);
                ctx.lineTo(width - margin.right, y);
                ctx.stroke();

                ctx.fillStyle = '#666';
                ctx.fillText(formatValue(v), margin.left - 8, y + 4);
            }

            if (zeroY >= margin.top && zeroY <= margin.top + plotH) {
                ctx.strokeStyle = '#8a8a8a';
                ctx.lineWidth = 1.5;
                ctx.beginPath();
                ctx.moveTo(margin.left, zeroY);
                ctx.lineTo(width - margin.right, zeroY);
                ctx.stroke();
            }

            ctx.textAlign = 'center';
            ctx.fillStyle = '#666';
            labels.forEach((lbl, i) => {
                const x = margin.left + (groupWidth * i) + (groupWidth / 2);
                const y = margin.top + plotH + 20;
                ctx.fillText(lbl || '', x, y);
            });

            series.forEach((s, seriesIdx) => {
                const color = s.color || '#1f5d86';
                const alpha = typeof s.alpha === 'number' ? s.alpha : 0.95;
                labels.forEach((_, monthIdx) => {
                    const value = typeof s.values[monthIdx] === 'number' && isFinite(s.values[monthIdx]) ? s.values[monthIdx] : null;
                    if (value === null) return;

                    const x = xAt(monthIdx) + seriesIdx * (barWidth + barGap);
                    const yValue = yAt(value);
                    const y = value >= 0 ? yValue : zeroY;
                    const barHeight = Math.abs(zeroY - yValue);
                    ctx.fillStyle = this._hexToRgba(color, alpha);
                    ctx.fillRect(x, y, barWidth, barHeight);
                });
            });

            let legendX = margin.left;
            const legendY = height - 22;
            ctx.textAlign = 'left';
            ctx.font = '14px Segoe UI, Arial';
            series.forEach(s => {
                ctx.fillStyle = this._hexToRgba(s.color || '#1f5d86', 0.95);
                ctx.fillRect(legendX, legendY - 12, 16, 12);
                ctx.fillStyle = '#4a4a4a';
                ctx.fillText(s.name || '', legendX + 20, legendY);
                legendX += 20 + ctx.measureText(s.name || '').width + 20;
            });
        };

        const previous = this._state.get(canvas);
        if (previous?.onResize) {
            window.removeEventListener('resize', previous.onResize);
        }

        const onResize = () => draw();
        window.addEventListener('resize', onResize);
        this._state.set(canvas, { onResize });

        draw();

        const points = (config.series || [])
            .flatMap(s => Array.isArray(s.values) ? s.values : [])
            .filter(v => typeof v === 'number' && isFinite(v)).length;

        return `ok labels=${(config.labels || []).length} series=${(config.series || []).length} points=${points}`;
    },

    _renderBillEvolutionChart(canvas, config) {
        if (!canvas || typeof canvas.getContext !== 'function') return 'canvas-invalid';

        let barRects = [];
        let tooltip = null;

        const formatNumber = (value, decimals, suffix) => `${Number(value).toLocaleString('fr-FR', { minimumFractionDigits: decimals, maximumFractionDigits: decimals })}${suffix || ''}`;

        const drawTooltip = (ctx, width, height) => {
            if (!tooltip) return;

            const lines = [tooltip.label || '', `${tooltip.seriesName || ''}: ${tooltip.formattedValue || ''}`].filter(Boolean);
            if (lines.length === 0) return;

            ctx.font = '13px Segoe UI, Arial';
            const padding = 8;
            const lineHeight = 16;
            const textWidth = Math.max(...lines.map(line => ctx.measureText(line).width));
            const boxWidth = textWidth + padding * 2;
            const boxHeight = (lines.length * lineHeight) + padding * 2;

            let x = tooltip.x + 12;
            let y = tooltip.y - boxHeight - 12;
            if (x + boxWidth > width - 8) x = width - boxWidth - 8;
            if (x < 8) x = 8;
            if (y < 8) y = tooltip.y + 12;
            if (y + boxHeight > height - 8) y = height - boxHeight - 8;

            ctx.fillStyle = 'rgba(32, 32, 32, 0.92)';
            ctx.strokeStyle = 'rgba(255, 255, 255, 0.18)';
            ctx.lineWidth = 1;
            ctx.beginPath();
            ctx.rect(x, y, boxWidth, boxHeight);
            ctx.fill();
            ctx.stroke();

            ctx.fillStyle = '#ffffff';
            ctx.textAlign = 'left';
            ctx.font = '13px Segoe UI, Arial';
            lines.forEach((line, index) => {
                ctx.fillText(line, x + padding, y + padding + 13 + (index * lineHeight));
            });
        };

        const draw = () => {
            const parent = canvas.parentElement;
            const width = Math.max(300, parent?.clientWidth || 900);
            const height = Math.max(320, parent?.clientHeight || 460);
            canvas.width = width;
            canvas.height = height;

            const ctx = canvas.getContext('2d');
            if (!ctx) return;

            ctx.clearRect(0, 0, width, height);
            ctx.fillStyle = '#ffffff';
            ctx.fillRect(0, 0, width, height);

            const margin = { top: 55, right: 52, bottom: 90, left: 70 };
            const plotW = width - margin.left - margin.right;
            const plotH = height - margin.top - margin.bottom;
            if (plotW <= 0 || plotH <= 0) return;

            const labels = config.labels || [];
            const barSeries = (config.barSeries || []).filter(s => Array.isArray(s.values));
            const lineSeries = (config.lineSeries || []).filter(s => Array.isArray(s.values));

            let maxY = 0;
            let minY = 0;
            barSeries.forEach(s => s.values.forEach(v => {
                if (typeof v === 'number' && isFinite(v)) {
                    maxY = Math.max(maxY, v);
                    minY = Math.min(minY, v);
                }
            }));
            if (maxY === 0 && minY === 0) maxY = 10;
            if (maxY > 0) maxY = Math.ceil(maxY / 10) * 10;
            if (minY < 0) minY = Math.floor(minY / 10) * 10;

            const leftRange = maxY - minY || 1;
            const rightMin = config.rightAxisMin;
            const rightMax = config.rightAxisMax;
            const rightRange = rightMax - rightMin || 1;

            const groupWidth = labels.length > 0 ? plotW / labels.length : plotW;
            const barGap = 8;
            const barsCount = Math.max(1, barSeries.length);
            const clusterWidth = Math.max(0, groupWidth * 0.68);
            const barWidth = Math.max(6, Math.min(30, (clusterWidth - (barGap * (barsCount - 1))) / barsCount));
            const totalBarsWidth = barsCount * barWidth + (barsCount - 1) * barGap;
            const xAt = i => margin.left + (groupWidth * i) + (groupWidth - totalBarsWidth) / 2;
            const xCenter = i => margin.left + (groupWidth * i) + (groupWidth / 2);
            const yLeft = v => margin.top + plotH - ((v - minY) / leftRange) * plotH;
            const yRight = v => margin.top + plotH - ((v - rightMin) / rightRange) * plotH;
            const barZeroY = yLeft(0);
            const barValueFormat = v => formatNumber(v, config.barValueDecimals ?? 0, config.barValueSuffix || '');
            const lineValueFormat = v => formatNumber(v, config.lineValueDecimals ?? 0, config.lineValueSuffix || '');

            ctx.fillStyle = '#4a4a4a';
            const titleFontSize = Math.max(16, Math.min(22, Math.floor(width / 52)));
            ctx.font = `${titleFontSize}px Segoe UI, Arial`;
            ctx.textAlign = 'center';
            ctx.fillText(config.title || '', width / 2, 34);

            const gridCount = 5;
            ctx.font = '14px Segoe UI, Arial';
            for (let i = 0; i <= gridCount; i++) {
                const leftValue = minY + ((leftRange / gridCount) * i);
                const rightValue = rightMin + ((rightRange / gridCount) * i);
                const y = yLeft(leftValue);
                ctx.strokeStyle = '#d9d9d9';
                ctx.lineWidth = 1;
                ctx.beginPath();
                ctx.moveTo(margin.left, y);
                ctx.lineTo(width - margin.right, y);
                ctx.stroke();

                ctx.fillStyle = '#666';
                ctx.textAlign = 'right';
                ctx.fillText(barValueFormat(leftValue), margin.left - 8, y + 4);
                ctx.textAlign = 'left';
                ctx.fillText(lineValueFormat(rightValue), width - margin.right + 8, y + 4);
            }

            if (barZeroY >= margin.top && barZeroY <= margin.top + plotH) {
                ctx.strokeStyle = '#8a8a8a';
                ctx.lineWidth = 1.5;
                ctx.beginPath();
                ctx.moveTo(margin.left, barZeroY);
                ctx.lineTo(width - margin.right, barZeroY);
                ctx.stroke();
            }

            ctx.textAlign = 'center';
            ctx.fillStyle = '#666';
            labels.forEach((lbl, i) => {
                const x = xCenter(i);
                const y = margin.top + plotH + 20;
                ctx.fillText(lbl || '', x, y);
            });

            barRects = [];
            barSeries.forEach((s, seriesIdx) => {
                const color = s.color || '#1f5d86';
                const alpha = typeof s.alpha === 'number' ? s.alpha : 0.95;
                labels.forEach((_, monthIdx) => {
                    const value = typeof s.values[monthIdx] === 'number' && isFinite(s.values[monthIdx]) ? s.values[monthIdx] : null;
                    if (value === null) return;

                    const x = xAt(monthIdx) + seriesIdx * (barWidth + barGap);
                    const yValue = yLeft(value);
                    const y = value >= 0 ? yValue : barZeroY;
                    const barHeight = Math.abs(barZeroY - yValue);
                    ctx.fillStyle = this._hexToRgba(color, alpha);
                    ctx.fillRect(x, y, barWidth, barHeight);
                    barRects.push({
                        x,
                        y,
                        w: barWidth,
                        h: barHeight,
                        label: labels[monthIdx] || '',
                        seriesName: s.name || '',
                        value,
                        formattedValue: barValueFormat(value),
                        color,
                        monthIdx,
                        seriesIdx
                    });
                });
            });

            lineSeries.forEach(s => {
                const color = s.color || '#7030a0';
                const alpha = typeof s.alpha === 'number' ? s.alpha : 0.95;
                ctx.strokeStyle = this._hexToRgba(color, alpha);
                ctx.fillStyle = this._hexToRgba(color, alpha);
                ctx.lineWidth = 3;
                ctx.beginPath();
                let started = false;
                labels.forEach((_, monthIdx) => {
                    const value = typeof s.values[monthIdx] === 'number' && isFinite(s.values[monthIdx]) ? s.values[monthIdx] : null;
                    if (value === null) {
                        started = false;
                        return;
                    }

                    const x = xCenter(monthIdx);
                    const y = yRight(value);
                    if (!started) {
                        ctx.moveTo(x, y);
                        started = true;
                    } else {
                        ctx.lineTo(x, y);
                    }
                });
                ctx.stroke();

                labels.forEach((_, monthIdx) => {
                    const value = typeof s.values[monthIdx] === 'number' && isFinite(s.values[monthIdx]) ? s.values[monthIdx] : null;
                    if (value === null) return;

                    const x = xCenter(monthIdx);
                    const y = yRight(value);
                    ctx.beginPath();
                    ctx.arc(x, y, 4, 0, Math.PI * 2);
                    ctx.fill();
                });
            });

            let legendX = margin.left;
            const legendY = height - 22;
            ctx.textAlign = 'left';
            ctx.font = '14px Segoe UI, Arial';
            barSeries.forEach(s => {
                ctx.fillStyle = this._hexToRgba(s.color || '#1f5d86', 0.95);
                ctx.fillRect(legendX, legendY - 12, 16, 12);
                ctx.fillStyle = '#4a4a4a';
                ctx.fillText(s.name || '', legendX + 20, legendY);
                legendX += 20 + ctx.measureText(s.name || '').width + 20;
            });

            lineSeries.forEach(s => {
                const color = s.color || '#7030a0';
                ctx.strokeStyle = this._hexToRgba(color, 0.95);
                ctx.fillStyle = this._hexToRgba(color, 0.95);
                ctx.beginPath();
                ctx.moveTo(legendX, legendY - 6);
                ctx.lineTo(legendX + 16, legendY - 6);
                ctx.stroke();
                ctx.beginPath();
                ctx.arc(legendX + 8, legendY - 6, 3.5, 0, Math.PI * 2);
                ctx.fill();
                ctx.fillStyle = '#4a4a4a';
                ctx.fillText(s.name || '', legendX + 24, legendY);
                legendX += 24 + ctx.measureText(s.name || '').width + 20;
            });

            drawTooltip(ctx, width, height);
        };

        const previous = this._state.get(canvas);
        if (previous?.onResize) {
            window.removeEventListener('resize', previous.onResize);
        }
        if (previous?.onMouseMove) {
            canvas.removeEventListener('mousemove', previous.onMouseMove);
        }
        if (previous?.onMouseLeave) {
            canvas.removeEventListener('mouseleave', previous.onMouseLeave);
        }

        const getMousePosition = event => {
            const rect = canvas.getBoundingClientRect();
            const scaleX = canvas.width / rect.width;
            const scaleY = canvas.height / rect.height;
            return {
                x: (event.clientX - rect.left) * scaleX,
                y: (event.clientY - rect.top) * scaleY
            };
        };

        const onMouseMove = event => {
            const { x, y } = getMousePosition(event);
            let hit = null;
            for (let i = barRects.length - 1; i >= 0; i--) {
                const bar = barRects[i];
                if (x >= bar.x && x <= bar.x + bar.w && y >= bar.y && y <= bar.y + bar.h) {
                    hit = bar;
                    break;
                }
            }

            const nextTooltip = hit ? {
                ...hit,
                x,
                y
            } : null;

            const currentKey = tooltip ? `${tooltip.seriesIdx}-${tooltip.monthIdx}-${Math.round(tooltip.x)}-${Math.round(tooltip.y)}` : null;
            const nextKey = nextTooltip ? `${nextTooltip.seriesIdx}-${nextTooltip.monthIdx}-${Math.round(nextTooltip.x)}-${Math.round(nextTooltip.y)}` : null;
            if (currentKey !== nextKey) {
                tooltip = nextTooltip;
                draw();
            }
        };

        const onMouseLeave = () => {
            if (tooltip !== null) {
                tooltip = null;
                draw();
            }
        };

        const onResize = () => draw();
        window.addEventListener('resize', onResize);
        canvas.addEventListener('mousemove', onMouseMove);
        canvas.addEventListener('mouseleave', onMouseLeave);
        this._state.set(canvas, { onResize, onMouseMove, onMouseLeave });

        draw();

        const barSeriesStats = (config.barSeries || []).filter(s => Array.isArray(s.values));
        const lineSeriesStats = (config.lineSeries || []).filter(s => Array.isArray(s.values));
        const barPoints = barSeriesStats
            .flatMap(s => Array.isArray(s.values) ? s.values : [])
            .filter(v => typeof v === 'number' && isFinite(v)).length;
        const linePoints = lineSeriesStats
            .flatMap(s => Array.isArray(s.values) ? s.values : [])
            .filter(v => typeof v === 'number' && isFinite(v)).length;

        return `ok labels=${(config.labels || []).length} bars=${barSeriesStats.length} lines=${lineSeriesStats.length} points=${barPoints + linePoints}`;
    },

    _hexToRgba(hex, alpha) {
        if (!hex || typeof hex !== 'string') return `rgba(0,0,0,${alpha})`;
        const clean = hex.replace('#', '').trim();
        const full = clean.length === 3
            ? clean.split('').map(c => c + c).join('')
            : clean;

        const r = parseInt(full.substring(0, 2), 16);
        const g = parseInt(full.substring(2, 4), 16);
        const b = parseInt(full.substring(4, 6), 16);

        if ([r, g, b].some(Number.isNaN)) {
            return `rgba(0,0,0,${alpha})`;
        }

        return `rgba(${r}, ${g}, ${b}, ${alpha})`;
    }
};
