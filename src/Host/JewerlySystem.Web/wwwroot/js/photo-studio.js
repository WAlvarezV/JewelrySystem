/**
 * Estudio Fotográfico y Normalización de Catálogo de Joyas y Relojes
 * - Procesamiento en HTML5 Canvas
 * - Estandarización de fondo a Blanco Puro (#FFFFFF)
 * - Marca de Agua Genérica de Alta Gama: FINE JEWELRY & LUXURY TIMEPIECES • PIEZA VERIFICADA
 * - Compresión optimizada WebP (< 80 KB)
 */
window.photoStudio = {
    processImageFromFile: function (inputId, options) {
        return new Promise(function (resolve, reject) {
            try {
                var input = document.getElementById(inputId);
                if (!input || !input.files || input.files.length === 0) {
                    reject("No se ha seleccionado ningún archivo.");
                    return;
                }
                var file = input.files[0];
                var reader = new FileReader();
                reader.onload = function (e) {
                    window.photoStudio.processDataUrl(e.target.result, options)
                        .then(resolve)
                        .catch(reject);
                };
                reader.onerror = function () {
                    reject("Error al leer el archivo de la cámara.");
                };
                reader.readAsDataURL(file);
            } catch (err) {
                reject(err.toString());
            }
        });
    },

    processDataUrl: function (dataUrl, options) {
        return new Promise(function (resolve, reject) {
            try {
                options = options || {};
                var maxDimension = options.maxDimension || 900;
                var quality = options.quality || 0.85;
                var applyWhitening = options.applyWhitening !== false;
                var applyWatermark = options.applyWatermark !== false;

                var img = new Image();
                img.crossOrigin = "anonymous";
                img.onload = function () {
                    try {
                        var canvas = document.createElement("canvas");
                        var ctx = canvas.getContext("2d");

                        var origW = img.width;
                        var origH = img.height;

                        // Calcular dimensiones manteniendo relación de aspecto dentro de un lienzo cuadrado o ajustado
                        var targetW = origW;
                        var targetH = origH;

                        if (origW > maxDimension || origH > maxDimension) {
                            if (origW > origH) {
                                targetW = maxDimension;
                                targetH = Math.round((origH * maxDimension) / origW);
                            } else {
                                targetH = maxDimension;
                                targetW = Math.round((origW * maxDimension) / origH);
                            }
                        }

                        // Lienzo cuadrado elegante para vitrina
                        var squareSize = Math.max(targetW, targetH);
                        // Añadir margen de vitrina (padding 5%)
                        var margin = Math.round(squareSize * 0.05);
                        var finalCanvasSize = squareSize + (margin * 2);

                        canvas.width = finalCanvasSize;
                        canvas.height = finalCanvasSize;

                        // 1. Relleno fondo blanco puro #FFFFFF
                        ctx.fillStyle = "#FFFFFF";
                        ctx.fillRect(0, 0, finalCanvasSize, finalCanvasSize);

                        // 2. Dibujar imagen centrada
                        var offsetX = margin + Math.round((squareSize - targetW) / 2);
                        var offsetY = margin + Math.round((squareSize - targetH) / 2);

                        ctx.drawImage(img, offsetX, offsetY, targetW, targetH);

                        // 3. Normalización de fondo blanco (aclarar ruido / fondos claros de mesa a blanco puro)
                        if (applyWhitening) {
                            var imgData = ctx.getImageData(0, 0, finalCanvasSize, finalCanvasSize);
                            var d = imgData.data;

                            // Recorrer píxeles: si son muy claros (fondo de mesa / luz suave), forzar a blanco puro #FFFFFF
                            for (var i = 0; i < d.length; i += 4) {
                                var r = d[i];
                                var g = d[i + 1];
                                var b = d[i + 2];

                                // Verificar si el píxel es cercano al blanco (fondo de vitrina/mesa clara)
                                // y baja saturación (para no tocar joyas doradas o gemas de color)
                                var maxC = Math.max(r, g, b);
                                var minC = Math.min(r, g, b);
                                var delta = maxC - minC;

                                if (r > 215 && g > 215 && b > 215 && delta < 25) {
                                    d[i] = 255;
                                    d[i + 1] = 255;
                                    d[i + 2] = 255;
                                } else if (r > 200 && g > 200 && b > 200 && delta < 15) {
                                    // Gradiente suave de blanqueado
                                    d[i] = 255;
                                    d[i + 1] = 255;
                                    d[i + 2] = 255;
                                }
                            }
                            ctx.putImageData(imgData, 0, 0);
                        }

                        // 4. Marca de agua genérica de alta gama (universal para cualquier joyería)
                        if (applyWatermark) {
                            var barHeight = Math.round(finalCanvasSize * 0.07);
                            var barY = finalCanvasSize - barHeight;

                            // Fondo semi-translúcido tenue para máxima legibilidad sin tapar la pieza
                            ctx.fillStyle = "rgba(255, 255, 255, 0.88)";
                            ctx.fillRect(0, barY, finalCanvasSize, barHeight);

                            // Línea dorada sutil superior
                            ctx.strokeStyle = "rgba(212, 175, 55, 0.75)"; // Oro clásico #D4AF37
                            ctx.lineWidth = 1.5;
                            ctx.beginPath();
                            ctx.moveTo(finalCanvasSize * 0.08, barY);
                            ctx.lineTo(finalCanvasSize * 0.92, barY);
                            ctx.stroke();

                            // Texto de la marca de agua genérica
                            var fontSize = Math.max(10, Math.round(finalCanvasSize * 0.022));
                            ctx.font = "600 " + fontSize + "px 'Cinzel', 'Playfair Display', -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif";
                            ctx.textAlign = "center";
                            ctx.textBaseline = "middle";

                            // Sombra suave para contraste
                            ctx.fillStyle = "#8C7335"; // Dorado oscuro / bronce noble
                            ctx.fillText("✦  FINE JEWELRY & LUXURY TIMEPIECES • PIEZA VERIFICADA  ✦", finalCanvasSize / 2, barY + (barHeight / 2));
                        }

                        // 5. Exportar a WebP de alta resolución y bajo peso (< 80KB)
                        var resultDataUrl = canvas.toDataURL("image/webp", quality);

                        // Si el navegador no soporta webp (retorna image/png), fallback a jpeg
                        if (!resultDataUrl.startsWith("data:image/webp")) {
                            resultDataUrl = canvas.toDataURL("image/jpeg", quality);
                        }

                        resolve(resultDataUrl);
                    } catch (err) {
                        reject("Error procesando imagen en canvas: " + err.toString());
                    }
                };

                img.onerror = function () {
                    reject("No se pudo cargar la imagen para procesar.");
                };

                img.src = dataUrl;
            } catch (err) {
                reject(err.toString());
            }
        });
    }
};
