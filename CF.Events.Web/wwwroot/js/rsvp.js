(function () {
    const steps = document.querySelectorAll(".form-step");
    const indicators = document.querySelectorAll(".step-indicator");
    const stepperProgress = document.querySelector(".stepper-progress");
    const nextBtn = document.querySelector(".btn-next");
    const prevBtn = document.querySelector(".btn-prev");
    const submitBtn = document.querySelector(".btn-submit");
    const cancelLink = document.getElementById("cancelLink");
    let currentStep = 1;

    function showStep(stepNumber) {
        steps.forEach((step, index) => {
            step.classList.toggle("d-none", index !== stepNumber - 1);
        });
        indicators.forEach((ind, index) => {
            ind.classList.toggle("active", index === stepNumber - 1);
        });

        // Navigation button visibility
        prevBtn.classList.toggle("d-none", stepNumber === 1);
        cancelLink.classList.toggle("d-none", stepNumber !== 1);

        if (stepNumber === 3) {
            nextBtn.classList.add("d-none");
            submitBtn.classList.remove("d-none");
        } else {
            nextBtn.classList.remove("d-none");
            submitBtn.classList.add("d-none");
        }

        currentStep = stepNumber;
    }

    // Handle "Next" button clicks
    nextBtn.addEventListener("click", () => {
        const isAttending = document.querySelector('input[name="Input.Attending"]:checked')?.value === "true";

        if (currentStep === 1) {
            if (isAttending) {
                showStep(2);
            } else {
                showStep(3);
            }
        } else if (currentStep === 2) {
            showStep(3);
        }
    });

    // Handle "Back" button clicks
    prevBtn.addEventListener("click", () => {
        if (currentStep === 3) {
            const isAttending = document.querySelector('input[name="Input.Attending"]:checked')?.value === "true";
            showStep(isAttending ? 2 : 1);
        } else if (currentStep === 2) {
            showStep(1);
        }
    });

    // Reset to step 1 if Attendance selection changes
    document.querySelectorAll('input[name="Input.Attending"]').forEach(radio => {
        radio.addEventListener("change", () => {
            if (radio.value === "false") {
                nextBtn.innerText = "Finalize & Submit";
                stepperProgress.classList.add("d-none");
            } else {
                nextBtn.innerText = "Next";
                stepperProgress.classList.remove("d-none");
            }
        });
    });

})();
