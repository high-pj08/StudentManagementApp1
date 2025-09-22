// Function to handle show/hide password toggle
function setupPasswordToggle() {
    document.querySelectorAll(".toggle-password").forEach(function (toggle) {
        toggle.addEventListener("click", function () {
            // Get the target input ID from the 'toggle' attribute
            var passwordInputId = this.getAttribute("toggle");
            var passwordInput = document.querySelector(passwordInputId);

            if (passwordInput.getAttribute("type") === "password") {
                passwordInput.setAttribute("type", "text");
                this.classList.remove("fa-eye");
                this.classList.add("fa-eye-slash");
            } else {
                passwordInput.setAttribute("type", "password");
                this.classList.remove("fa-eye-slash");
                this.classList.add("fa-eye");
            }
        });
    });
}

// Call the function when the DOM is fully loaded
document.addEventListener("DOMContentLoaded", function () {
    setupPasswordToggle();
});

// If you have existing JS in site.js, just append this new function and its call.
// For example, if you already have a DOMContentLoaded listener, just add setupPasswordToggle(); inside it.
