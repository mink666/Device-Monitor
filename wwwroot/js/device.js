let selectedRow = null;
const apiBaseUrl = "/api/Device"; // Centralize API base URL

// function for health status
function getRowClassForHealthJS(statusString) {
    if (statusString === 'Healthy') return 'table-success';
    if (statusString === 'Warning') return 'table-warning';
    if (statusString === 'Unreachable') return 'table-danger';
    return '';
}

function getBootstrapBadgeClassJS(statusString) {
    if (statusString === 'Healthy') return 'bg-success';
    if (statusString === 'Warning') return 'bg-warning text-dark';
    if (statusString === 'Unreachable') return 'bg-danger';
    return 'bg-secondary';
}

function truncateTextJS(text, maxLength) {
    if (!text || text.length <= maxLength) return text || "";
    return text.substring(0, maxLength) + "...";
}
// --- Formatting Helper Functions ---
function formatUptimeJS(totalSecondsStr, status) {
    const totalSeconds = parseInt(totalSecondsStr, 10);
    if (isNaN(totalSeconds) || totalSeconds < 0) return "N/A";
    if (totalSeconds === 0 && status === "Online") return "< 1 min";
    if (totalSeconds === 0) return "0s";

    const days = Math.floor(totalSeconds / (3600 * 24));
    const hrs = Math.floor((totalSeconds % (3600 * 24)) / 3600);
    const mins = Math.floor((totalSeconds % 3600) / 60);
    const secs = Math.floor(totalSeconds % 60);
    return `${days}d ${hrs}h ${mins}m ${secs}s`;
}

function formatCpuJS(cpuLoad) {
    if (typeof cpuLoad === 'number') {
        return `${cpuLoad.toFixed(2)}%`;
    }
    return "N/A";
}

function formatStorageJS(usedKBytes, totalKBytes, percentage) {
    if (typeof totalKBytes !== 'number' || totalKBytes <= 0) {
        return (typeof percentage === 'number') ? `${percentage.toFixed(2)}%` : "N/A";
    }
    if (typeof usedKBytes !== 'number' && typeof percentage === 'number') {
        usedKBytes = totalKBytes * (percentage / 100);
    }
    if (typeof usedKBytes !== 'number') {
        return (typeof percentage === 'number') ? `${percentage.toFixed(2)}%` : "N/A";
    }

    let cUsed = usedKBytes;
    let usedUnit = "KB";
    let cTotal = totalKBytes;
    let totalUnit = "KB";

    if (cUsed >= 1024 * 1024) { cUsed /= (1024 * 1024); usedUnit = "GB"; }
    else if (cUsed >= 1024) { cUsed /= 1024; usedUnit = "MB"; }

    if (cTotal >= 1024 * 1024) { cTotal /= (1024 * 1024); totalUnit = "GB"; }
    else if (cTotal >= 1024) { cTotal /= 1024; totalUnit = "MB"; }

    const percentageString = (typeof percentage === 'number') ? ` (${percentage.toFixed(2)}%)` : "";

    if (usedUnit === totalUnit) {
        return `${cUsed.toFixed(2)} / ${cTotal.toFixed(2)} ${totalUnit}${percentageString}`;
    } else {
        return `${cUsed.toFixed(2)} ${usedUnit} / ${cTotal.toFixed(2)} ${totalUnit}${percentageString}`;
    }
}

