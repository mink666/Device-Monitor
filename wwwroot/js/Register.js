    async function register() {
        const email = document.getElementById("email").value.trim();
    const username = document.getElementById("username").value.trim();

    if (!email || !username) {
        showError("All fields are required.");
    return;
        }

    if (!validateEmail(email)) {
        showError("Invalid email format.");
    return;
        }

    const payload = {email, username};

    try {
            const response = await fetch('/api/auth/register', {
        method: 'POST',
    headers: {'Content-Type': 'application/json' },
    body: JSON.stringify(payload)
            });

    const data = await response.json();

    if (!response.ok) {
                if (data.errors) {
        let errorMessages = data.errors.map(e => e.description).join("\n");
    showError(errorMessages);
                } else {
        showError(data.message || "Registration failed.");
                }
    return;
            }

    alert(data.message);
    window.location.href = '/Account/Login';
        } catch (error) {
        console.error("❌ Registration Error:", error);
    showError("Network error. Try again.");
        }
    }

    function validateEmail(email) {
        const re = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    return re.test(email);
    }

    function showError(message) {
        const errorMessageDiv = document.getElementById("errorMessage");
    errorMessageDiv.textContent = message;
    errorMessageDiv.classList.remove("d-none");
    }
