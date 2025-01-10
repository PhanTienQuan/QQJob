function showLoginModal(message, email, password) {
    $("divx2").text('');
    $.ajax({
        url: '/Account/Login', // Replace 'YourController' with the actual controller name
        method: 'GET',
        contentType: 'application/json',
        success: function (response) {
            $("#modalPlaceholder").html(response);
            $("#loginModal").modal("show");
            $("divx2").html(message);
            $("#Email").val(email);
            $("#Password").val(password);
        },
        error: function () {
            alert("An error occurred while processing your request.");
        }
    });
}
function showRegisterModal() {
    $.ajax({
        url: '/Account/Register', // Replace 'YourController' with the actual controller name
        method: 'GET',
        contentType: 'application/json',
        success: function (response) {
            $("#modalPlaceholder").html(response);
            $("#signupModal").modal("show");
        },
        error: function () {
            alert("An error occurred while processing your request.");
        }
    });
}
function login(element) {
    let formData = $('#loginForm').serialize();
    $('span[data-valmsg-for]').text('');
    $("divx").text("");
    $("divx2").text("");
    disableElement("#Email", "#Password");
    const loading = $("#loading");
    loading.removeAttr("hidden");
    $.ajax({
        url: '/Account/Login',
        type: 'POST',
        data: formData,
        success: function (response) {
            if (response.success) {
                window.location.href = response.url
            } else {
                $.each(response.errors, function (key, messages) {
                    if (key == "All") {
                        $("divx").text(messages.join(', '))
                    }
                    let errorPlaceholder = $('span[data-valmsg-for="' + key + '"]');
                    errorPlaceholder.text(messages.join(', '));
                });
                loading.attr("hidden", true);
                enableElement("#Email", "#Password");
            }
        },
        error: function () {
            loading.attr("hidden", true);
            enableElement("#Email", "#Password");
            alert('An error occurred while processing your request.');
        }
    });
}
function setAccountType(e, bool) {
    $("button[class*='active']").removeClass("active");
    $(e).toggleClass("active");
    $('#AccountType').val(bool);
}
function register(element) {
    const formData = $('#registerForm').serialize(); // Serialize form data
    const onRegisterLoading = $("#loading");
    onRegisterLoading.removeAttr("hidden");
    disableElement("#Fullname", "#Email", "#Password", "#ConfirmPassword");
    // Clear previous validation messages
    $('span[data-valmsg-for]').text('');
    $.ajax({
        url: '/Account/Register',
        type: 'POST',
        data: formData,
        success: function (response) {
            if (response.success) {
                $("#signupModal").modal("hide");
                showLoginModal(response.message, response.email, response.password);
            } else {
                $.each(response.errors, function (key, messages) {
                    if (key == "ALL") {
                        $("divx").text(messages)
                    } else {
                        let errorPlaceholder = $('span[data-valmsg-for="' + key + '"]');
                        errorPlaceholder.text(messages.join(', '));
                    }
                });

                onRegisterLoading.attr("hidden", true);
                enableElement("#Fullname", "#Email", "#Password", "#ConfirmPassword");
            }
        },
        error: function () {
            onRegisterLoading.attr("hidden", true);
            enableElement("#Fullname", "#Email", "#Password", "#ConfirmPassword");
            alert('An error occurred while processing your request.');
        }
    });
}
function disableElement(...elements) {
    elements.forEach(e => {
        const inputElement = $(e);
        if (inputElement) {
            inputElement.attr("disabled", true);
        } else {
            console.warn(`Element not found for selector: ${e}`);
        }
    });
}
function enableElement(...elements) {
    elements.forEach(e => {
        const inputElement = $(e);
        if (inputElement) {
            inputElement.removeAttr("disabled");
        } else {
            console.warn(`Element not found for selector: ${e}`);
        }
    });
}
function handleExternalLoginClick(button) {
    var provider = button.getAttribute('data-provider');
    var url = button.getAttribute('data-url');
    // Define popup window size
    var width = 500;
    var height = 600;
    var left = (window.innerWidth / 2) - (width / 2);
    var top = (window.innerHeight / 2) - (height / 2);
    // Open the popup window
    var popup = window.open(url, provider, `width=${width},height=${height},top=${top},left=${left},resizable=yes`);
    // Monitor the popup to check if it closes
}
function showSetAccountTypeModel() {
    $.ajax({
        url: '/Account/SetAccountType',
        method: 'GET',
        contentType: 'application/json',
        success: function (response) {
            $("#loginModal").modal("hide");
            $("#modalPlaceholder").html(response);
            $("#setAccountModal").modal("show");
        },
        error: function () {
            alert("An error occurred while processing your request.");
        }
    });
}
