window.messageFunctions = {
    WaitMessage: function (message) {
        Swal.fire({
            title: '¡Espere Por favor!',
            text: message,
            allowOutsideClick: false
        });
        Swal.showLoading();
    }
};

