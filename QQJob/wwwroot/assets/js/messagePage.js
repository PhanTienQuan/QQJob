const messageInput = document.getElementById("messageInput");
const sendButton = document.querySelector(".message__btn");
const chatMessagesDiv = document.getElementById("chatMessages");
const searchInput = document.getElementById("search");
const chatSessionList = document.querySelector(".chat__user__list");
const typingDelay = 5000;
const loadBatchSize = 10;
let loadedMessagesCount = 10;
let hasMore = true;

let typingTimeout;
const connection = new signalR.HubConnectionBuilder()
    .withUrl("/chathub")
    .build();

connection.start().then(() => {
    document.querySelectorAll(".single__chat__person").forEach(session => {
        const chatId = session.dataset.sessionid;
        connection.invoke("JoinChat", chatId);
    });
});

connection.on("ReceiveMessage", function (response) {
    const message = response.message;

    const messagePreview = document.getElementById(message.chatId + "_messagePreview");
    const countPreview = document.getElementById(message.chatId + "_count");
    const timePreview = document.getElementById(message.chatId + "_time");
    const isCurrentUser = message.senderId === currentUserId;
    const unreadCount = isCurrentUser ? 0 : response.unreadMessagesCount;

    // Update chat preview
    if (countPreview) countPreview.innerHTML = unreadCount;
    if (messagePreview) {
        messagePreview.innerHTML = isCurrentUser ? `You: ${message.messageText}` : `${message.sender.fullName}: ${message.messageText}`;
        messagePreview.style.fontWeight = unreadCount === 0 ? "normal" : "bold";
    }
    if (timePreview) timePreview.innerHTML = formatTimeAgo(message.sentAt);

    // 🔼 Move session to top
    const sessionElement = document.querySelector(`.single__chat__person[data-sessionid="${message.chatId}"]`);

    if (sessionElement && chatSessionList) {
        chatSessionList.removeChild(sessionElement);
        chatSessionList.insertBefore(sessionElement, chatSessionList.firstChild);
    }

    // Show in chat window if it's the current chat
    if (message.chatId !== currentChatId) return;

    renderMessage(message, chatMessagesDiv.querySelector("#chatIndicator"));
    connection.invoke("UserStoppedTyping", currentChatId, currentChatId);
});

sendButton.addEventListener("click", function (e) {
    e.preventDefault();
    const message = messageInput.value;
    connection.invoke("SendMessage", currentChatId, currentUserId, message);
    messageInput.value = "";
});

messageInput.addEventListener("input", function () {
    const isEmpty = messageInput.value.trim() === "";
    sendButton.disabled = isEmpty;

    if (isEmpty) {
        sendButton.style.backgroundColor = "#ccc";
        sendButton.style.cursor = "not-allowed";
        connection.invoke("UserStoppedTyping", currentChatId, currentChatId);
    } else {
        connection.invoke("UserTyping", currentChatId, currentUserId);

        // Reset debounce
        clearTimeout(typingTimeout);
        typingTimeout = setTimeout(() => {
            connection.invoke("UserStoppedTyping", currentChatId, currentUserId);
        }, typingDelay);
        sendButton.style.backgroundColor = "#34A853"; // blue when enabled
        sendButton.style.cursor = "pointer";
    }
});

window.addEventListener("DOMContentLoaded", () => {
    sendButton.disabled = true;
    sendButton.style.backgroundColor = "#ccc";
    sendButton.style.cursor = "not-allowed";
    if (chatMessagesDiv) {
        chatMessagesDiv.scrollTop = chatMessagesDiv.scrollHeight;
    }
    renderSession("");
    highlightCurrentSession();
});

