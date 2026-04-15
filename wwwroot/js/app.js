/**
 * Aplicación principal para Control de Usuarios
 * Maneja la carga de archivos, progreso en tiempo real y navegación
 */
class UsuarioApp {
    constructor() {
        this.procesoId = null;
        this.connection = null;
        this.initializeElements();
        this.initializeEvents();
        this.initializeSignalR();
    }

    initializeElements() {
        // Elementos de la interfaz
        this.fileInput = document.getElementById('fileInput');
        this.elegirArchivoBtn = document.getElementById('elegirArchivoBtn');
        this.fileNameDisplay = document.getElementById('fileNameDisplay');
        

        this.processBtn = document.getElementById('processBtn');
        this.progressContainer = document.getElementById('progressContainer');
        this.progressBar = document.getElementById('progressBar');
        this.progressPercent = document.getElementById('progressPercent');
        this.progressLabel = document.getElementById('progressLabel');
        this.progressMessage = document.getElementById('progressMessage');
        this.resultsContainer = document.getElementById('resultsContainer');
        this.resultsSummary = document.getElementById('resultsSummary');
        this.viewResultsBtn = document.getElementById('viewResultsBtn');
        this.downloadExcelBtn = document.getElementById('downloadExcelBtn');
        this.errorContainer = document.getElementById('errorContainer');
        this.errorMessage = document.getElementById('errorMessage');
        this.fileValidationMessage = document.getElementById('fileValidationMessage');
    }

    initializeEvents() {
        // Eventos de carga de archivos
        if (this.fileInput) {
            this.fileInput.addEventListener('change', (e) => this.handleFileSelect(e));
        }

        // Botón Elegir archivo
        if (this.elegirArchivoBtn) {
            this.elegirArchivoBtn.addEventListener('click', () => {
                this.fileInput.click();
            });
        }

        // Botón Iniciar
        if (this.processBtn) {
            this.processBtn.addEventListener('click', () => this.processFile());
        }
    }

    async initializeSignalR() {
        try {
            this.connection = new signalR.HubConnectionBuilder()
                .withUrl("/progressHub")
                .withAutomaticReconnect()
                .build();

            this.connection.on("ActualizarProgreso", (progreso) => {
                this.updateProgress(progreso);
            });

            await this.connection.start();
            console.log("SignalR conectado");
        } catch (err) {
            console.error("Error conectando SignalR:", err);
        }
    }

    handleFileSelect(event) {
        const file = event.target.files[0];
        if (file) {
            this.handleFile(file);
        } else {
            // Si no hay archivo seleccionado, limpiar estado
            this.clearFileState();
        }
    }

    handleFile(file) {
        // Limpiar errores previos
        this.hideContainers(['errorContainer']);
        this.hideFileValidationMessage();
        
        // Validar archivo
        if (!this.validateFile(file)) {
            this.clearFileState();
            return;
        }

        // Habilitar botón
        this.processBtn.disabled = false;
        this.processBtn.textContent = 'Iniciar';
        this.hideContainers(['progressContainer', 'resultsContainer']);
        
        // Actualizar el texto del archivo
        if (this.fileNameDisplay) {
            this.fileNameDisplay.textContent = `Archivo seleccionado: ${file.name} (${this.formatFileSize(file.size)})`;
        }
    }

    clearFileState() {
        this.processBtn.disabled = true;
        this.processBtn.textContent = 'Iniciar';
        if (this.fileNameDisplay) {
            this.fileNameDisplay.textContent = 'No se eligió ningún archivo';
        }
        this.hideContainers(['progressContainer', 'resultsContainer', 'errorContainer']);
        this.hideFileValidationMessage();
    }

    // Método eliminado ya que ahora manejamos el texto directamente en handleFile y clearFileState

