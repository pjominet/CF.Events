(function () {
    const steps = document.querySelectorAll(".form-step");
    const indicators = document.querySelectorAll(".step-indicator");
    const stepperProgress = document.querySelector(".stepper-progress");
    const nextBtn = document.querySelector(".btn-next");
    const prevBtn = document.querySelector(".btn-prev");
    const submitBtn = document.querySelector(".btn-submit");
    const cancelLink = document.getElementById("cancelLink");

    // If the RSVP form is not present (already responded view), skip stepper setup
    if (!nextBtn || !prevBtn) return;

    // Step 1 = attendance selection (no stepper), steps 2-4 = stepper steps 1-3
    let currentStep = 1;

    function showStep(stepNumber) {
        // Show the correct form-step div
        steps.forEach((step, index) => {
            step.classList.toggle("d-none", index !== stepNumber - 1);
        });

        // Stepper indicators map to steps 2-4 (indices 0-2)
        indicators.forEach((ind, index) => {
            ind.classList.toggle("active", index === stepNumber - 2);
        });

        // Navigation button visibility
        prevBtn.classList.toggle("d-none", stepNumber === 1);
        cancelLink.classList.toggle("d-none", stepNumber !== 1);

        // Last step (4) shows submit, others show next
        if (stepNumber === 4) {
            nextBtn.classList.add("d-none");
            submitBtn.classList.remove("d-none");
        } else {
            nextBtn.classList.remove("d-none");
            submitBtn.classList.add("d-none");
        }

        // Show/hide stepper progress (only for attending flow, steps 2-4)
        const isAttending = document.querySelector('input[name="NewRsvp.Attending"]:checked')?.value === "true";
        if (isAttending && stepNumber >= 2) {
            stepperProgress.classList.remove("d-none");
        } else {
            stepperProgress.classList.add("d-none");
        }

        // Show/hide comment section based on attendance
        const commentSection = document.querySelector(".commentSection");
        if (commentSection) {
            commentSection.classList.toggle("d-none", !isAttending);
        }

        currentStep = stepNumber;
    }

    // Handle "Next" button clicks
    nextBtn.addEventListener("click", () => {
        const isAttending = document.querySelector('input[name="NewRsvp.Attending"]:checked')?.value === "true";

        if (currentStep === 1) {
            if (isAttending) {
                showStep(2);
            } else {
                showStep(4);
            }
        } else if (currentStep === 2) {
            showStep(3);
        } else if (currentStep === 3) {
            showStep(4);
        }
    });

    // Handle "Back" button clicks
    prevBtn.addEventListener("click", () => {
        if (currentStep === 4) {
            const isAttending = document.querySelector('input[name="NewRsvp.Attending"]:checked')?.value === "true";
            showStep(isAttending ? 3 : 1);
        } else if (currentStep === 3) {
            showStep(2);
        } else if (currentStep === 2) {
            showStep(1);
        }
    });

    // Handle day checkbox changes
    document.querySelectorAll(".day-checkbox").forEach(checkbox => {
        checkbox.addEventListener("change", function () {
            const day = this.getAttribute("data-day");
            const input = document.querySelector(`input[name="NewRsvp.AttendanceDays[${day}]"]`);
            if (input) {
                input.disabled = !this.checked;
            }
        });
    });

    // Handle dietary options changes to show/hide number of people
    const dietaryOptionsSelect = document.getElementById("dietaryOptions");
    const dietaryOtherDetails = document.getElementById("dietaryOtherDetails");
    const dietaryNbrPeopleInput = document.getElementById("dietaryNbrPeople");
    const dietaryOptionsDetails = document.getElementById("dietaryOptionsDetails");

    function updateDietaryVisibility() {
        if (!dietaryNbrPeopleInput || !dietaryOptionsDetails) return;

        const count = parseInt(dietaryNbrPeopleInput.value) || 0;
        const shouldShow = count > 0;

        dietaryOptionsDetails.style.display = shouldShow ? "block" : "none";

        // If hidden, clear the other fields
        if (!shouldShow) {
            if (dietaryOptionsSelect && dietaryOptionsSelect.tomselect) {
                dietaryOptionsSelect.tomselect.clear();
            }
            if (dietaryOtherDetails) {
                dietaryOtherDetails.value = "";
            }
        }
    }

    if (dietaryNbrPeopleInput) {
        dietaryNbrPeopleInput.addEventListener("input", updateDietaryVisibility);
        dietaryNbrPeopleInput.addEventListener("change", updateDietaryVisibility);
    }

    // Initial check
    updateDietaryVisibility();

})();
