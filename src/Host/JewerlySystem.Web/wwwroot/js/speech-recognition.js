// Web Speech API Interop for Blazor .NET 10 (JewerlySystem)
window.jewelrySpeech = {
    recognition: null,
    isListening: false,
    activeDotNetRef: null,
    lastInterim: '',
    lastFinalizedSegment: '',

    isSupported: function () {
        return ('SpeechRecognition' in window) || ('webkitSpeechRecognition' in window);
    },

    isMobileDevice: function () {
        return /Android|webOS|iPhone|iPad|iPod|BlackBerry|IEMobile|Opera Mini/i.test(navigator.userAgent || '');
    },

    start: function (dotNetHelper, lang) {
        if (!this.isSupported()) {
            dotNetHelper.invokeMethodAsync('OnSpeechError', 'Tu navegador no soporta reconocimiento de voz nativo (Web Speech API). Por favor utiliza Chrome, Edge o Safari.');
            return false;
        }

        const SpeechRec = window.SpeechRecognition || window.webkitSpeechRecognition;

        if (this.recognition) {
            try {
                this.recognition.abort();
            } catch (e) { }
            this.recognition = null;
        }

        try {
            this.recognition = new SpeechRec();
            this.recognition.lang = lang || 'es-CO';
            // En Android móvil, continuous=true suele duplicar o dejar pegada la segunda mitad del dictado;
            // gestionamos reinicio suave o volcado de interim para una experiencia móvil fluida.
            this.recognition.continuous = !this.isMobileDevice();
            this.recognition.interimResults = true;
            this.activeDotNetRef = dotNetHelper;
            this.lastInterim = '';
            this.lastFinalizedSegment = '';

            this.recognition.onstart = () => {
                this.isListening = true;
                if (this.activeDotNetRef) {
                    this.activeDotNetRef.invokeMethodAsync('OnSpeechStateChanged', true);
                }
            };

            this.recognition.onresult = (event) => {
                let finalTranscript = '';
                let interimTranscript = '';

                for (let i = event.resultIndex; i < event.results.length; ++i) {
                    const result = event.results[i];
                    const piece = (result[0] && result[0].transcript) ? result[0].transcript.trim() : '';
                    if (!piece) continue;

                    if (result.isFinal) {
                        // Evita duplicación acumulativa típica de Chrome en Android
                        if (piece !== this.lastFinalizedSegment) {
                            finalTranscript += (finalTranscript ? ' ' : '') + piece;
                            this.lastFinalizedSegment = piece;
                        }
                    } else {
                        interimTranscript += (interimTranscript ? ' ' : '') + piece;
                    }
                }

                this.lastInterim = finalTranscript ? '' : interimTranscript;

                if (this.activeDotNetRef && (finalTranscript.length > 0 || interimTranscript.length > 0)) {
                    this.activeDotNetRef.invokeMethodAsync('OnSpeechResult', finalTranscript, interimTranscript);
                }
            };

            this.recognition.onerror = (event) => {
                console.warn('Speech recognition error:', event.error);
                let message = 'Error en reconocimiento de voz.';
                if (event.error === 'not-allowed' || event.error === 'service-not-allowed') {
                    message = 'Permiso de micrófono denegado en el navegador.';
                } else if (event.error === 'no-speech' || event.error === 'aborted') {
                    return; // Silenciar si no se detectó voz temporalmente o fue detenido por el usuario
                } else if (event.error === 'audio-capture') {
                    message = 'No se detectó ningún micrófono conectado.';
                }
                if (this.activeDotNetRef) {
                    this.activeDotNetRef.invokeMethodAsync('OnSpeechError', message);
                    this.activeDotNetRef.invokeMethodAsync('OnSpeechStateChanged', false);
                }
                this.isListening = false;
            };

            this.recognition.onend = () => {
                // Si quedó texto intermedio sin confirmar al cerrarse el micrófono en móvil, volcarlo como final
                if (this.lastInterim && this.lastInterim.trim().length > 0 && this.activeDotNetRef) {
                    const pending = this.lastInterim.trim();
                    this.lastInterim = '';
                    try {
                        this.activeDotNetRef.invokeMethodAsync('OnSpeechResult', pending, '');
                    } catch (e) { }
                }
                this.isListening = false;
                if (this.activeDotNetRef) {
                    this.activeDotNetRef.invokeMethodAsync('OnSpeechStateChanged', false);
                }
            };

            this.recognition.start();
            return true;
        } catch (err) {
            console.error('Error starting speech recognition:', err);
            if (this.activeDotNetRef) {
                this.activeDotNetRef.invokeMethodAsync('OnSpeechError', 'No se pudo iniciar el micrófono: ' + err.message);
                this.activeDotNetRef.invokeMethodAsync('OnSpeechStateChanged', false);
            }
            this.isListening = false;
            return false;
        }
    },

    stop: function () {
        if (this.recognition) {
            try {
                this.recognition.stop();
            } catch (e) { }
            this.isListening = false;
        }
    }
};
