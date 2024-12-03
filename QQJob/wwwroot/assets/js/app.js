function showLoginModal() {
    $.ajax({
        url: '/Account/Login', // Replace 'YourController' with the actual controller name
        method: 'GET',
        contentType: 'application/json',
        success: function (response) {
            $("#modalPlaceholder").html(response);

            $("#loginModal").modal("show");
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
function register() {
    let formData = $('#registerForm').serialize(); // Serialize form data
    let a = $('input[name="AccountType"]').val()

    $.ajax({
        url: '/Account/Register', // Update the URL as needed
        type: 'POST',
        data: formData,
        success: function (response) {
            // Clear previous validation messages
            $('span[data-valmsg-for]').text('');
            if (response.success) {
                $("#signupModal").modal("hide");
                showLoginModal();
            } else {
                // Display validation errors
                $.each(response.errors, function (key, messages) {
                    if (key == "ALL") {
                        $("divx").val(messages.join(', '))
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