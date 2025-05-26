let selectedRow = null;
const apiBaseUrl = "/api/Device"; // Centralize API base URL
function showContextMenu(e, row) { // Changed 'event' to 'e'
    console.log("showContextMenu CALLED. Event:", e, "Row:", row);
    e.preventDefault(); 
    selectedRow = row;
    var deviceId = row.dataset.id;
    console.log("Device ID from row.dataset.id:", deviceId);

    const editLinkEl = document.getElementById('editLink');
    const deleteLinkEl = document.getElementById('deleteLink');

    var contextMenu = document.getElementById('contextMenu');
    if (contextMenu) {
        contextMenu.style.left = e.pageX + 'px'; 
        contextMenu.style.top = e.pageY + 'px';
        contextMenu.style.display = 'block';
    } else {
        console.error("Context menu HTML element with ID 'contextMenu' NOT FOUND!");
    }

    return false;
}

// Function to fetch all devices and render them in the table
async function fetchAndRenderDevices() {
    try {
        const response = await fetch(apiBaseUrl);
        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }
        const devices = await response.json();
        updateDeviceTable(devices); // Use your existing function to populate
    } catch (error) {
        console.error("Error fetching devices:", error);
        alert("Failed to load devices. Please try refreshing the page.");
    }
}

// Function to create a single table row (helper)
function createDeviceRow(device) {
    const row = document.createElement("tr");
    // Ensure these data-* attributes match exactly what you have in Index.cshtml's loop
    row.setAttribute("data-id", device.id);
    row.setAttribute("data-name", device.name);
    row.setAttribute("data-ip", device.ipAddress);
    row.setAttribute("data-port", device.port);
    row.setAttribute("data-community", device.communityString || "");
    row.setAttribute("data-isenabled", device.isEnabled.toString().toLowerCase());
    row.setAttribute("data-pollinginterval", device.pollingIntervalSeconds);
    row.setAttribute("data-status", device.lastStatus || "Unknown");
    row.setAttribute("data-lastcheck", device.lastCheckTimestamp ? new Date(device.lastCheckTimestamp).toLocaleString() : "N/A");
    row.setAttribute("data-osversion", device.osVersion || "N/A");
    row.setAttribute("data-latest-sys-up-time-seconds", device.latestSysUpTimeSeconds || "N/A");

    row.oncontextmenu = function (e) { return showContextMenu(e, this); };

    let lastCheckDisplay = "N/A";
    if (device.lastCheckTimestamp) {
        try {
            lastCheckDisplay = new Date(device.lastCheckTimestamp).toLocaleString();
        } catch (e) {
            console.error("Error parsing lastCheckTimestamp:", e);
        }
    }
    function formatUptimeJS(totalSeconds) {
        if (totalSeconds === null || totalSeconds === undefined || totalSeconds < 0) return "N/A";
        if (totalSeconds === 0 && device.lastStatus === "Online") return "< 1 min";
        if (totalSeconds === 0) return "0s";
        const days = Math.floor(totalSeconds / (3600 * 24));
        totalSeconds %= (3600 * 24);
        const hrs = Math.floor(totalSeconds / 3600);
        totalSeconds %= 3600;
        const mins = Math.floor(totalSeconds / 60);
        const secs = Math.floor(totalSeconds % 60);
        return `${days}d ${hrs}h ${mins}m ${secs}s`;
    }

    row.innerHTML = `
        <td>${device.id}</td>
        <td>${device.name}</td>
        <td>${device.ipAddress}</td>
        <td>${device.port}</td>
        <td>${device.isEnabled ? 'Enabled' : 'Disabled'}</td>
        <td>${device.lastStatus || "Unknown"}</td>
        <td>${lastCheckDisplay}</td>
        <td>${device.osVersion || "N/A"}</td> 
        <td>${formatUptimeJS(device.latestSysUpTimeSeconds)}</td>
    `;
    return row;
}
function updateDeviceTable(devices) {
    const deviceListTableBody = document.getElementById("deviceList");
    deviceList.innerHTML = ""; // Clear existing rows

    if (devices && devices.length > 0) {
        devices.forEach(device => {
            const newRow = createDeviceRow(device);
            deviceListTableBody.appendChild(newRow);
        });
    } else {
        const colCount = 11; // Adjust if you have more/fewer columns
        deviceListTableBody.innerHTML = `<tr><td colspan="${colCount}" class="text-center">No devices found.</td></tr>`;
    }
}