chatMessagesDiv.addEventListener('scroll', function () {
    if (chatMessagesDiv.scrollTop === 0 && hasMore) {
        isLoadingMore = true;

        const currentScrollHeight = chatMessagesDiv.scrollHeight;

        $.ajax({
            url: '/Message/GetMessages',
            data: {
                chatId: currentChatId,
                skip: loadedMessagesCount,
                take: loadBatchSize
            },
            success: function (response) {
                const realMessages = response.messages.$values || [];

                if (realMessages.length > 0) {
                    realMessages.forEach(msg => {
                        renderMessage(msg, chatMessagesDiv.firstChild);
                    });

                    // Maintain scroll position
                    const newScrollHeight = chatMessagesDiv.scrollHeight;
                    chatMessagesDiv.scrollTop = newScrollHeight - currentScrollHeight;

                    loadedMessagesCount += realMessages.length;
                }

                hasMore = response.hasMore;
                isLoadingMore = false;
            },
            error: function () {
                console.error('Failed to load messages');
                isLoadingMore = false;
            }
        });
    }
});

function loadSessionMessages(sessionId) {
    hasMore = false;
    const messagePreview = document.getElementById(currentChatId + "_messagePreview");
    const countPreview = document.getElementById(currentChatId + "_count");
    countPreview.innerHTML = 0;
    messagePreview.style.fontWeight = "normal";

    if (sessionId === currentChatId) return; // Already active

    loadedMessagesCount = 10;
    isLoadingMore = false;
    hasMore = true;

    document.querySelectorAll('.chat__message').forEach(el => el.remove());

    $.ajax({
        url: '/Message/GetMessages',
        data: {
            chatId: sessionId,
            skip: 0,
            take: 10,
            previousChatId: currentChatId,
            currentUserId: currentUserId
        },
        success: function (response) {
            const realMessages = response.messages.$values || [];
            if (realMessages.length > 0) {
                realMessages.forEach(msg => {
                    renderMessage(msg, chatMessagesDiv.firstChild);
                });
            }
        },
        error: function () {
            console.error('Failed to load messages');
        }
    });
    currentChatId = sessionId;
    highlightCurrentSession();
}

connection.on("ShowTyping", function (senderId) {
    const typingIndicator = document.getElementById("typingIndicator");
    const typingIndicator_avatar = document.getElementById("typingIndicator_avatar");
    if (typingIndicator) {
        typingIndicator.style.display = "flex";
        typingIndicator_avatar.style.display = "grid";
        chatMessagesDiv.scrollTop = chatMessagesDiv.scrollHeight;
    }
});

connection.on("HideTyping", function (senderId) {
    const typingIndicator = document.getElementById("typingIndicator");
    const typingIndicator_avatar = document.getElementById("typingIndicator_avatar");
    if (typingIndicator) {
        typingIndicator.style.display = "none";
        typingIndicator_avatar.style.display = "none";
        chatMessagesDiv.scrollTop = chatMessagesDiv.scrollHeight;
    }
});


searchInput.addEventListener("input", loadFilteredSessions);

