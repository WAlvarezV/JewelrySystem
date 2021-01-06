function saveAsFile(filename, bytesBase64) {
    var link = document.createElement('a');
    link.download = filename;
    link.href = "data:application/octet-stream;base64," + bytesBase64;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
}

function CustomConfirm(title, message, type, confirmText) {
    return new Promise((resolve) => {
        Swal.fire({
            title: title,
            text: message,
            icon: type,
            showCancelButton: true,
            confirmButtonColor: '#3085d6',
            cancelButtonColor: '#d33',
            confirmButtonText: confirmText,
            cancelButtonText: 'Cancelar'
        }).then((result) => {
            if (result.value) {
                resolve(true);
            }
            else {
                resolve(false);
            }
        });
    });
}

function initializeInactivityTimer(dotnetHelper) {
    var timer;
    document.onmousemove = resetTimer;
    document.onkeypress = resetTimer;

    function resetTimer() {
        clearTimeout(timer);
        timer = setTimeout(logout, 900000);
    }

    function logout() {
        dotnetHelper.invokeMethodAsync("Logout");
    }
}
