const selectedSkills = new Set();
document.addEventListener("DOMContentLoaded", () => {
    // Load skills from the server-rendered list
    const skillInput = document.getElementById("skillInput");
    const skillTags = document.getElementById("skillTags");
    const skillRecommendations = document.getElementById("skillRecommendations");
    const endDateInput = document.getElementById("Close");
    const selectedSkillsInput = document.getElementById("SelectedSkill");

    // Set the minimum date to today for openDate
    const today = new Date().toISOString().split("T")[0];
    endDateInput.setAttribute("min", today);
    endDateInput.value = today;

    // Function to show recommendations
    window.showRecommendations = function () {
        const query = skillInput.value.trim().toLowerCase();
        skillRecommendations.innerHTML = ""; // Clear previous suggestions

        if (query) {
            const filteredSkills = skillList.filter(skill =>
                skill.name.toLowerCase().includes(query) && !selectedSkills.has(skill.id)
            );

            if (filteredSkills.length > 0) {
                skillRecommendations.style.display = "block";
                filteredSkills.forEach(skill => {
                    const li = document.createElement("li");
                    li.className = "list-group-item";
                    li.textContent = skill.name;
                    li.onclick = () => addSkill(skill);
                    skillRecommendations.appendChild(li);
                });
            } else {
                skillRecommendations.style.display = "none";
            }
        } else {
            skillRecommendations.style.display = "none";
        }
    };

    // Updated addSkill function
    function addSkill(skill) {
        if (!selectedSkills.has(skill.id)) {
            selectedSkills.add(skill.id);

            // Create skill tag
            const tag = document.createElement("span");
            tag.className = "badge bg-primary text-white me-2";
            tag.textContent = skill.name;

            // Create remove button
            const removeBtn = document.createElement("span");
            removeBtn.className = "ms-2 text-white";
            removeBtn.style.cursor = "pointer";
            removeBtn.textContent = "×";
            removeBtn.onclick = () => {
                selectedSkills.delete(skill.id);
                skillTags.removeChild(tag);
                selectedSkillsInput.value = Array.from(selectedSkills).join(",");
            };

            tag.appendChild(removeBtn);
            skillTags.appendChild(tag);

            // Clear input and hide recommendations
            skillInput.value = "";
            skillRecommendations.style.display = "none";
            selectedSkillsInput.value = Array.from(selectedSkills).join(",");
        }
    }
    if (selectedSkillsInput.value) {
        const loadedSkillList = selectedSkillsInput.value.split(",") // Fetch skills from hidden JSON
        console.log(skillList)
        loadedSkillList.forEach(skillId => {
            const skill = skillList.find(s => s.id == skillId);
            if (skill) {
                addSkill(skill)
            }
        });
    }
});
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