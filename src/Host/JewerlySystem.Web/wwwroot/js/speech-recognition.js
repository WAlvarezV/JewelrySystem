// Web Speech API Interop for Blazor .NET 10 (JewerlySystem)
window.jewelrySpeech = {
    recognition: null,
    isListening: false,
    activeDotNetRef: null,

    isSupported: function () {
        return ('SpeechRecognition' in window) || ('webkitSpeechRecognition' in window);
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
            this.recognition.continuous = true;
            this.recognition.interimResults = true;
            this.activeDotNetRef = dotNetHelper;

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
                    if (result.isFinal) {
                        finalTranscript += result[0].transcript;
                    } else {
                        interimTranscript += result[0].transcript;
                    }
                }

                if (this.activeDotNetRef && (finalTranscript.length > 0 || interimTranscript.length > 0)) {
                    this.activeDotNetRef.invokeMethodAsync('OnSpeechResult', finalTranscript, interimTranscript);
                }
            };

            this.recognition.onerror = (event) => {
                console.warn('Speech recognition error:', event.error);
                let message = 'Error en reconocimiento de voz.';
                if (event.error === 'not-allowed' || event.error === 'service-not-allowed') {
                    message = 'Permiso de micrófono denegado en el navegador.';
                } else if (event.error === 'no-speech') {
                    return; // Silenciar si no se detectó voz temporalmente
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
