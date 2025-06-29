const messageInput = document.getElementById("messageInput");
const sendButton = document.querySelector(".message__btn");
const chatMessagesDiv = document.getElementById("chatMessages");
const searchInputs = document.querySelectorAll("#search");
const chatSessionList = document.querySelectorAll(".chat__user__list");
const typingDelay = 5000;
const loadBatchSize = 10;
let loadedMessagesCount = 10;
let hasMore = true;

let typingTimeout;
function handleSendButtonClick(e) {
    e.preventDefault();
    const message = messageInput.value;
    if (message != "") {
        connection.invoke("SendMessage", currentChatId, currentUserId, message);
        messageInput.value = "";
    }
}
function handleMessageInput() {
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
}

window.addEventListener("DOMContentLoaded", () => {
    if (chatMessagesDiv) {
        chatMessagesDiv.scrollTop = chatMessagesDiv.scrollHeight;
    }
    renderSession("");
    highlightCurrentSession();
});

chatMessagesDiv.addEventListener('scroll', function () {
    if (chatMessagesDiv.scrollTop === 0 && hasMore) {

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
            },
            error: function () {
                console.error('Failed to load messages');
            }
        });
    }
});

function loadSessionMessages(sessionId) {
    try {
        hasMore = false;
        const messagePreview = document.querySelectorAll(`#MessagePreview_${currentChatId}`);
        const countPreview = document.querySelectorAll(`#Count_${currentChatId}`);
        const el = document.querySelector(`[data-sessionid="${sessionId}"]`);

        if (el) {
            const img = el.querySelector('img');
            if (img) {
                document.querySelector(`#header__img`).src = img.src;
            }
        }

        messagePreview.forEach(function (element) {
            element.style.fontWeight = "normal";
        })

        countPreview.forEach(function (element) {
            element.innerHTML = 0;
        })

        if (sessionId === currentChatId) return; // Already active

        loadedMessagesCount = 10;
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
                console.log(response);
                console.log(realMessages);
                if (realMessages.length > 0) {
                    realMessages.forEach(msg => {
                        renderMessage(msg, chatMessagesDiv.firstChild);
                    });
                }

                if (response.otherUserAvailable) {
                    $('#messageForm').show();
                    $('#userNotAvailableMsg').hide();
                } else {
                    $('#messageForm').hide();
                    $('#userNotAvailableMsg').show();
                }
            },
            error: function () {
                console.error('Failed to load messages');
            }
        });
        currentChatId = sessionId;
        highlightCurrentSession();
    } catch (e) {
        console.log(e)
    }
}

window.connection.on("ShowTyping", function (chatId) {
    const typingIndicator = document.getElementById("typingIndicator");
    const typingIndicator_avatar = document.getElementById("typingIndicator_avatar");
    if (typingIndicator && chatId == currentChatId) {
        typingIndicator.style.display = "flex";
        typingIndicator_avatar.style.display = "grid";
        chatMessagesDiv.scrollTop = chatMessagesDiv.scrollHeight;
    }
});

window.connection.on("HideTyping", function (senderId) {
    const typingIndicator = document.getElementById("typingIndicator");
    const typingIndicator_avatar = document.getElementById("typingIndicator_avatar");
    if (typingIndicator) {
        typingIndicator.style.display = "none";
        typingIndicator_avatar.style.display = "none";
        chatMessagesDiv.scrollTop = chatMessagesDiv.scrollHeight;
    }
});

let searchName = "";
searchInputs.forEach(function (element) {
    element.addEventListener("input", function () {
        searchName = element.value;
        loadFilteredSessions();
    });
});

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
    renderSession(searchName);
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
    const messageAvatar = message.avatar && message.avatar.trim() !== "" ? message.avatar : "/assets/img/avatars/default-avatar.jpg";
    msgDiv.innerHTML = `
        <div class="avatar">
            <img src="${messageAvatar}" alt="">
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
            chatSessionList.forEach(function (element) {
                element.innerHTML = "";
                const sessions = response.sessions.$values;
                if (sessions && sessions.length > 0) {
                    sessions.forEach(function (session) {
                        var sessionResult = session.result;
                        const otherUser = sessionResult.user1Id == currentUserId ? sessionResult.user2 : sessionResult.user1;
                        const otherUserAvatar = otherUser && otherUser.avatar !== ""
                            ? otherUser.avatar
                            : "/assets/img/avatars/default-avatar.jpg";

                        const otherUserName = otherUser && otherUser.fullName && otherUser.fullName !== ""
                            ? otherUser.fullName
                            : "Unknown User";

                        const unreadMessages = sessionResult.unreadCount || 0;

                        const lastMessage = sessionResult.messages?.$values.length > 0
                            ? sessionResult.messages.$values[sessionResult.messages.$values.length - 1]
                            : "No messages";

                        const isCurrentUser = lastMessage && lastMessage.senderId === currentUserId;
                        const timeText = lastMessage == undefined ? formatTimeAgo(lastMessage.sentAt) : "";
                        const previewText = lastMessage != undefined
                            ? (isCurrentUser
                                ? `You: ${lastMessage.messageText}`
                                : `${otherUserName}: ${lastMessage.messageText}`)
                            : "No messages yet";

                        const previewWeight = !isCurrentUser && unreadMessages > 0 ? "bold" : "normal";

                        const sessionHtml = `
                            <div class="single__chat__person" data-sessionId="${sessionResult.chatId}" onclick="loadSessionMessages('${sessionResult.chatId}')">
                                <div class="d-flex align-items-center gap-30">
                                    <div class="avater">
                                        <img src="${otherUserAvatar}" alt="">
                                    </div>
                                    <div class="chat__person__meta">
                                        <h6 class="font-20 fw-medium mb-0">${otherUserName}</h6>
                                        <p id="MessagePreview_${sessionResult.chatId}" style="font-weight: ${previewWeight}">${previewText}</p>
                                    </div>
                                </div>
                                <div class="right__count">
                                    <span class="time" id="Time_${sessionResult.chatId}">${timeText}</span>
                                    <span class="count" id="Count_${sessionResult.chatId}">${unreadMessages}</span>
                                </div>
                            </div>
                            `;

                        element.insertAdjacentHTML('beforeend', sessionHtml);
                        highlightCurrentSession();
                    });
                } else {
                    element.innerHTML = "<div>No sessions found</div>";
                }
            });
        },
        error: function () {
            chatSessionList.forEach(function (element) {
                element.innerHTML = "<div>Error loading sessions</div>";
            });
        }
    });
}
function highlightCurrentSession() {
    const currentSession = document.querySelectorAll(`.single__chat__person[data-sessionId="${currentChatId}"]`);

    // Remove existing highlight
    const previouslyHighlighted = document.querySelectorAll(".single__chat__person.highlight");
    if (previouslyHighlighted) {
        previouslyHighlighted.forEach(function (element) {
            element.classList.remove("highlight");
        });
    }

    if (currentSession) {
        // Highlight the current session if it's visible
        currentSession.forEach(function (element) {
            element.classList.add("highlight");
        });
    } else {
        // Otherwise highlight the first session in the list
        const firstSession = document.querySelectorAll(".single__chat__person")[0];
        if (firstSession) {
            firstSession.classList.add("highlight");
        }
    }
}