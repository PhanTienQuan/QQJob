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

function initializeSocialLinksManager({
    socialLinksContainerSelector,
    addSocialLinkBtnSelector,
    maxSocialLinks = 5,
    initialLinks = [],
    errorMessage
}) {
    const socialLinksContainer = document.querySelector(socialLinksContainerSelector);
    const addSocialLinkBtn = document.querySelector(addSocialLinkBtnSelector);

    // Add new social link functionality
    addSocialLinkBtn.addEventListener("click", (event) => {
        event.preventDefault(); // Prevent default button action

        const currentLinks = socialLinksContainer.querySelectorAll(".rt-input-group").length;

        if (currentLinks < maxSocialLinks) {
            createSocialLinkGroup("", ""); // Create an empty input group
        } else {
            showToastMessage(errorMessage, "warning");
        }
    });

    // Function to create a new social link group
    function createSocialLinkGroup(platform = "", url = "") {
        const currentLinks = socialLinksContainer.querySelectorAll(".rt-input-group").length;
        const container = document.createElement("div");
        container.className = "position-relative";
        const newSocialLink = document.createElement("div");
        newSocialLink.className = "rt-input-group";

        // Hidden platform input
        const platformInput = document.createElement("input");
        platformInput.setAttribute("hidden", true);
        platformInput.type = "hidden";
        platformInput.value = platform;

        // Label for platform
        const label = document.createElement("label");
        label.textContent = platform || `New Social link ${currentLinks + 1}`;
        label.addEventListener("dblclick", () => {
            const input = document.createElement("input");
            input.type = "text";
            input.value = label.textContent;
            input.addEventListener("blur", () => {
                label.textContent = input.value || label.textContent;
                platformInput.value = input.value || platformInput.value;
                label.style.display = "";
                input.remove();
            });
            label.style.display = "none";
            newSocialLink.insertBefore(input, label);
            input.focus();
        });

        // Input for URL
        const urlInput = document.createElement("input");
        urlInput.type = "text";
        urlInput.placeholder = "Enter URL";
        urlInput.required = true;
        urlInput.value = url;
        urlInput.className = "form-control pe-5";

        // Remove icon
        const removeIcon = document.createElement("span");
        removeIcon.innerHTML = "<i class='fa-solid fa-xmark'></i>";
        removeIcon.className = "remove-icon";
        removeIcon.style.cursor = "pointer";
        removeIcon.onclick = () => {
            socialLinksContainer.removeChild(container); // Remove the input group
            reindexInputs(); // Re-index remaining inputs
        };

        // Data validation
        const dataValidationSpan = document.createElement("span");
        dataValidationSpan.className = "text-danger field-validation-valid";
        dataValidationSpan.setAttribute("data-valmsg-for", `SocialLinks[${currentLinks}].Url`);
        dataValidationSpan.setAttribute("data-valmsg-replace", true);

        // Append elements
        newSocialLink.appendChild(label);
        newSocialLink.appendChild(platformInput);
        newSocialLink.appendChild(urlInput);
        newSocialLink.appendChild(removeIcon);
        container.appendChild(newSocialLink);
        container.appendChild(dataValidationSpan);

        socialLinksContainer.appendChild(container);
        reindexInputs(); // Re-index inputs after adding a new one
    }

    // Function to re-index all input groups
    function reindexInputs() {
        const inputGroups = socialLinksContainer.querySelectorAll(".rt-input-group");
        inputGroups.forEach((group, index) => {
            // Re-index platform input
            const platformInput = group.querySelector("input[type='hidden']");
            platformInput.id = `SocialLinks_${index}__Platform`;
            platformInput.name = `SocialLinks[${index}].Platform`;

            // Re-index URL input
            const urlInput = group.querySelector("input[type='text']");
            urlInput.id = `SocialLinks_${index}__Url`;
            urlInput.name = `SocialLinks[${index}].Url`;

            // Update label "for" attribute
            const label = group.querySelector("label");
            label.setAttribute("for", platformInput.id);

            // Update validation
            const validateSpan = group.parentElement.querySelector("span[data-valmsg-for]");
            validateSpan.setAttribute("data-valmsg-for", `SocialLinks[${index}].Url`);
        });
    }

    // Function to populate existing social links
    function populateSocialLinks() {
        socialLinksContainer.innerHTML = ""; // Clear container
        initialLinks.forEach(link => {
            createSocialLinkGroup(link.platform, link.url);
        });
    }

    // Initialize existing social links
    populateSocialLinks();
}

