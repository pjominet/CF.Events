(function () {
    "use strict";

    const steps = document.querySelectorAll(".form-step");
    const indicators = document.querySelectorAll(".step-indicator");
    const stepperProgress = document.querySelector(".stepper-progress");
    const nextBtn = document.querySelector(".btn-next");
    const prevBtn = document.querySelector(".btn-prev");
    const submitBtn = document.querySelector(".btn-submit");
    const cancelLink = document.getElementById("cancelLink");

    // Shared functions
    window.rsvpShared = {
        initParticipantManagement: function (container = document, addBtnId = "add-participant", participantContainerId = "participant-container") {
            const participantContainer = container.getElementById ? container.getElementById(participantContainerId) : container.querySelector("#" + participantContainerId);
            const addParticipantBtn = container.getElementById ? container.getElementById(addBtnId) : container.querySelector("#" + addBtnId);

            if (!participantContainer) return;

            const update = () => this.updateParticipantSelections(container);

            addParticipantBtn?.addEventListener("click", () => {
                const maxParticipants = parseInt(participantContainer.getAttribute("data-max-participants")) || 2;
                const currentParticipants = participantContainer.querySelectorAll(".participant-row").length;

                if (currentParticipants >= maxParticipants) return;

                const index = currentParticipants;
                const row = document.createElement("div");
                row.className = "row g-2 mb-2 participant-row";
                row.innerHTML = `
                    <div class="col">
                        <input name="NewRsvp.Participants[${index}]" class="form-control participant-input" placeholder="Participant Name" required />
                    </div>
                    <div class="col-auto">
                        <button type="button" class="btn btn-link text-danger remove-participant"><i class="bi bi-x-lg"></i></button>
                    </div>
                `;
                participantContainer.appendChild(row);

                if (participantContainer.querySelectorAll(".participant-row").length >= maxParticipants) {
                    addParticipantBtn.classList.add("d-none");
                }

                row.querySelector(".remove-participant").addEventListener("click", () => {
                    row.remove();
                    if (participantContainer.querySelectorAll(".participant-row").length < maxParticipants) {
                        addParticipantBtn.classList.remove("d-none");
                    }
                    update();
                });
            });

            participantContainer.addEventListener("click", (e) => {
                if (e.target.closest(".remove-participant")) {
                    const maxParticipants = parseInt(participantContainer.getAttribute("data-max-participants")) || 4;
                    e.target.closest(".participant-row").remove();
                    if (participantContainer.querySelectorAll(".participant-row").length < maxParticipants) {
                        addParticipantBtn?.classList.remove("d-none");
                    }
                    update();
                }
            });

            participantContainer.addEventListener("input", (e) => {
                if (e.target.classList.contains("participant-input")) {
                    update();
                }
            });
        },

        updateParticipantSelections: function (container = document) {
            const participants = Array.from(container.querySelectorAll(".participant-input")).map(i => i.value).filter(v => v);

            // Update attendance selections
            container.querySelectorAll("select.participant-day-select").forEach(select => {
                let currentSelected = [];
                if (select.tomselect) {
                    currentSelected = select.tomselect.getValue();
                    if (!Array.isArray(currentSelected)) {
                        currentSelected = currentSelected ? [currentSelected] : [];
                    }
                } else if (select.selectedOptions) {
                    currentSelected = Array.from(select.selectedOptions).map(o => o.value);
                }

                if (select.tomselect) {
                    select.tomselect.clearOptions();
                    participants.forEach(p => {
                        select.tomselect.addOption({value: p, text: p});
                        if (currentSelected.includes(p)) {
                            select.tomselect.addItem(p);
                        }
                    });
                } else {
                    select.innerHTML = "";
                    participants.forEach(p => {
                        const option = new Option(p, p);
                        option.selected = currentSelected.includes(p);
                        if (typeof select.add === "function") {
                            select.add(option);
                        } else {
                            select.appendChild(option);
                        }
                    });
                }
            });

            // Update dietary sections
            const dietaryContainer = container.getElementById ? container.getElementById("dietary-participants-container") : container.querySelector("#dietary-participants-container");
            if (dietaryContainer) {
                const currentDietary = Array.from(dietaryContainer.querySelectorAll(".dietary-participant-row")).map(row => {
                    const select = row.querySelector("select.dietary-options-select");
                    let options = [];
                    if (select) {
                        if (select.tomselect) {
                            options = select.tomselect.getValue();
                            if (!Array.isArray(options)) {
                                options = options ? [options] : [];
                            }
                        } else if (select.selectedOptions) {
                            options = Array.from(select.selectedOptions).map(o => o.value);
                        }
                    }
                    return {
                        name: row.querySelector(".participant-name-label").textContent,
                        options: options,
                        other: row.querySelector("textarea") ? row.querySelector("textarea").value : ""
                    };
                });

                dietaryContainer.innerHTML = "";
                participants.forEach((p, pIndex) => {
                    const existing = currentDietary.find(d => d.name === p);
                    const row = document.createElement("div");
                    row.className = "mb-3 p-3 border rounded bg-white dietary-participant-row";
                    row.innerHTML = `
                        <div class="d-flex justify-content-between align-items-center">
                            <strong class="participant-name-label">${p}</strong>
                            <input type="hidden" name="NewRsvp.ParticipantsDiets[${pIndex}].ParticipantName" value="${p}" />
                            <div class="form-check form-switch">
                                <input class="form-check-input has-dietary-switch" type="checkbox" id="hasDietary_${pIndex}" ${existing && (existing.options.length > 0 || existing.other) ? "checked" : ""}>
                                <label class="form-check-label" for="hasDietary_${pIndex}">Dietary Needs</label>
                            </div>
                        </div>
                        <div class="dietary-options-wrapper mt-2 ${existing && (existing.options.length > 0 || existing.other) ? "" : "d-none"}">
                            <select name="NewRsvp.ParticipantsDiets[${pIndex}].Restrictions" class="form-select tom-select dietary-options-select" multiple data-placeholder="Select options...">
                                ${document.getElementById("dietary-options-template").innerHTML}
                            </select>
                            <textarea name="NewRsvp.ParticipantsDiets[${pIndex}].OtherDetails" class="form-control mt-2" maxlength="500" placeholder="Other dietary details or allergies..." rows="2">${existing ? existing.other : ""}</textarea>
                        </div>
                    `;
                    dietaryContainer.appendChild(row);

                    const select = row.querySelector(".dietary-options-select");
                    if (existing) {
                        Array.from(select.options).forEach(o => o.selected = existing.options.includes(o.value));
                    }

                    row.querySelector(".has-dietary-switch").addEventListener("change", (e) => {
                        row.querySelector(".dietary-options-wrapper").classList.toggle("d-none", !e.target.checked);
                        if (!e.target.checked) {
                            if (select.tomselect) select.tomselect.clear();
                            row.querySelector("textarea").value = "";
                        }
                    });
                });

                // Re-init multi-selects
                window?.initMultiSelects(dietaryContainer);
            }
        },

        initDayCheckboxes: function (container = document) {
            container.querySelectorAll(".day-checkbox").forEach(checkbox => {
                checkbox.addEventListener("change", function () {
                    const day = this.getAttribute("data-day");
                    const select = container.querySelector(`.participant-day-select[data-day="${day}"]`);
                    if (select) {
                        if (select.tomselect) {
                            if (this.checked)
                                select.tomselect.enable();
                            else {
                                select.tomselect.clear();
                                select.tomselect.disable();
                            }
                        }
                    }
                });
            });
        },

        initDietarySwitches: function (container = document) {
            container.querySelectorAll(".has-dietary-switch").forEach(sw => {
                sw.addEventListener("change", (e) => {
                    const row = e.target.closest(".dietary-participant-row");
                    if (!row) return;
                    const wrapper = row.querySelector(".dietary-options-wrapper");
                    if (wrapper) wrapper.classList.toggle("d-none", !e.target.checked);
                    if (!e.target.checked) {
                        const select = row.querySelector(".dietary-options-select");
                        if (select && select.tomselect) select.tomselect.clear();
                        const textarea = row.querySelector("textarea");
                        if (textarea) textarea.value = "";
                    }
                });
            });
        },

        prepareAttendanceInputs: function (form, hiddenContainer) {
            if (!hiddenContainer) return;
            hiddenContainer.innerHTML = "";
            const attendanceMap = new Map(); // participantName -> set of days

            form.querySelectorAll("select.participant-day-select").forEach(select => {
                if (select.disabled) return;
                const day = parseInt(select.getAttribute("data-day"));
                let selectedValues = [];
                if (select.tomselect) {
                    selectedValues = select.tomselect.getValue();
                    if (!Array.isArray(selectedValues)) {
                        selectedValues = selectedValues ? [selectedValues] : [];
                    }
                } else if (select.selectedOptions) {
                    selectedValues = Array.from(select.selectedOptions).map(o => o.value);
                }

                selectedValues.forEach(name => {
                    if (!attendanceMap.has(name)) attendanceMap.set(name, new Set());
                    attendanceMap.get(name).add(day);
                });
            });

            let idx = 0;
            attendanceMap.forEach((days, name) => {
                const nameInput = document.createElement("input");
                nameInput.type = "hidden";
                nameInput.name = `NewRsvp.ParticipantsAttendance[${idx}].ParticipantName`;
                nameInput.value = name;
                hiddenContainer.appendChild(nameInput);

                let dayIdx = 0;
                days.forEach(day => {
                    const dayInput = document.createElement("input");
                    dayInput.type = "hidden";
                    dayInput.name = `NewRsvp.ParticipantsAttendance[${idx}].AttendingDays[${dayIdx}]`;
                    dayInput.value = day;
                    hiddenContainer.appendChild(dayInput);
                    dayIdx++;
                });
                idx++;
            });
        }
    };

    // If the RSVP form is not present (already responded view), skip stepper setup
    if (!nextBtn || !prevBtn) return;

    window.rsvpShared.initParticipantManagement(document);

    // Step 1 = Attendance, Step 2 = Participants, steps 2-5 = stepper steps 1-4
    let currentStep = 1;

    function showStep(stepNumber) {
        // Show the correct form-step div
        steps.forEach((step, index) => {
            step.classList.toggle("d-none", index !== stepNumber - 1);
        });

        // Stepper indicators map to steps 2-5 (indices 0-3)
        indicators.forEach((ind, index) => {
            ind.classList.toggle("active", index === stepNumber - 2);
        });

        // Navigation button visibility
        prevBtn.classList.toggle("d-none", stepNumber === 1);
        cancelLink.classList.toggle("d-none", stepNumber !== 1);

        // Last step (5) shows submit, others show next
        if (stepNumber === 5) {
            nextBtn.classList.add("d-none");
            submitBtn.classList.remove("d-none");
        } else {
            nextBtn.classList.remove("d-none");
            submitBtn.classList.add("d-none");
        }

        // Show/hide stepper progress (only for attending flow, steps 2-5)
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

        // Prepare participants for subsequent steps
        if (stepNumber >= 3) {
            window.rsvpShared.updateParticipantSelections();
        }

        currentStep = stepNumber;
    }

    // Prepare hidden inputs for attendance before submit
    document.getElementById("rsvpForm")?.addEventListener("submit", function () {
        window.rsvpShared.prepareAttendanceInputs(this, document.getElementById("participant-attendance"));
    });

    // Handle "Next" button clicks
    nextBtn.addEventListener("click", () => {
        const isAttending = document.querySelector('input[name="NewRsvp.Attending"]:checked')?.value === "true";

        if (currentStep === 1) {
            if (isAttending) {
                showStep(2);
            } else {
                showStep(5);
            }
        } else if (currentStep === 2) {
            showStep(3);
        } else if (currentStep === 3) {
            showStep(4);
        } else if (currentStep === 4) {
            showStep(5);
        }
    });

    // Handle "Back" button clicks
    prevBtn.addEventListener("click", () => {
        if (currentStep === 5) {
            const isAttending = document.querySelector('input[name="NewRsvp.Attending"]:checked')?.value === "true";
            showStep(isAttending ? 4 : 1);
        } else if (currentStep === 4) {
            showStep(3);
        } else if (currentStep === 3) {
            showStep(2);
        } else if (currentStep === 2) {
            showStep(1);
        }
    });

    // Handle day checkbox changes
    window.rsvpShared.initDayCheckboxes();
})();
