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
    //remove message text
    $('span[data-valmsg-for]').text('');
    $("divx").text("");
    $("divx2").text("");
    //show loading icon
    const loading = $("#loading");
    loading.removeAttr("hidden");
    //disable interaction with the webpage
    document.getElementById("overlay").style.display = "block";
    $(element).attr("disabled", true);
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
                        $("divx").text(messages)
                    } else {
                        let errorPlaceholder = $('span[data-valmsg-for="' + key + '"]');
                        errorPlaceholder.text(messages);
                    }
                });
                loading.attr("hidden", true);
                document.getElementById("overlay").style.display = "none";
                $(element).attr("disabled", false);
            }
        },
        error: function () {
            loading.attr("hidden", true);
            document.getElementById("overlay").style.display = "none";
            $(element).attr("disabled", false);
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
    document.getElementById("overlay").style.display = "block";
    $(element).attr("disabled", true);
    // Clear previous validation messages
    $('span[data-valmsg-for]').text('');
    $.ajax({
        url: '/Account/Register',
        type: 'POST',
        data: formData,
        success: function (response) {
            if (response.success) {
                $("#signupModal").modal("hide");
                document.getElementById("overlay").style.display = "none";
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
                document.getElementById("overlay").style.display = "none";
                $(element).removeAttr("disabled");
            }
        },
        error: function () {
            onRegisterLoading.attr("hidden", true);
            document.getElementById("overlay").style.display = "none";
            $(element).removeAttr("disabled"); 
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
    //var checkPopup = setInterval(function () {
    //    if (popup.closed) {
    //        clearInterval(checkPopup);
    //        // Optionally, reload or fetch updated user state
    //        location.reload(); // Refresh the parent window to reflect login state
    //    }
    //}, 500);
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
function showToastMessage(message, type) {
    const container = document.querySelector('.toast-container');
    const toastElement = document.createElement('div');

    // Set class and styles based on the type
    let backgroundColor, textColor, closeButtonColor;
    switch (type) {
        case "success":
            backgroundColor = "bg-success";
            textColor = "text-white";
            closeButtonColor = "btn-close-white";
            break;
        case "error":
            backgroundColor = "bg-danger";
            textColor = "text-white";
            closeButtonColor = "btn-close-white";
            break;
        case "warning":
            backgroundColor = "bg-warning";
            textColor = "text-black";
            closeButtonColor = "btn-close-black";
            break;
        default:
            backgroundColor = "bg-secondary";
            textColor = "text-white";
            closeButtonColor = "btn-close-white";
            break;
    }

    toastElement.className = `toast align-items-center ${textColor} ${backgroundColor} border-0`;
    //toastElement.style = `background-color: ${backgroundColor}`;
    toastElement.setAttribute('role', 'status');
    toastElement.setAttribute('aria-live', 'polite');
    toastElement.setAttribute('aria-atomic', 'true');
    toastElement.setAttribute('data-bs-delay', 4000);

    toastElement.innerHTML = `
        <div class="d-flex">
            <div class="toast-body">${message}</div>
            <button type="button" class="btn-close ${closeButtonColor} me-2 m-auto" data-bs-dismiss="toast" aria-label="Close"></button>
        </div>
    `;

    // Remove the toast from DOM when it is hidden
    toastElement.addEventListener('hidden.bs.toast', function () {
        container.removeChild(toastElement);
    });

    // Append the toast to the container
    container.appendChild(toastElement);

    // Initialize and show the new toast
    const toast = new bootstrap.Toast(toastElement);
    toast.show();
}

window.connection = new signalR.HubConnectionBuilder()
    .withUrl("/chathub")
    .build();

connection.start().then(() => {
    fetch('/Message/GetChatSessionIds') // <-- Use your controller/route here
        .then(response => response.json())
        .then(chatIds => {
            chatIds.forEach(chatId => {
                connection.invoke("JoinChat", chatId);
            });
        })
        .catch(err => {
            console.error('Failed to load chat sessions:', err);
        });
});

connection.on("ReceiveMessage", function (response) {
    const message = response.message;

    const messagePreview = document.querySelectorAll(`#MessagePreview_${message.chatId}`);
    const countPreview = document.querySelectorAll(`#Count_${message.chatId}`);
    const timePreview = document.querySelectorAll(`#Time_${message.chatId}`);
    const isCurrentUser = message.senderId === currentUserId;
    const unreadCount = isCurrentUser ? 0 : response.unreadMessagesCount;
    const onChatPage = window.location.pathname.includes("/Message"); // adjust for your route
    try {
        if (onChatPage) {
            // Update chat preview
            if (countPreview.length > 0) {
                countPreview.forEach(function (element) {
                    element.innerHTML = unreadCount;
                });
            }
            if (messagePreview) {
                messagePreview.forEach(function (element) {
                    element.innerHTML = isCurrentUser ? `You: ${message.messageText}` : `${message.fullName}: ${message.messageText}`;
                    element.style.fontWeight = unreadCount === 0 ? "normal" : "bold";
                });
            }
            if (timePreview) {
                timePreview.forEach(function (element) {
                    element.innerHTML = formatTimeAgo(message.sentAt);
                });
            }

            if (chatSessionList) {
                chatSessionList.forEach(function (element) {
                    const sessionElement = element.querySelector(`.single__chat__person[data-sessionid="${message.chatId}"]`);
                    element.removeChild(sessionElement);
                    element.insertBefore(sessionElement, element.firstChild);
                });
            }

            let isCurrentChat = message.chatId === currentChatId;

            if (isCurrentChat) {
                renderMessage(message, chatMessagesDiv.querySelector("#chatIndicator"));
                if (currentUserId != message.senderId) {
                    // Optionally show a toast even on the chat page
                    // showToastMessage("Message Received from " + message.fullName, "none");
                }
                connection.invoke("UserStoppedTyping", currentChatId, currentChatId);
            }
        } else {
            if (currentUserId != message.senderId) {
                showToastMessage("Message Received from " + message.fullName, "none");
            }
        }
    } catch (e) {
        console.error("Error in message handler:", e);
    }
});