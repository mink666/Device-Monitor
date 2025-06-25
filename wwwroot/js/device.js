let selectedRow = null;
const apiBaseUrl = "/api/Device"; // Centralize API base URL
let allDevicesData = []; // Cache for holding the latest device data
let selectedDeviceId = null;
let messageBoxTimeout;

// function for health status
function getRowClassForHealthJS(statusString) {
    if (statusString === 'Healthy') return 'table-success';
    if (statusString === 'Warning') return 'table-warning';
    if (statusString === 'Unreachable') return 'table-danger';
    if (statusString === 'Unknown') return 'table-secondary';
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
// --- NEW: UI Rendering Functions ---

/**
 * Renders the device icons in the left panel.
 */
function renderDeviceIcons(devices) {
    const iconList = document.getElementById("device-icon-list");
    if (!iconList) return;

    iconList.innerHTML = ""; // Clear previous icons

    if (!devices || devices.length === 0) {
        iconList.innerHTML = '<p class="text-muted">No devices found.</p>';
        return;
    }

    devices.forEach(device => {
        try {
            const card = document.createElement("div");
            const statusClass = device.healthStatus ? getBootstrapBadgeClassJS(device.healthStatus) : 'bg-secondary';
            const displayName = device.name ? truncateTextJS(device.name, 20) : "Unnamed Device";

            //card.className = `device-card ${getBootstrapBadgeClassJS(device.healthStatus)}`;
            card.className = `device-card ${statusClass}`;
            card.dataset.id = device.id;

            // Add selected class if this device is the currently selected one
            if (device.id === selectedDeviceId) {
                card.classList.add('selected');
            }

            //card.innerHTML = `
            //    <i class="bi bi-pc-display"></i>
            //    <div class="device-card-name">${truncateTextJS(device.name, 20)}</div>
            //`;
            card.innerHTML = `<i class="bi bi-pc-display"></i><div class="device-card-name">${displayName}</div>`;

            card.addEventListener("click", () => {
                selectedDeviceId = device.id;
                displayDeviceDetails(device.id);

                // Update selection visual
                document.querySelectorAll('.device-card').forEach(c => c.classList.remove('selected'));
                card.classList.add('selected');
            });

            iconList.appendChild(card);
        } catch (err) {
            // If any error happens while creating a card, log it to the console.
            console.error("Failed to render a device card. The device data might be incomplete. Error:", err);
            console.error("Problematic Device Object:", device);
        }
    });
}
/**
 * Displays the details of a single device in the right panel.
 */
function displayDeviceDetails(deviceId) {
    const device = allDevicesData.find(d => d.id === deviceId);
    if (!device) {
        return;
    }

    // Hide placeholder, show content
    document.getElementById("details-placeholder").style.display = "none";
    document.getElementById("details-content").style.display = "block";

    // --- Populate Header ---
    document.getElementById("details-device-name").innerText = device.name;
    const badge = document.getElementById("details-health-status-badge");
    badge.className = `badge ${getBootstrapBadgeClassJS(device.healthStatus)}`;
    badge.innerText = device.healthStatus;

    // --- Populate Info Table ---
    const infoTable = document.getElementById("details-info-table");
    infoTable.innerHTML = `
        <tr><th scope="row">IP Address</th><td>${device.ipAddress}</td></tr>
        <tr><th scope="row">Port</th><td>${device.port}</td></tr>
        <tr><th scope="row">Monitoring</th><td>${device.isEnabled ? 'Enabled' : 'Disabled'}</td></tr>
        <tr><th scope="row">Polling Status</th><td>${device.lastStatus || "Unknown"}</td></tr>
        <tr><th scope="row">Uptime</th><td>${formatUptimeJS(device.latestSysUpTimeSeconds)}</td></tr>
        <tr><th scope="row">Last Check</th><td>${device.lastCheckTimestamp ? new Date(device.lastCheckTimestamp).toLocaleString() : "N/A"}</td></tr>
        <tr><th scope="row">Health Reason</th><td>${device.healthStatusReason || "N/A"}</td></tr>
    `;

    // --- Update Metric Graphs ---
    // CPU
    const cpuBar = document.getElementById("details-cpu-bar");
    const cpuPercent = device.latestCpuLoadPercentage || 0;
    cpuBar.style.width = `${cpuPercent.toFixed(2)}%`;
    cpuBar.setAttribute('aria-valuenow', cpuPercent.toFixed(2));
    cpuBar.innerText = `${cpuPercent.toFixed(2)}%`;
    document.getElementById("details-cpu-label").innerText = `${cpuPercent.toFixed(2)}%`;

    // RAM
    const ramBar = document.getElementById("details-ram-bar");
    const ramPercent = device.latestMemoryUsagePercentage || 0;
    ramBar.style.width = `${ramPercent.toFixed(2)}%`;
    ramBar.setAttribute('aria-valuenow', ramPercent.toFixed(2));
    ramBar.innerText = `${ramPercent.toFixed(2)}%`;
    document.getElementById("details-ram-label").innerText = formatStorageJS(device.latestUsedRamKBytes, device.latestTotalRamKBytes, device.latestMemoryUsagePercentage);

    // Handle Disk C
    const diskCBar = document.getElementById('details-disk-c-bar');
    const diskCLabel = document.getElementById('details-disk-c-label');
    if (device.latestDiskCUsagePercentage != null) {
        const percent = device.latestDiskCUsagePercentage;
        diskCBar.style.width = `${percent.toFixed(2)}%`;
        diskCBar.textContent = `${percent.toFixed(2)}%`;
        diskCLabel.textContent = formatStorageJS(device.latestUsedDiskCKBytes, device.latestTotalDiskCKBytes, percent);
    } else {
        diskCBar.style.width = '0%';
        diskCBar.textContent = 'N/A';
        diskCLabel.textContent = 'N/A';
    }

    // Handle Disk D
    const diskDBar = document.getElementById('details-disk-d-bar');
    const diskDLabel = document.getElementById('details-disk-d-label');
    if (device.latestDiskDUsagePercentage != null) {
        const percent = device.latestDiskDUsagePercentage;
        diskDBar.style.width = `${percent.toFixed(2)}%`;
        diskDBar.textContent = `${percent.toFixed(2)}%`;
        diskDLabel.textContent = formatStorageJS(device.latestUsedDiskDKBytes, device.latestTotalDiskDKBytes, percent);
    } else {
        diskDBar.style.width = '0%';
        diskDBar.textContent = 'N/A';
        diskDLabel.textContent = 'N/A';
    }

    // Handle Disk E
    const diskEBar = document.getElementById('details-disk-e-bar');
    const diskELabel = document.getElementById('details-disk-e-label');
    if (device.latestDiskEUsagePercentage != null) {
        const percent = device.latestDiskEUsagePercentage;
        diskEBar.style.width = `${percent.toFixed(2)}%`;
        diskEBar.textContent = `${percent.toFixed(2)}%`;
        diskELabel.textContent = formatStorageJS(device.latestUsedDiskEKBytes, device.latestTotalDiskEKBytes, percent);
    } else {
        diskEBar.style.width = '0%';
        diskEBar.textContent = 'N/A';
        diskELabel.textContent = 'N/A';
    }

    // --- Update Generate Report Button ---
    const reportButton = document.getElementById('generate-device-summary-button');
    reportButton.dataset.deviceId = device.id; // Store device ID for the action
    reportButton.dataset.deviceName = device.name; // Store name for the report title
}
//Message box function
function showMessage(message, type = 'success') {
    const messageBox = document.getElementById('customMessageBox');
    if (!messageBox) return;

    // Clear any existing timer to prevent premature hiding
    clearTimeout(messageBoxTimeout);

    // Set the message and style
    messageBox.textContent = message;
    messageBox.className = 'message-box'; // Reset classes
    messageBox.classList.add(type === 'success' ? 'message-box-success' : 'message-box-error');

    // Show the message box
    messageBox.style.display = 'block';

    // Set a timer to automatically hide the message box after 3 seconds
    messageBoxTimeout = setTimeout(() => {
        messageBox.style.display = 'none';
    }, 3000); // 3000 milliseconds = 3 seconds
}
//Clear modal function
function cleanupModalEffects() {
    const backdrops = document.querySelectorAll('.modal-backdrop');
    backdrops.forEach(backdrop => {
        backdrop.remove();
    });

    document.body.classList.remove('modal-open');
    document.body.style.overflow = 'auto';
    document.body.style.paddingRight = '';
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
    row.setAttribute("data-cpuwarningthreshold", device.cpuWarningThreshold || "");
    row.setAttribute("data-ramwarningthreshold", device.ramWarningThreshold || "");
    row.setAttribute("data-diskwarningthreshold", device.diskWarningThreshold || "");

    row.className = getRowClassForHealthJS(device.healthStatus);

    row.oncontextmenu = function (e) { return showContextMenu(e, this); };

    let lastCheckDisplay = device.lastCheckTimestamp ? new Date(device.lastCheckTimestamp).toLocaleString() : "N/A";
    const healthReasonHtml = device.healthStatusReason
        ? `<small class="d-block text-muted" title="${device.healthStatusReason}">${truncateTextJS(device.healthStatusReason, 50)}</small>`
        : "";

    row.innerHTML = `
    <td data-label="Name">${device.name}</td>
    <td data-label="Active">${device.isEnabled ? 'Enabled' : 'Disabled'}</td>
    <td data-label="Polling">${device.lastStatus || "Unknown"}</td>
    <td data-label="Health Status">
        <span class="badge ${getBootstrapBadgeClassJS(device.healthStatus)}">
            ${device.healthStatus || "Unknown"}
        </span>
        ${healthReasonHtml}
    </td>
    <td data-label="CPU">${formatCpuJS(device.latestCpuLoadPercentage)}</td>
    <td data-label="RAM">${formatStorageJS(device.latestUsedRamKBytes, device.latestTotalRamKBytes, device.latestMemoryUsagePercentage)}</td>
    <td data-label="Disk C">${formatStorageJS(device.latestUsedDiskCKBytes, device.latestTotalDiskCKBytes, device.latestDiskCUsagePercentage)}</td>
    <td data-label="Disk D">${formatStorageJS(device.latestUsedDiskDKBytes, device.latestTotalDiskDKBytes, device.latestDiskDUsagePercentage)}</td>
    <td data-label="Disk E">${formatStorageJS(device.latestUsedDiskEKBytes, device.latestTotalDiskEKBytes, device.latestDiskEUsagePercentage)}</td>
    <td data-label="Uptime" class="col-uptime">${formatUptimeJS(device.latestSysUpTimeSeconds)}</td>
    <td data-label="Last Check" class="col-last-check">${lastCheckDisplay}</td>
    `;
    return row;
}
function updateDeviceTable(devices) {
    const deviceListTableBody = document.getElementById("deviceList");
    if (!deviceListTableBody) return;
    deviceListTableBody.innerHTML = "";

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
    document.getElementById('device-list-loader')?.remove();
    try {
        const response = await fetch(apiBaseUrl);
        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }
        const devices = await response.json();
        allDevicesData = devices; // Cache the data

        renderDeviceIcons(devices);
        updateDeviceTable(devices); 
        if (selectedDeviceId) {
            displayDeviceDetails(selectedDeviceId);
        }
    } catch (error) {
        console.error("Error fetching devices:", error);
        document.getElementById("device-icon-list").innerHTML = `<p class="text-danger">Failed to load devices.</p>`;
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
    const autoRefreshInterval = 60000; // 60,000 milliseconds = 60 seconds

    setInterval(() => {
        fetchAndRenderDevices();
    }, autoRefreshInterval);

    const searchInput = document.getElementById('deviceSearchInput');
    if (searchInput) {
        searchInput.addEventListener('input', () => {
            const searchTerm = searchInput.value.toLowerCase().trim();

            // Filter the cached device data based on the search term
            const filteredDevices = allDevicesData.filter(device => {
                return device.name.toLowerCase().includes(searchTerm);
            });

            // Re-render the icon list with only the filtered devices
            renderDeviceIcons(filteredDevices);
        });
    }

    const refreshButton = document.getElementById("refreshDeviceListButton");
        refreshButton.addEventListener("click", function () {

            const originalHtml = this.innerHTML;
            this.disabled = true;
            this.innerHTML = '<span class="spinner-border spinner-border-sm" role="status" aria-hidden="true"></span> Refreshing...';

            fetchAndRenderDevices() // This is your existing function
                .catch(error => {
                    showMessage("❌Failed to refresh device data. Please try again.", 'error');
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

                if (response.ok) { 
                    //const newDevice = await response.json(); // The controller returns the created device
                    showMessage("✅Device created successfully!",'success');
                    //const deviceListTableBody = document.getElementById("deviceList");
                    //const newRow = createDeviceRow(newDevice);
                    //deviceListTableBody.appendChild(newRow);

                    addDeviceForm.reset();

                    addDeviceModalInstance.hide();

                    setTimeout(() => {
                        cleanupModalEffects();
                        fetchAndRenderDevices();
                    }, 500);
                    
                } else {
                    // Try to get more detailed error from API response
                    const errorResult = await response.json().catch(() => ({ message: "An unknown error occurred. Status: " + response.status }));
                    showMessage("❌Error creating device: " + (errorResult.title || errorResult.message || "Please check your input."),'error');
                }
            } catch (error) {
                showMessage("❌Failed to create device. Network error or server issue.",'error');
            } finally {
                if (addDeviceModalInstance) addDeviceModalInstance.hide();
            }
        });

        // --- START: SignalR Connection for Live Notifications ---
        const connection = new signalR.HubConnectionBuilder()
            .withUrl("/notificationHub")
            .withAutomaticReconnect()
            .build();

        // This function is called by the server whenever it sends a "ReceiveWarning" message
        connection.on("ReceiveWarning", (deviceName, reason) => {
            // Use our existing message box to display the notification!
            showMessage(`WARNING for ${deviceName}: ${reason}`, 'error');

            // Also refresh the main UI to show the new status in the lists
            fetchAndRenderDevices();
        });

        // Function to start the connection, with retry logic
        async function startSignalR() {
            try {
                await connection.start();
                console.log("SignalR Connected.");
            } catch (err) {
                console.error("SignalR Connection Failed: ", err);
                setTimeout(startSignalR, 5000); // Retry connection after 5 seconds
            }
        };

        startSignalR(); // Start the connection when the page loads
    }

    const editDeviceModalEl = document.getElementById('editDeviceModal');
    const editDeviceModalInstance = editDeviceModalEl ? new bootstrap.Modal(editDeviceModalEl) : null;
    const editDeviceForm = document.getElementById("editDeviceForm");

    if (editDeviceForm) {
        editDeviceForm.addEventListener("submit", async function (e) {
            e.preventDefault();

            const id = document.getElementById("editDeviceId").value.trim();
            const cpuThresholdInput = document.getElementById("editCpuThreshold").value;
            const ramThresholdInput = document.getElementById("editRamThreshold").value;
            const diskThresholdInput = document.getElementById("editDiskThreshold").value;

            const deviceDto = { // This object MUST match your DeviceEditDto on the server
                Name: document.getElementById("editDeviceName").value.trim(),
                IPAddress: document.getElementById("editDeviceIP").value.trim(), 
                Port: Number(document.getElementById("editDevicePort").value),
                CommunityString: document.getElementById("editDeviceCommunity").value.trim(), 
                IsEnabled: document.getElementById("editDeviceIsEnabled").value === 'true', // Convert select value to boolean
                PollingIntervalSeconds: Number(document.getElementById("editDevicePollingInterval").value),
                CpuWarningThreshold: cpuThresholdInput ? Number(cpuThresholdInput) : null,
                RamWarningThreshold: ramThresholdInput ? Number(ramThresholdInput) : null,
                DiskWarningThreshold: diskThresholdInput ? Number(diskThresholdInput) : null,
            };

            if (!deviceDto.IPAddress || !deviceDto.Port || !deviceDto.CommunityString) {
                showMessage("❌IP Address, Port, and Community String are required for editing.",'error');
                return; // Stop submission
            }

            try {
                const response = await fetch(`${apiBaseUrl}/${id}`, { 
                    method: "PUT",
                    headers: { "Content-Type": "application/json" },
                    body: JSON.stringify(deviceDto)
                });

                if (response.ok) { // For PUT, 204 No Content is typical success
                    showMessage("✅ Device updated successfully!",'success');
                    fetchAndRenderDevices();
                    
                    editDeviceModalInstance.hide();

                    setTimeout(() => {
                        cleanupModalEffects(); // This is the function that fixes the scrollbar
                        fetchAndRenderDevices();
                    }, 500);
                } else {
                    const errorResult = await response.json().catch(() => ({ message: `Server Error (${response.status})` }));
                    showMessage("❌ Error updating device: " + (errorResult.title || errorResult.message),'error');
                }
            } catch (error) {
                showMessage("❌ Error updating device. Network error or server issue.",'error');
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
                document.getElementById('editCpuThreshold').value = selectedRow.dataset.cpuwarningthreshold || '';
                document.getElementById('editRamThreshold').value = selectedRow.dataset.ramwarningthreshold || '';
                document.getElementById('editDiskThreshold').value = selectedRow.dataset.diskwarningthreshold || '';
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
        showMessage("❌No device selected for deletion.",'error');
        return false;
    }
    const deviceId = selectedRow.dataset.id;

    if (confirm(`Are you sure you want to delete device ID ${deviceId}?`)) {
        try {
            const response = await fetch(`${apiBaseUrl}/${deviceId}`, {
                method: "DELETE"
            });

            if (response.ok) { // For DELETE, 204 No Content is success
                showMessage("✅Device deleted successfully!",'success');
                selectedRow.remove(); // selectedRow is the <tr> element
                selectedRow = null; // Clear selectedRow
                if (selectedDeviceId && selectedDeviceId == deviceId) {
                    selectedDeviceId = null;
                    document.getElementById('details-placeholder').style.display = 'block';
                    document.getElementById('details-content').style.display = 'none';
                }

                // Re-fetch all data and redraw the entire UI to reflect the deletion.
                await fetchAndRenderDevices();
            } else {
                const errorResult = await response.json().catch(() => ({ message: `Server Error (${response.status})` }));
                showMessage("❌Error deleting device: " + (errorResult.title || errorResult.message),'error');
            }
        } catch (error) {
            showMessage("❌Error deleting device. Network error or server issue.",'error');
        }
    }
    const contextMenu = document.getElementById("contextMenu");
    if (contextMenu) contextMenu.style.display = "none"; 
    return false;
}

//for reports
const generateReportButton = document.getElementById('generateReportButton');
const reportTitleInput = document.getElementById('reportTitleInput');

// This check tells you if the script found your HTML elements
if (generateReportButton && reportTitleInput) {
    generateReportButton.addEventListener('click', function () {

        const reportTitle = reportTitleInput.value.trim();

        if (!reportTitle) {
            showMessage('❌Please enter a report title.','error');
            reportTitleInput.focus();
            return;
        }

        const reportTab = window.open('', '_blank');
        if (!reportTab) {
            showMessage("❌Popup blocked! Please allow popups for this site.",'error');
            return;
        }

        reportTab.document.title = reportTitle;
        reportTab.document.body.innerHTML = "<h3>Generating your report, please wait...</h3>";

        const reportUrl = `/Reports/Generate?title=${encodeURIComponent(reportTitle)}`;
        reportTab.location.href = reportUrl;
    });
} else {
    console.error("ERROR: Could not find 'generateReportButton' or 'reportTitleInput'. Check the IDs in Index.cshtml.");
}

// --- START: Historical Report ---

// Get a reference to the new modal
const historyReportModalEl = document.getElementById('historyReportModal');
const historyReportModal = new bootstrap.Modal(historyReportModalEl);

// 1. Listener for the button in the RIGHT-SIDE DETAILS PANEL
const generateDeviceReportBtn = document.getElementById('generate-device-summary-button');
if (generateDeviceReportBtn) {
    generateDeviceReportBtn.addEventListener('click', function () {
        const deviceId = this.dataset.deviceId;
        const deviceName = this.dataset.deviceName;
        if (!deviceId) {
            showMessage('❌Please select a device first.','error');
            return;
        }

        // Pre-populate the modal form
        document.getElementById('historyReportDeviceId').value = deviceId;
        document.getElementById('historyReportTitle').value = `${deviceName} - Performance Report`;

        // Open the modal
        historyReportModal.show();
    });
}

// 2. Helper function to format date for datetime-local input
const toLocalISOString = (date) => {
    const tzoffset = (new Date()).getTimezoneOffset() * 60000; //offset in milliseconds
    const localISOTime = (new Date(date - tzoffset)).toISOString().slice(0, 16);
    return localISOTime;
};

// 3. Listeners for the "Now" buttons in the modal
document.getElementById('setStartDateToNow').addEventListener('click', () => {
    document.getElementById('historyReportStartDate').value = toLocalISOString(new Date());
});
document.getElementById('setEndDateToNow').addEventListener('click', () => {
    document.getElementById('historyReportEndDate').value = toLocalISOString(new Date());
});

// 4. Listener for the final "Generate Report" SUBMIT button inside the modal
document.getElementById('historyReportForm').addEventListener('submit', function (e) {
    e.preventDefault(); // Prevent default form submission

    const deviceId = document.getElementById('historyReportDeviceId').value;
    const title = document.getElementById('historyReportTitle').value;
    const startDate = document.getElementById('historyReportStartDate').value;
    const endDate = document.getElementById('historyReportEndDate').value;

    if (new Date(startDate) > new Date(endDate)) {
        alert('Start date cannot be after the end date.','error');
        return;
    }

    // Construct the URL for our new controller action
    const reportUrl = `/Reports/GenerateHistoryReport?deviceId=${deviceId}&title=${encodeURIComponent(title)}&startDate=${startDate}&endDate=${endDate}`;

    // Open the report in a new tab
    window.open(reportUrl, '_blank');

    // Hide the modal after generating
    historyReportModal.hide();
});

// --- END: New Listeners for Historical Report ---