let currentFilter = "";
document.querySelectorAll(".message__filter .nav-link").forEach(link => {
    link.addEventListener("click", function (e) {
        e.preventDefault();

        // Update active class
        document.querySelectorAll(".message__filter .nav-link").forEach(l => l.classList.remove("active"));
        this.classList.add("active");

        // Set filter based on clicked text
        const text = this.textContent.trim().toLowerCase();
        if (text === "unread") {
            currentFilter = false; // false means show unread
        } else if (text === "read") {
            currentFilter = true;  // true means show read
        } else {
            currentFilter = "";
        }

        loadFilteredSessions();
    });
});
function loadFilteredSessions() {
    const name = searchInput.value.trim();
    renderSession(name);
}
function formatTimeAgo(dateString) {
    const date = new Date(dateString);
    const now = new Date();
    const seconds = Math.floor((now - date) / 1000);

    const timeIntervals = [
        { label: 'year', seconds: 31536000 },
        { label: 'month', seconds: 2592000 },
        { label: 'day', seconds: 86400 },
        { label: 'hour', seconds: 3600 },
        { label: 'minute', seconds: 60 },
        { label: 'second', seconds: 1 }
    ];

    for (const interval of timeIntervals) {
        const count = Math.floor(seconds / interval.seconds);
        if (count >= 1) {
            const plural = count === 1 ? '' : 's';
            const result = `${count} ${interval.label}${plural} ago`;
            return result.charAt(0).toUpperCase() + result.slice(1);
        }
    }

    return "Just now";
}
function renderMessage(message, anchorElement) {
    const msgDiv = document.createElement("div");
    const isCurrentUser = message.senderId === currentUserId;
    msgDiv.className = isCurrentUser ? "msg receiver chat__message" : "msg sender chat__message";
    msgDiv.innerHTML = `
        <div class="avatar">
            <img src="${message.avatar}" alt="">
        </div>
        <div class="content">
            <p>${message.messageText}</p>
            <span class="time">${new Date(message.sentAt).toLocaleString()}</span>
        </div>
    `;
    chatMessagesDiv.insertBefore(msgDiv, anchorElement);
    chatMessagesDiv.scrollTop = chatMessagesDiv.scrollHeight;
}
function renderSession(name) {
    $.ajax({
        url: '/Message/GetSessions',
        method: 'GET',
        data: {
            userId: currentUserId,
            name: name,
            isRead: currentFilter !== "" ? currentFilter : null
        },
        success: function (response) {
            chatSessionList.innerHTML = "";
            const sessions = response.sessions.$values;
            if (sessions && sessions.length > 0) {
                sessions.forEach(function (session) {
                    var sessionResult = session.result;
                    const otherUser = sessionResult.user1Id == currentUserId ? sessionResult.user2 : sessionResult.user1
                    const unreadMessages = sessionResult.unreadCount || 0;

                    const lastMessage = sessionResult.messages?.$values.length > 0
                        ? sessionResult.messages.$values[sessionResult.messages.$values.length - 1]
                        : "No messages";
                    const isCurrentUser = lastMessage && lastMessage.senderId === currentUserId;
                    const timeText = lastMessage == undefined ? formatTimeAgo(lastMessage.sentAt) : "";
                    const previewText = lastMessage == undefined    
                        ? (isCurrentUser
                            ? `You: ${lastMessage.messageText}`
                            : `${otherUser.fullName}: ${lastMessage.messageText}`)
                        : "No messages yet";

                    const previewWeight = !isCurrentUser && unreadMessages > 0 ? "bold" : "normal";

                    const sessionHtml = `
                    <div class="single__chat__person" data-sessionId="${sessionResult.chatId}" onclick="loadSessionMessages('${sessionResult.chatId}')">
                        <div class="d-flex align-items-center gap-30">
                            <div class="avater">
                                <img src="${otherUser.avatar}" alt="">
                            </div>
                            <div class="chat__person__meta">
                                <h6 class="font-20 fw-medium mb-0">${otherUser.fullName}</h6>
                                <p id="${sessionResult.chatId}_messagePreview" style="font-weight: ${previewWeight}">${previewText}</p>
                            </div>
                        </div>
                        <div class="right__count">
                            <span class="time" id="${sessionResult.chatId}_time">${timeText}</span>
                            <span class="count" id="${sessionResult.chatId}_count">${unreadMessages}</span>
                        </div>
                    </div>
                    `;

                    chatSessionList.insertAdjacentHTML('beforeend', sessionHtml);
                    highlightCurrentSession();
                });
            } else {
                chatSessionList.innerHTML = "<div>No sessions found</div>";
            }
        },
        error: function () {
            chatSessionList.innerHTML = "<div>Error loading sessions</div>";
        }
    });
}
function highlightCurrentSession() {
    const currentSession = document.querySelector(`.single__chat__person[data-sessionId="${currentChatId}"]`);

    // Remove existing highlight
    const previouslyHighlighted = document.querySelector(".single__chat__person.highlight");
    if (previouslyHighlighted) {
        previouslyHighlighted.classList.remove("highlight");
    }

    if (currentSession) {
        // Highlight the current session if it's visible
        currentSession.classList.add("highlight");
    } else {
        // Otherwise highlight the first session in the list
        const firstSession = document.querySelector(".single__chat__person");
        if (firstSession) {
            firstSession.classList.add("highlight");
        }
    }
}