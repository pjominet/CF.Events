(function() {
    const options = document.getElementById("additionalOptions");
    document.querySelectorAll('input[name="Input.Attending"]').forEach(function (radio) {
        radio.addEventListener("change", function () {
            options.style.display = (this.value === "true" || this.value === "True") ? "block" : "none";
            console.log(options.style.display);
        });
    });
})();
