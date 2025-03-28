async function login() {
    const username = document.getElementById("username").value.trim();
    const password = document.getElementById("password").value.trim();

    if (!username || !password) {
        showError("Username and Password are required!");
        return;
    }

    const payload = { username, password };

    try {
        const response = await fetch('/api/auth/login', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(payload)
        });

        const data = await response.json();

        if (!response.ok) {
            showError(data.message || "Login failed.");
            return;
        }

        // ✅ Save user session (Optional)
        localStorage.setItem("username", data.username);

        // ✅ Redirect after login success
        alert(data.message);
        window.location.href = "/Home/Index";

    } catch (error) {
        console.error("❌ Login Error:", error);
        showError("Network error. Try again.");
    }
}

function showError(message) {
    const errorMessageDiv = document.getElementById("errorMessage");
    errorMessageDiv.textContent = message;
    errorMessageDiv.classList.remove("d-none");
}
