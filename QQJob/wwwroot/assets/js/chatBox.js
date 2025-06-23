document.addEventListener("DOMContentLoaded", function () {
    const chatWidget = document.getElementById("chatWidget");
    const openChat = document.getElementById("openChat");
    const closeChat = document.getElementById("closeChat");
    const sendMessageButton = document.getElementById("sendMessage");
    const userMessageInput = document.getElementById("userMessage");
    const chatHistory = document.getElementById("chatHistory");
    const typingIndicator = document.getElementById("typingIndicator");
    let isChatOpen = false;

    function toggleChat() {
        isChatOpen = !isChatOpen;
        chatWidget.style.display = isChatOpen ? "flex" : "none";
        if (isChatOpen) userMessageInput.focus();
    }

    openChat.addEventListener("click", toggleChat);
    closeChat.addEventListener("click", () => {
        isChatOpen = false;
        chatWidget.style.display = "none";
    });

    document.addEventListener("click", function (event) {
        const isClickInsideChat = chatWidget.contains(event.target) || openChat.contains(event.target);
        if (!isClickInsideChat && isChatOpen) {
            isChatOpen = false;
            chatWidget.style.display = "none";
        }
    });

    function getSessionHistory() {
        return JSON.parse(sessionStorage.getItem("chatHistory") || "[]");
    }

    function saveSessionHistory(message, role = "User") {
        const history = getSessionHistory();
        history.push({ role, content: message });
        sessionStorage.setItem("chatHistory", JSON.stringify(history));
    }

    function addMessage(content, className) {
        const messageElement = document.createElement("div");
        messageElement.classList.add(className);
        messageElement.innerHTML = content;
        chatHistory.insertBefore(messageElement, typingIndicator);
        chatHistory.scrollTop = chatHistory.scrollHeight;
    }

    function handleSendMessage() {
        const userMessage = userMessageInput.value.trim();
        if (!userMessage) return;

        addMessage(userMessage, "user-message");
        saveSessionHistory(userMessage, "User");
        userMessageInput.value = "";

        // 1. Show typing indicator and note start time
        const typingIndicator = document.getElementById("typingIndicator");
        typingIndicator.style.display = "flex";
        chatHistory.scrollTop = chatHistory.scrollHeight;
        const start = Date.now();

        // 2. Send fetch immediately
        fetch("/api/chat", {
            method: "POST",
            headers: {
                "Content-Type": "application/json"
            },
            body: JSON.stringify({
                sender: senderId,
                receiver: "system",
                content: userMessage,
                history: getSessionHistory()
            })
        })
            .then(response => response.json())
            .then(data => {
                // 3. When response arrives, check how long has passed
                const elapsed = Date.now() - start;
                const minDelay = 1000; // 1 second

                function showBotMessage() {
                    typingIndicator.style.display = "none";
                    const botMessage = data.message || "<p>Sorry, I couldn't understand your request.</p>";
                    addMessage(botMessage, "bot-message");
                    saveSessionHistory(botMessage, "Bot");
                }

                if (elapsed < minDelay) {
                    setTimeout(showBotMessage, minDelay - elapsed);
                } else {
                    showBotMessage();
                }
            })
            .catch(error => {
                typingIndicator.style.display = "none";
                console.error("Chat error:", error);
            });
    }


    sendMessageButton.addEventListener("click", handleSendMessage);

    userMessageInput.addEventListener("keypress", function (event) {
        if (event.key === "Enter") {
            event.preventDefault();
            handleSendMessage();
        }
    });
    window.addEventListener("load", function () {
        sessionStorage.removeItem("chatHistory");
        sessionStorage.removeItem("lastQueryResult");
    });
});