document.addEventListener("DOMContentLoaded", function () {
    fetchAndRenderDevices(); // Load devices when the page is ready

    const addDeviceModalEl = document.getElementById('addDeviceModal');
    const addDeviceModalInstance = addDeviceModalEl ? new bootstrap.Modal(addDeviceModalEl) : null;
    const addDeviceForm = document.getElementById("addDeviceForm");

    if (addDeviceForm) {
        addDeviceForm.addEventListener("submit", async function (e) {
            e.preventDefault();

            const deviceDto = { // This object structure MUST match your DeviceCreateDto on the server
                Name: document.getElementById("deviceName").value.trim(),
                IPAddress: document.getElementById("deviceIP").value.trim(),
                Port: Number(document.getElementById("devicePort").value),
                CommunityString: document.getElementById("deviceCommunityString").value.trim(), 
                IsEnabled: document.getElementById("deviceIsEnabled").checked, 
                PollingIntervalSeconds: Number(document.getElementById("devicePollingInterval").value), 
            };

            try {
                const response = await fetch(apiBaseUrl, { 
                    method: "POST",
                    headers: { "Content-Type": "application/json" },
                    body: JSON.stringify(deviceDto)
                });

                if (response.ok) { // Standard way to check for success (HTTP 200-299)
                    const newDevice = await response.json(); // The controller returns the created device
                    alert("Device created successfully!");
                    const deviceListTableBody = document.getElementById("deviceList");
                    const newRow = createDeviceRow(newDevice);
                    deviceListTableBody.appendChild(newRow);

                    if (addDeviceModalInstance) addDeviceModalInstance.hide();
                    addDeviceForm.reset();
                } else {
                    // Try to get more detailed error from API response
                    const errorResult = await response.json().catch(() => ({ message: "An unknown error occurred. Status: " + response.status }));
                    alert("Error creating device: " + (errorResult.title || errorResult.message || "Please check your input."));
                    console.error("Error creating device:", errorResult);
                }
            } catch (error) {
                console.error("Fetch Error:", error);
                alert("Failed to create device. Network error or server issue.");
            }
        });
    }

    const editDeviceModalEl = document.getElementById('editDeviceModal');
    const editDeviceModalInstance = editDeviceModalEl ? new bootstrap.Modal(editDeviceModalEl) : null;
    const editDeviceForm = document.getElementById("editDeviceForm");

    if (editDeviceForm) {
        editDeviceForm.addEventListener("submit", async function (e) {
            e.preventDefault();

            const id = document.getElementById("editDeviceId").value.trim();
            const deviceDto = { // This object MUST match your DeviceEditDto on the server
                Name: document.getElementById("editDeviceName").value.trim(),
                IPAddress: document.getElementById("editDeviceIP").value.trim(), 
                Port: Number(document.getElementById("editDevicePort").value),
                CommunityString: document.getElementById("editDeviceCommunity").value.trim(), 
                IsEnabled: document.getElementById("editDeviceIsEnabled").value === 'true', // Convert select value to boolean
                PollingIntervalSeconds: Number(document.getElementById("editDevicePollingInterval").value),
            };

            console.log("Device DTO to be sent to server:", JSON.stringify(deviceDto, null, 2)); // Pretty print the DTO
            if (!deviceDto.IPAddress || !deviceDto.Port || !deviceDto.CommunityString) {
                alert("IP Address, Port, and Community String are required for editing.");
                return; // Stop submission
            }

            try {
                const response = await fetch(`${apiBaseUrl}/${id}`, { 
                    method: "PUT",
                    headers: { "Content-Type": "application/json" },
                    body: JSON.stringify(deviceDto)
                });

                if (response.ok) { // For PUT, 204 No Content is typical success
                    alert("✅ Device updated successfully!");
                    const rowToUpdate = document.querySelector(`#deviceList tr[data-id="${id}"]`);
                    if (rowToUpdate) {
                        const displayData = {
                            id: parseInt(id), name: deviceDto.Name, ipAddress: deviceDto.IPAddress, port: deviceDto.Port,
                            communityString: deviceDto.CommunityString, isEnabled: deviceDto.IsEnabled,
                            pollingIntervalSeconds: deviceDto.PollingIntervalSeconds,
                            osVersion: deviceDto.OSVersion,
                            lastStatus: selectedRow ? selectedRow.dataset.status : "Unknown", // Keep existing status from data-*
                            lastCheckTimestamp: selectedRow ? selectedRow.dataset.lastcheck : null
                        };
                        const newRowContent = createDeviceRow(displayData); // create a new row
                        rowToUpdate.parentNode.replaceChild(newRowContent, rowToUpdate); // replace old with new
                    } else {
                        fetchAndRenderDevices(); // Fallback: if row not found, just reload all
                    }
                    if (editDeviceModalInstance) editDeviceModalInstance.hide();
                } else {
                    const errorResult = await response.json().catch(() => ({ message: `Server Error (${response.status})` }));
                    alert("❌ Error updating device: " + (errorResult.title || errorResult.message));
                    console.error("❌ Error updating device:", errorResult);
                }
            } catch (error) {
                console.error("❌ Fetch Error:", error);
                alert("❌ Error updating device. Network error or server issue.");
            }
        });
    }

    // Populate Edit Modal (ensure data-* attributes in HTML are correct)
    if (editDeviceModalEl) {
        editDeviceModalEl.addEventListener('show.bs.modal', function (event) {
            if (selectedRow) {
                document.getElementById('editDeviceId').value = selectedRow.dataset.id || '';
                document.getElementById('editDeviceName').value = selectedRow.dataset.name || '';
                document.getElementById('editDeviceIP').value = selectedRow.dataset.ip || '';
                document.getElementById('editDevicePort').value = selectedRow.dataset.port || '';
                document.getElementById('editDeviceCommunity').value = selectedRow.dataset.community || '';
                document.getElementById('editDeviceIsEnabled').value = selectedRow.dataset.isenabled || 'true'; 
                document.getElementById('editDevicePollingInterval').value = selectedRow.dataset.pollinginterval || '';
            }
        });
    }
});

