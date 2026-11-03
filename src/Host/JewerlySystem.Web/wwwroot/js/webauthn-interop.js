// WebAuthn / Passkeys JavaScript Interop for Blazor
window.webauthn = {
    coerceToArrayBuffer: function (thing) {
        if (typeof thing === "string") {
            thing = thing.replace(/-/g, "+").replace(/_/g, "/");
            while (thing.length % 4) {
                thing += "=";
            }
            const raw = window.atob(thing);
            const rawLength = raw.length;
            const array = new Uint8Array(new ArrayBuffer(rawLength));
            for (let i = 0; i < rawLength; i++) {
                array[i] = raw.charCodeAt(i);
            }
            return array.buffer;
        }
        return thing;
    },

    coerceToBase64Url: function (thing) {
        if (thing instanceof ArrayBuffer) {
            thing = new Uint8Array(thing);
        }
        if (thing instanceof Uint8Array) {
            let str = "";
            const len = thing.byteLength;
            for (let i = 0; i < len; i++) {
                str += String.fromCharCode(thing[i]);
            }
            return window.btoa(str).replace(/\+/g, "-").replace(/\//g, "_").replace(/=*$/g, "");
        }
        return thing;
    },

    createPasskey: async function (optionsJson) {
        try {
            const currentHost = window.location.hostname;
            const isIpAddress = /^(\d{1,3}\.){3}\d{1,3}$/.test(currentHost) || currentHost === "::1" || currentHost === "[::1]";

            if (isIpAddress) {
                const port = window.location.port ? ":" + window.location.port : "";
                if (currentHost === "127.0.0.1") {
                    throw new Error("WebAuthn / Passkeys no admite direcciones IP como '127.0.0.1'. Por favor abre el sistema desde " + window.location.protocol + "//localhost" + port + " para registrar Passkeys.");
                } else {
                    throw new Error("WebAuthn / Passkeys requiere un nombre de dominio (ej. localhost o tu dominio en Railway), no una dirección IP (" + currentHost + ").");
                }
            }

            const options = JSON.parse(optionsJson);
            options.challenge = this.coerceToArrayBuffer(options.challenge);
            options.user.id = this.coerceToArrayBuffer(options.user.id);

            if (options.excludeCredentials) {
                for (let i = 0; i < options.excludeCredentials.length; i++) {
                    options.excludeCredentials[i].id = this.coerceToArrayBuffer(options.excludeCredentials[i].id);
                }
            }

            // Always align rp.id with the current domain in browser (e.g. jewelrysystem-production.up.railway.app or localhost)
            if (options.rp) {
                options.rp.id = currentHost;
            }

            const cred = await navigator.credentials.create({ publicKey: options });

            let transports = [];
            if (cred.response.getTransports && typeof cred.response.getTransports === "function") {
                transports = cred.response.getTransports();
            }

            let clientExtensionResults = {};
            if (cred.getClientExtensionResults && typeof cred.getClientExtensionResults === "function") {
                clientExtensionResults = cred.getClientExtensionResults();
            }

            const clientDataB64 = this.coerceToBase64Url(cred.response.clientDataJSON);

            return JSON.stringify({
                id: cred.id,
                rawId: this.coerceToBase64Url(cred.rawId),
                type: cred.type || "public-key",
                clientExtensionResults: clientExtensionResults,
                response: {
                    attestationObject: this.coerceToBase64Url(cred.response.attestationObject),
                    clientDataJSON: clientDataB64,
                    clientDataJson: clientDataB64,
                    transports: transports
                }
            });
        } catch (err) {
            console.error("Passkey registration failed:", err);
            throw err.message || err;
        }
    },

    getAssertion: async function (optionsJson) {
        try {
            const currentHost = window.location.hostname;
            const isIpAddress = /^(\d{1,3}\.){3}\d{1,3}$/.test(currentHost) || currentHost === "::1" || currentHost === "[::1]";

            if (isIpAddress) {
                const port = window.location.port ? ":" + window.location.port : "";
                if (currentHost === "127.0.0.1") {
                    throw new Error("WebAuthn / Passkeys no admite direcciones IP como '127.0.0.1'. Por favor abre el sistema desde " + window.location.protocol + "//localhost" + port + " para iniciar sesión con Passkeys.");
                } else {
                    throw new Error("WebAuthn / Passkeys requiere un nombre de dominio (ej. localhost o tu dominio en Railway), no una dirección IP (" + currentHost + ").");
                }
            }

            const options = JSON.parse(optionsJson);
            options.challenge = this.coerceToArrayBuffer(options.challenge);

            if (options.allowCredentials) {
                for (let i = 0; i < options.allowCredentials.length; i++) {
                    options.allowCredentials[i].id = this.coerceToArrayBuffer(options.allowCredentials[i].id);
                }
            }

            // Always align rpId with the current domain in browser (e.g. jewelrysystem-production.up.railway.app or localhost)
            options.rpId = currentHost;

            const assertion = await navigator.credentials.get({ publicKey: options });

            let clientExtensionResults = {};
            if (assertion.getClientExtensionResults && typeof assertion.getClientExtensionResults === "function") {
                clientExtensionResults = assertion.getClientExtensionResults();
            }

            const clientDataB64 = this.coerceToBase64Url(assertion.response.clientDataJSON);

            return JSON.stringify({
                id: assertion.id,
                rawId: this.coerceToBase64Url(assertion.rawId),
                type: assertion.type || "public-key",
                clientExtensionResults: clientExtensionResults,
                response: {
                    authenticatorData: this.coerceToBase64Url(assertion.response.authenticatorData),
                    clientDataJSON: clientDataB64,
                    clientDataJson: clientDataB64,
                    signature: this.coerceToBase64Url(assertion.response.signature),
                    userHandle: assertion.response.userHandle ? this.coerceToBase64Url(assertion.response.userHandle) : null
                }
            });
        } catch (err) {
            console.error("Passkey assertion failed:", err);
            throw err.message || err;
        }
    }
};