// --- Context Menu ---
function showContextMenu(e, row) { // Changed 'event' to 'e'
    e.preventDefault(); 
    selectedRow = row;
    var deviceId = row.dataset.id;

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

// --- Device Table Population ---
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
    row.setAttribute("data-osversion", device.osVersion || "N/A");
    row.setAttribute("data-cpu-load", device.latestCpuLoadPercentage || "");
    row.setAttribute("data-mem-usage-percent", device.latestMemoryUsagePercentage || "");
    row.setAttribute("data-disk-usage-percent", device.latestDiskUsagePercentage || "");
    row.setAttribute("data-total-ram-kb", device.latestTotalRamKBytes || "");
    row.setAttribute("data-used-ram-kb", device.latestUsedRamKBytes || "");
    row.setAttribute("data-total-disk-kb", device.latestTotalDiskKBytes || "");
    row.setAttribute("data-used-disk-kb", device.latestUsedDiskKBytes || "");
    row.setAttribute("data-sysuptime", device.latestSysUpTimeSeconds || "");
    row.setAttribute("data-lastcheck", device.lastCheckTimestamp ? new Date(device.lastCheckTimestamp).toLocaleString() : "N/A");

    //Health status attributes
    row.setAttribute("data-health-status", device.healthStatus || "Unknown");
    row.setAttribute("data-health-reason", device.healthStatusReason || "");

    row.className = getRowClassForHealthJS(device.healthStatus);

    row.oncontextmenu = function (e) { return showContextMenu(e, this); };

    let lastCheckDisplay = device.lastCheckTimestamp ? new Date(device.lastCheckTimestamp).toLocaleString() : "N/A";
    const healthReasonHtml = device.healthStatusReason
        ? `<small class="d-block text-muted" title="${device.healthStatusReason}">${truncateTextJS(device.healthStatusReason, 50)}</small>`
        : "";

    row.innerHTML = `
        <td>${device.name}</td>
        <td>${device.ipAddress}</td>
        <td>${device.port}</td>
        <td>${device.isEnabled ? 'Enabled' : 'Disabled'}</td>
        <td>${device.lastStatus || "Unknown"}</td>
        <td>
            <span class="badge ${getBootstrapBadgeClassJS(device.healthStatus)}">
                ${device.healthStatus || "Unknown"}
            </span>
            ${healthReasonHtml}
        </td>
        <td>${formatCpuJS(device.latestCpuLoadPercentage)}</td>
        <td>${formatStorageJS(device.latestUsedRamKBytes, device.latestTotalRamKBytes, device.latestMemoryUsagePercentage)}</td>
        <td>${formatStorageJS(device.latestUsedDiskKBytes, device.latestTotalDiskKBytes, device.latestDiskUsagePercentage)}</td>        
        <td>${formatUptimeJS(device.latestSysUpTimeSeconds)}</td>
        <td>${lastCheckDisplay}</td>
    `;
    return row;
}
function updateDeviceTable(devices) {
    const deviceListTableBody = document.getElementById("deviceList");
    if (!deviceListTableBody) {
        console.error("Element with ID 'deviceList' not found.");
        return;
    }
    deviceListTableBody.innerHTML = ""; // Clear existing rows

    if (devices && devices.length > 0) {
        devices.forEach(device => {
            const newRow = createDeviceRow(device);
            deviceListTableBody.appendChild(newRow);
        });
    } else {
        const colCount = document.querySelector("#deviceList").closest("table").querySelectorAll("thead th").length || 11; // Adjust to new column count
        deviceListTableBody.innerHTML = `<tr><td colspan="${colCount}" class="text-center">No devices found.</td></tr>`;
    }
}

// Function to fetch all devices and render them in the table
async function fetchAndRenderDevices() {
    try {
        const response = await fetch(apiBaseUrl);
        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }
        const devices = await response.json();
        updateDeviceTable(devices); 
    } catch (error) {
        console.error("Error fetching devices:", error);
        const deviceListTableBody = document.getElementById("deviceList");
        if (deviceListTableBody) {
            const colCount = document.querySelector("#deviceList").closest("table").querySelectorAll("thead th").length || 11;
            deviceListTableBody.innerHTML = `<tr><td colspan="${colCount}" class="text-center text-danger">Failed to load devices.</td></tr>`;
        }
    }
}

// --- Document Ready & Event Listeners ---
document.addEventListener("DOMContentLoaded", function () {
    fetchAndRenderDevices(); // Load devices when the page is ready

    const refreshButton = document.getElementById("refreshDeviceListButton");
        refreshButton.addEventListener("click", function () {

            const originalHtml = this.innerHTML;
            this.disabled = true;
            this.innerHTML = '<span class="spinner-border spinner-border-sm" role="status" aria-hidden="true"></span> Refreshing...';

            fetchAndRenderDevices() // This is your existing function
                .catch(error => {
                    alert("Failed to refresh device data. Please try again.");
                })
                .finally(() => {
                    this.disabled = false;
                    this.innerHTML = originalHtml;
                });
        });

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
                IsEnabled: document.getElementById("deviceIsEnabled").value === 'true',
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
                    fetchAndRenderDevices();
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