function initializeSkillTagsManager({
    skillTagsSelector,
    addSkillBtnSelector,
    fieldInputSelector,
    initialCompanyField = []
}) {
    const skillTags = document.querySelector(skillTagsSelector);
    const addSkillBtn = document.querySelector(addSkillBtnSelector);
    const fieldInput = document.querySelector(fieldInputSelector);


    addSkillBtn.addEventListener("click", () => {
        // Create input field
        const inputField = document.createElement("input");
        inputField.type = "text";
        inputField.className = "skill__input";
        inputField.placeholder = "Enter skill";

        // Add save and cancel buttons
        const saveBtn = document.createElement("button");
        saveBtn.innerHTML = "<i class='fa-solid fa-check'></i>";
        saveBtn.className = "save-skill-btn";

        const cancelBtn = document.createElement("button");
        cancelBtn.innerHTML = "<i class='fa-solid fa-xmark'></i>";
        cancelBtn.className = "cancel-skill-btn";

        const inputContainer = document.createElement("li");
        inputContainer.className = "inputContainer";
        inputContainer.appendChild(inputField);
        inputContainer.appendChild(saveBtn);
        inputContainer.appendChild(cancelBtn);

        // Replace the add button with the input container
        skillTags.replaceChild(inputContainer, addSkillBtn);

        // Focus on input field
        inputField.focus();

        // Save skill
        saveBtn.addEventListener("click", () => {
            const skillValue = inputField.value.trim();
            if (skillValue) {
                createSkillTag(skillValue, inputContainer);
                fieldInput.value = fieldInput.value ? `${fieldInput.value}, ${skillValue}` : skillValue;
            }
            skillTags.replaceChild(addSkillBtn, inputContainer); // Restore add button
        });

        // Cancel input
        cancelBtn.addEventListener("click", () => {
            skillTags.replaceChild(addSkillBtn, inputContainer); // Restore add button
        });
    });
    
    function createSkillTag(skillValue, btn) {
        const placeholder = skillTags.querySelector(".placeholder");
        if (placeholder) {
            skillTags.removeChild(placeholder);
        }

        const newSkill = document.createElement("li");
        newSkill.className = "skill__item_parent";
        newSkill.innerHTML = `<span class="skill__item">${skillValue}</span>`;

        const removeBtn = document.createElement("span");
        removeBtn.innerHTML = `<i class="fa-regular fa-xmark"></i>`;
        removeBtn.style.cursor = "pointer";
        removeBtn.addEventListener("click", () => {
            skillTags.removeChild(newSkill); // Remove the skill item
            const regex = new RegExp(`(^|,\\s?)${skillValue}(,\\s?|$)`, "g");
            fieldInput.value = fieldInput.value.replace(regex, "$1").replace(/,\s*$/, "");
            // Add placeholder back if no skills remain
            if (!skillTags.querySelector(".skill__item_parent")) {
                const placeholder = document.createElement("li");
                placeholder.className = "placeholder";
                placeholder.innerHTML = `<span>Your company has not added any fields yet</span>`;
                skillTags.insertBefore(placeholder, addSkillBtn.closest('li'));
            }
        });
        newSkill.appendChild(removeBtn);

        skillTags.insertBefore(newSkill, btn); // Add before input container
    }

    function populateSkill() {
        initialCompanyField.forEach(skill => createSkillTag(skill, addSkillBtn));
    }

    populateSkill();
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