window.addEventListener("click", function (event) {
    const contextMenu = document.getElementById("contextMenu");
    // Check if contextMenu exists before trying to access its style
    if (contextMenu && !event.target.closest("#contextMenu") && !event.target.closest("tr[oncontextmenu]")) {
        contextMenu.style.display = "none";
        selectedRow = null;
    }
});


//Delete Function
async function confirmDelete() {
    if (!selectedRow) {
        alert("No device selected for deletion.");
        return false;
    }
    const deviceId = selectedRow.dataset.id;

    if (confirm(`Are you sure you want to delete device ID ${deviceId}?`)) {
        try {
            const response = await fetch(`${apiBaseUrl}/${deviceId}`, {
                method: "DELETE"
            });

            if (response.ok) { // For DELETE, 204 No Content is success
                alert("Device deleted successfully!");
                selectedRow.remove(); // selectedRow is the <tr> element
                selectedRow = null; // Clear selectedRow
            } else {
                const errorResult = await response.json().catch(() => ({ message: `Server Error (${response.status})` }));
                alert("Error deleting device: " + (errorResult.title || errorResult.message));
                console.error("Error deleting device:", errorResult);
            }
        } catch (error) {
            console.error("Fetch Error:", error);
            alert("Error deleting device. Network error or server issue.");
        }
    }
    const contextMenu = document.getElementById("contextMenu");
    if (contextMenu) contextMenu.style.display = "none"; 
    return false;
}