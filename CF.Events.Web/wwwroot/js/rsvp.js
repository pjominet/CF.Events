(function() {
    const options = document.getElementById("additionalOptions");
    document.querySelectorAll('input[name="Input.Attending"]').forEach(function (radio) {
        radio.addEventListener("change", function () {
            options.style.display = (this.value === "true" || this.value === "True") ? "block" : "none";
        });
    });

    // Kids dynamic rows
    const kidsCheckbox = document.getElementById("kids");
    const kidsSection = document.getElementById("kidsDetailsSection");
    if (kidsCheckbox && kidsSection) {
        kidsCheckbox.addEventListener("change", function () {
            kidsSection.style.display = this.checked ? "block" : "none";
        });
    }

    const addKidBtn = document.getElementById("addKidBracketBtn");
    const kidSelect = document.getElementById("kidAgeBracketSelect");
    if (addKidBtn && kidSelect) {
        addKidBtn.addEventListener("click", function () {
            let bracket = kidSelect.value;
            if (!bracket) return;
            let row = document.querySelector(`.kids-row[data-bracket="${bracket}"]`);
            if (row) {
                row.classList.remove("d-none");
                let input = row.querySelector(".kid-count-input");
                if (input && input.value === 0) {
                    input.value = 1;
                }
            }
            kidSelect.value = "";
        });
    }

    // Accommodation duration
    const accCheckbox = document.getElementById("accommodation");
    const accDurationWrapper = document.getElementById("accommodationDurationWrapper");
    const accCodeAlert = document.getElementById("accommodationCodeAlert");
    if (accCheckbox) {
        accCheckbox.addEventListener("change", function () {
            if (accDurationWrapper)
                accDurationWrapper.style.display = this.checked ? "block" : "none";
            if (accCodeAlert)
                accCodeAlert.style.display = this.checked ? "block" : "none";
        });
    }

    // Dietary options visibility
    const dietarySection = document.getElementById("dietarySection");
    const foodCheckboxes = [
        document.getElementById("breakfast"),
        document.getElementById("brunch"),
        document.getElementById("lunch"),
        document.getElementById("dinner")
    ];

    if (dietarySection) {
        const updateDietaryVisibility = () => {
            const anyFoodSelected = foodCheckboxes.some(cb => cb && cb.checked);
            dietarySection.style.display = anyFoodSelected ? "block" : "none";
        };

        foodCheckboxes.forEach(cb => {
            if (cb) {
                cb.addEventListener("change", updateDietaryVisibility);
            }
        });
    }
})();
