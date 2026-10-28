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
            const options = JSON.parse(optionsJson);
            options.challenge = this.coerceToArrayBuffer(options.challenge);
            options.user.id = this.coerceToArrayBuffer(options.user.id);

            if (options.excludeCredentials) {
                for (let i = 0; i < options.excludeCredentials.length; i++) {
                    options.excludeCredentials[i].id = this.coerceToArrayBuffer(options.excludeCredentials[i].id);
                }
            }

            const cred = await navigator.credentials.create({ publicKey: options });

            const utf8Decoder = new TextDecoder("utf-8");
            return JSON.stringify({
                id: cred.id,
                rawId: this.coerceToBase64Url(cred.rawId),
                type: cred.type,
                response: {
                    attestationObject: this.coerceToBase64Url(cred.response.attestationObject),
                    clientDataJson: this.coerceToBase64Url(cred.response.clientDataJSON)
                }
            });
        } catch (err) {
            console.error("Passkey registration failed:", err);
            throw err.message || err;
        }
    },

    getAssertion: async function (optionsJson) {
        try {
            const options = JSON.parse(optionsJson);
            options.challenge = this.coerceToArrayBuffer(options.challenge);

            if (options.allowCredentials) {
                for (let i = 0; i < options.allowCredentials.length; i++) {
                    options.allowCredentials[i].id = this.coerceToArrayBuffer(options.allowCredentials[i].id);
                }
            }

            const assertion = await navigator.credentials.get({ publicKey: options });

            return JSON.stringify({
                id: assertion.id,
                rawId: this.coerceToBase64Url(assertion.rawId),
                type: assertion.type,
                response: {
                    authenticatorData: this.coerceToBase64Url(assertion.response.authenticatorData),
                    clientDataJson: this.coerceToBase64Url(assertion.response.clientDataJSON),
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