    validateFile(file) {
        const validTypes = ['application/vnd.ms-excel', 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet'];
        const maxSize = 50 * 1024 * 1024; // 50MB

        if (!validTypes.includes(file.type) && !file.name.match(/\.(xls|xlsx)$/i)) {
            this.showError('Tipo de archivo inválido. Solo se permiten archivos Excel (.xls, .xlsx)');
            return false;
        }

        if (file.size > maxSize) {
            this.showFileValidationMessage('El archivo es demasiado grande. Tamaño máximo: 50MB');
            return false;
        }

        return true;
    }

    async processFile() {
        const file = this.fileInput.files[0];
        if (!file) {
            this.showError('Debe seleccionar un archivo');
            return;
        }

        try {
            // Deshabilitar botón y mostrar progreso
            this.processBtn.disabled = true;
            this.processBtn.textContent = 'Procesando...';
            this.progressContainer.style.display = 'block';
            this.hideContainers(['resultsContainer', 'errorContainer']);

            // Preparar datos para envío
            const formData = new FormData();
            formData.append('file', file);

            // Enviar archivo
            const response = await fetch('/api/Api/process-excel', {
                method: 'POST',
                body: formData
            });

            if (!response.ok) {
                throw new Error(`HTTP error! status: ${response.status}`);
            }

            const result = await response.json();

            if (result.success) {
                this.procesoId = result.procesoId;
                
                // Unirse al grupo de SignalR para recibir actualizaciones
                if (this.connection) {
                    await this.connection.invoke("JoinGroup", this.procesoId);
                }

                this.updateProgress({
                    procesoId: this.procesoId,
                    porcentaje: 5,
                    etapa: "Iniciado",
                    mensaje: "Proceso iniciado correctamente"
                });

                // Polling para obtener estado final
                this.startProgressPolling();
            } else {
                this.showError(result.message || 'Error procesando el archivo');
                this.resetForm();
            }
        } catch (error) {
            console.error('Error:', error);
            this.showError('Error de conexión. Por favor, inténtelo nuevamente.');
            this.resetForm();
        }
    }

    updateProgress(progreso) {
        if (!progreso) return;

        const { porcentaje, etapa, mensaje } = progreso;

        // Actualizar barra de progreso
        this.progressBar.style.width = `${porcentaje}%`;
        this.progressPercent.textContent = `${porcentaje}%`;
        this.progressLabel.textContent = etapa;
        
        // Mejorar el formato del mensaje
        if (mensaje) {
            this.progressMessage.innerHTML = mensaje.replace(/\n/g, '<br>');
        }

        // Si hay error, mostrar en rojo
        if (etapa === 'Error') {
            this.progressMessage.style.color = '#dc3545';
            this.progressMessage.style.fontWeight = 'bold';
        } else {
            this.progressMessage.style.color = '#6c757d';
            this.progressMessage.style.fontWeight = 'normal';
        }

        // Si está completado, mostrar resultados
        if (porcentaje >= 100) {
            setTimeout(() => this.loadResults(), 1000);
        }
    }

    startProgressPolling() {
        if (!this.procesoId) return;

        const pollProgress = async () => {
            try {
                const response = await fetch(`/api/Api/progress/${this.procesoId}`);
                const result = await response.json();

                if (result.success && result.progreso) {
                    this.updateProgress(result.progreso);
                    
                    if (result.progreso.porcentaje < 100) {
                        setTimeout(pollProgress, 2000);
                    }
                } else {
                    setTimeout(pollProgress, 2000);
                }
            } catch (error) {
                console.error('Error en polling:', error);
                setTimeout(pollProgress, 5000);
            }
        };

        pollProgress();
    }

    async loadResults() {
        if (!this.procesoId) return;

        try {
            const response = await fetch(`/api/Api/summary/${this.procesoId}`);
            const result = await response.json();

            if (result.success && result.resumen) {
                this.showResults(result.resumen);
            } else {
                this.showError('Error cargando los resultados');
            }
        } catch (error) {
            console.error('Error cargando resultados:', error);
            this.showError('Error cargando los resultados');
        }
    }

    showResults(resumen) {
        this.hideContainers(['progressContainer', 'errorContainer']);
        
        // Generar resumen HTML
        const porcentajeHabilitados = resumen.total > 0 ? (resumen.habilitados / resumen.total * 100).toFixed(1) : 0;
        const porcentajeInactivos = resumen.total > 0 ? (resumen.inactivos / resumen.total * 100).toFixed(1) : 0;

        this.resultsSummary.innerHTML = `
            <div class="row text-center">
                <div class="col-md-4">
                    <div class="border rounded p-3">
                        <h4 class="text-primary fw-bold">${resumen.total.toLocaleString()}</h4>
                        <div class="text-muted">Total de Usuarios</div>
                    </div>
                </div>
                <div class="col-md-4">
                    <div class="border rounded p-3">
                        <h4 class="text-success fw-bold">${resumen.habilitados.toLocaleString()}</h4>
                        <div class="text-muted">Habilitados (${porcentajeHabilitados}%)</div>
                    </div>
                </div>
                <div class="col-md-4">
                    <div class="border rounded p-3">
                        <h4 class="text-warning fw-bold">${resumen.inactivos.toLocaleString()}</h4>
                        <div class="text-muted">Inactivos (${porcentajeInactivos}%)</div>
                    </div>
                </div>
            </div>
        `;

        // Configurar botones
        this.viewResultsBtn.href = `/Home/Results?procesoId=${this.procesoId}`;
        this.downloadExcelBtn.href = `/api/Api/download-excel/${this.procesoId}`;

        this.resultsContainer.style.display = 'block';
        this.resetForm();
    }

    showError(message) {
        this.hideContainers(['progressContainer', 'resultsContainer']);
        this.errorMessage.textContent = `Error: ${message}`;
        this.errorContainer.style.display = 'block';
        this.resetForm();
    }

    hideContainers(containerIds) {
        containerIds.forEach(id => {
            const element = document.getElementById(id);
            if (element) {
                element.style.display = 'none';
            }
        });
    }

    showFileValidationMessage(message) {
        if (this.fileValidationMessage) {
            this.fileValidationMessage.querySelector('small').textContent = message;
            this.fileValidationMessage.style.display = 'block';
        }
    }

    hideFileValidationMessage() {
        if (this.fileValidationMessage) {
            this.fileValidationMessage.style.display = 'none';
        }
    }

    resetForm() {
        if (this.processBtn) {
            this.processBtn.disabled = false;
            this.processBtn.textContent = 'Iniciar';
        }
    }

    formatFileSize(bytes) {
        if (bytes === 0) return '0 Bytes';
        const k = 1024;
        const sizes = ['Bytes', 'KB', 'MB', 'GB'];
        const i = Math.floor(Math.log(bytes) / Math.log(k));
        return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i];
    }
}

// Inicializar aplicación cuando el DOM esté listo
document.addEventListener('DOMContentLoaded', () => {
    new UsuarioApp();
});
