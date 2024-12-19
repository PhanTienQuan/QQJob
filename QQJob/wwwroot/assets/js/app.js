function showLoginModal(message) {
    $("divx2").text('');
    $.ajax({
        url: '/Account/Login', // Replace 'YourController' with the actual controller name
        method: 'GET',
        contentType: 'application/json',
        success: function (response) {
            $("#modalPlaceholder").html(response);
            $("#loginModal").modal("show");
            $("divx2").text(message);
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
function login() {
    let formData = $('#loginForm').serialize();
    $('span[data-valmsg-for]').text('');
    $("divx").text("")
    $.ajax({
        url: '/Account/Login', // Update the URL as needed
        type: 'POST',
        data: formData,
        success: function (response) {
            // Clear previous validation messages
            if (response.success) {
                window.location.href = response.url
            } else {
                // Display validation errors
                $.each(response.errors, function (key, messages) {
                    if (key == "All") {
                        $("divx").text(messages.join(', '))
                    }
                    let errorPlaceholder = $('span[data-valmsg-for="' + key + '"]');
                    errorPlaceholder.text(messages.join(', '));
                });
            }
        },
        error: function () {
            alert('An error occurred while processing your request.');
        }
    });
}
function register() {
    let formData = $('#registerForm').serialize(); // Serialize form data
    let a = $('input[name="AccountType"]').val()
    // Clear previous validation messages
    $('span[data-valmsg-for]').text('');
    $.ajax({
        url: '/Account/Register', // Update the URL as needed
        type: 'POST',
        data: formData,
        success: function (response) {
            if (response.success) {
                $("#signupModal").modal("hide");
                showLoginModal(response.message);
            } else {
                // Display validation errors
                $.each(response.errors, function (key, messages) {
                    if (key == "ALL") {
                        $("divx").text(messages.join(', '))
                    }
                    let errorPlaceholder = $('span[data-valmsg-for="' + key + '"]');
                    errorPlaceholder.text(messages.join(', '));
                });
            }

            $("button[data-account-type=" + a + "]").addClass('active');
            $('input[name="AccountType"]').val(a);
        },
        error: function () {
            alert('An error occurred while processing your request.');
        }
    });
}
