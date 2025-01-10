function initializeTagManager({
    inputSelector,
    tagsContainerSelector,
    recommendationsSelector,
    selectedItemsInputSelector,
    itemsList,
    uniqueIdField = "id",
    displayNameField = "name"
}) {
    const selectedItems = new Set();
    const inputElement = document.querySelector(inputSelector);
    const tagsContainer = document.querySelector(tagsContainerSelector);
    const recommendationsContainer = document.querySelector(recommendationsSelector);
    const selectedItemsInput = document.querySelector(selectedItemsInputSelector);

    // Function to show recommendations
    function showRecommendations() {
        const query = inputElement.value.trim().toLowerCase();
        recommendationsContainer.innerHTML = ""; // Clear previous suggestions

        if (query) {
            const filteredItems = itemsList.filter(item =>
                item[displayNameField].toLowerCase().includes(query) && !selectedItems.has(item[uniqueIdField])
            );

            if (filteredItems.length > 0) {
                recommendationsContainer.style.display = "block";
                filteredItems.forEach(item => {
                    const li = document.createElement("li");
                    li.className = "list-group-item";
                    li.textContent = item[displayNameField];
                    li.onclick = () => addItem(item);
                    recommendationsContainer.appendChild(li);
                });
            } else {
                recommendationsContainer.style.display = "none";
            }
        } else {
            recommendationsContainer.style.display = "none";
        }
    }

    // Function to add an item
    function addItem(item) {
        if (!selectedItems.has(item[uniqueIdField])) {
            selectedItems.add(item[uniqueIdField]);

            // Create tag
            const tag = document.createElement("span");
            tag.className = "badge bg-primary text-white me-2";
            tag.textContent = item[displayNameField];

            // Create remove button
            const removeBtn = document.createElement("span");
            removeBtn.className = "ms-2 text-white";
            removeBtn.style.cursor = "pointer";
            removeBtn.textContent = "×";
            removeBtn.onclick = () => {
                selectedItems.delete(item[uniqueIdField]);
                tagsContainer.removeChild(tag);
                selectedItemsInput.value = Array.from(selectedItems).join(",");
            };

            tag.appendChild(removeBtn);
            tagsContainer.appendChild(tag);

            // Clear input and hide recommendations
            inputElement.value = "";
            recommendationsContainer.style.display = "none";
            selectedItemsInput.value = Array.from(selectedItems).join(",");
        }
    }

    // Initialize preloaded selected items
    if (selectedItemsInput.value) {
        const loadedItemIds = selectedItemsInput.value.split(",");
        loadedItemIds.forEach(itemId => {
            const item = itemsList.find(i => i[uniqueIdField] == itemId);
            if (item) {
                addItem(item);
            }
        });
    }

    // Attach event listeners
    inputElement.addEventListener("input", showRecommendations);
}


function toggleCustomField(selectElement, customFieldId) {
    const customField = document.querySelector(customFieldId);
    if (selectElement.value === "Other") {
        customField.style.display = "block";
        customField.querySelector("input").required = true;
    } else {
        customField.style.display = "none";
        customField.querySelector("input").required = false;
        customField.querySelector("input").value = "";
    }
};