let selectedRow = null;

document.addEventListener("DOMContentLoaded", function () {
    // Add Device Form
    document.getElementById("addDeviceForm").addEventListener("submit", async function (e) {
        e.preventDefault();

        const device = {
            Name: document.getElementById("deviceName").value.trim(),
            Username: document.getElementById("deviceUsername").value.trim(),
            PlaintextPassword: document.getElementById("devicePassword").value.trim(),
            IPAddress: document.getElementById("deviceIP").value.trim(),
            Port: parseInt(document.getElementById("devicePort").value) || null,
            Status: document.getElementById("deviceStatus").value.trim()
        };

        try {
            const response = await fetch("/Device/Create", {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify(device)
            });
            const result = await response.json();
            if (result.success) {
                alert("Device created successfully!");
                location.reload();  
            } else {
                alert("Error: " + result.message);
            }
        } catch (error) {
            console.error("Error:", error);
            alert("Failed to register device.");
        }
    });
});



    // Edit Device Form
const editForm = document.getElementById("editDeviceForm");

if (editForm) {
    editForm.addEventListener("submit", async function (e) {
        e.preventDefault();

        const id = document.getElementById("editDeviceId").value.trim();
        const updatedDevice = {
            Name: document.getElementById("editDeviceName").value.trim(),
            IPAddress: document.getElementById("editDeviceIP").value.trim(),
            Status: document.getElementById("editDeviceStatus").value.trim()
        };
        try {
            const response = await fetch(`/Device/Edit/${id}`, {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify(updatedDevice)
            });

            const responseText = await response.text();

            if (!response.ok) {
                throw new Error(`Server Error (${response.status}): ${responseText}`);
            }

            const result = JSON.parse(responseText); 
            if (result.success) {
                alert("✅ Device updated successfully!");
                location.reload();
            } else {
                alert("❌ Error updating device. Check console for details.");
                console.error("❌ Error updating device:", result.errors);
            }
        } catch (error) {
            console.error("❌ Fetch Error:", error);
            alert("❌ Error updating device. Check console for details.");
        }
    });
}


    // Populate Edit Modal
    document.getElementById('editDeviceModal').addEventListener('show.bs.modal', function (event) {
        if (selectedRow) {
            document.getElementById('editDeviceId').value = selectedRow.dataset.id;
            document.getElementById('editDeviceName').value = selectedRow.dataset.name;
            document.getElementById('editDeviceIP').value = selectedRow.dataset.ip;
            document.getElementById('editDeviceStatus').value = selectedRow.dataset.status;
        }
    });

//Update device table
function updateDeviceTable(devices) {
    const deviceList = document.getElementById("deviceList");
    deviceList.innerHTML = "";

    devices.forEach(device => {
        const row = document.createElement("tr");
        row.setAttribute("data-id", device.id);
        row.setAttribute("data-name", device.name);
        row.setAttribute("data-ip", device.ipAddress);
        row.setAttribute("data-status", device.status);
        row.oncontextmenu = function (e) { return showContextMenu(e, this); };

        row.innerHTML = `
                <td>${device.id}</td>
                <td>${device.name}</td>
                <td>${device.ipAddress}</td>
                <td>${device.status}</td>
            `;
        deviceList.appendChild(row);
    });
}

//Show context menu onclick
function showContextMenu(event, row) {
    event.preventDefault();
    selectedRow = row;
    var deviceId = row.dataset.id;

    document.getElementById('editLink').href = '/Device/Edit/' + deviceId;
    document.getElementById('deleteLink').href = '/Device/Delete/' + deviceId;

    var contextMenu = document.getElementById('contextMenu');
    contextMenu.style.left = event.pageX + 'px';
    contextMenu.style.top = event.pageY + 'px';
    contextMenu.style.display = 'block';

    return false;
}
//Hide context menu onlcik outside
window.addEventListener("click", function (event) {
    if (!event.target.closest("#contextMenu")) {
        document.getElementById("contextMenu").style.display = "none";
        selectedRow = null;
    }
});


//Delete Function
function confirmDelete() {
    var deleteLink = document.getElementById('deleteLink');
    var deviceId = deleteLink.getAttribute('href').split('/').pop();

    if (confirm("Are you sure you want to delete this device?")) {
        fetch(`/Device/Delete/${deviceId}`, {
            method: "POST",
            headers: { "Content-Type": "application/json" }
        })
            .then(response => response.json())
            .then(result => {
                if (result.success) {
                    alert("Device deleted successfully!");
                    location.reload();
                } else {
                    alert("Error deleting device.");
                    console.error("Error deleting device:", result.errors);
                }
            })
            .catch(error => {
                console.error("Error:", error);
                alert("Error deleting device. Check console for details.");
            });
    }
    return false; // Prevents the default navigation
}



