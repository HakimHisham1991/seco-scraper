(function () {
    const itemInput = document.getElementById("itemInput");
    const csvFile = document.getElementById("csvFile");
    const statusMessage = document.getElementById("statusMessage");
    const resultsBody = document.getElementById("resultsBody");
    const filterInput = document.getElementById("filterInput");
    const progressElapsed = document.getElementById("progressElapsed");

    let allResults = [];
    let elapsedTimer = null;
    let elapsedSeconds = 0;
    let isProcessingRun = false;

    function showStatus(message, type) {
        statusMessage.textContent = message;
        statusMessage.className = `alert alert-${type} mt-3`;
        statusMessage.classList.remove("d-none");
    }

    function formatElapsed(seconds) {
        const hrs = Math.floor(seconds / 3600);
        const mins = Math.floor((seconds % 3600) / 60);
        const secs = seconds % 60;
        return [hrs, mins, secs].map((v) => String(v).padStart(2, "0")).join(":");
    }

    function startElapsedTimer() {
        stopElapsedTimer();
        elapsedSeconds = 0;
        progressElapsed.textContent = formatElapsed(elapsedSeconds);
        elapsedTimer = window.setInterval(() => {
            elapsedSeconds += 1;
            progressElapsed.textContent = formatElapsed(elapsedSeconds);
        }, 1000);
    }

    function stopElapsedTimer() {
        if (elapsedTimer) {
            window.clearInterval(elapsedTimer);
            elapsedTimer = null;
        }
    }

    function beginProcessingRun() {
        isProcessingRun = true;
        startElapsedTimer();
    }

    function clearDashboard() {
        allResults = [];
        resultsBody.innerHTML = "";
        filterInput.value = "";
        progressElapsed.textContent = "00:00:00";
        stopElapsedTimer();
        isProcessingRun = false;
        elapsedSeconds = 0;
        document.getElementById("statTotal").textContent = "0";
        document.getElementById("statPending").textContent = "0";
        document.getElementById("statProcessing").textContent = "0";
        document.getElementById("statCompleted").textContent = "0";
        document.getElementById("statFailed").textContent = "0";
    }

    async function clearAllData() {
        clearDashboard();
        itemInput.value = "";
        csvFile.value = "";
        statusMessage.classList.add("d-none");

        try {
            await fetch("/api/items/clear", { method: "POST", cache: "no-store" });
        } catch {
            // UI is cleared; server clear will retry on next action
        }
    }

    function parseLines(text) {
        return text
            .split(/[\r\n,;\t]+/)
            .map((x) => x.trim().replace(/^p_/i, ""))
            .map((x) => x.replace(/\D/g, ""))
            .filter((x) => x.length > 0);
    }

    async function refreshStatus() {
        const response = await fetch("/api/items/status", { cache: "no-store" });
        if (!response.ok) return;
        const data = await response.json();
        document.getElementById("statTotal").textContent = data.total ?? 0;
        document.getElementById("statPending").textContent = data.pending ?? 0;
        document.getElementById("statProcessing").textContent = data.processing ?? 0;
        document.getElementById("statCompleted").textContent = data.completed ?? 0;
        document.getElementById("statFailed").textContent = data.failed ?? 0;

        const active = (data.pending ?? 0) + (data.processing ?? 0);
        if (isProcessingRun) {
            if (active > 0 && !elapsedTimer) {
                elapsedTimer = window.setInterval(() => {
                    elapsedSeconds += 1;
                    progressElapsed.textContent = formatElapsed(elapsedSeconds);
                }, 1000);
            }

            if (active === 0 && (data.total ?? 0) > 0) {
                stopElapsedTimer();
                isProcessingRun = false;
            }
        }
    }

    async function refreshResults() {
        const response = await fetch("/api/items/results?take=2000", { cache: "no-store" });
        if (!response.ok) return;
        allResults = await response.json();
        renderResults();
    }

    function statusClass(status) {
        switch ((status || "").toLowerCase()) {
            case "completed": return "badge-completed";
            case "failed": return "badge-failed";
            case "processing": return "badge-processing";
            default: return "badge-pending";
        }
    }

    function renderResults() {
        const filter = (filterInput.value || "").trim().toLowerCase();
        const rows = allResults.filter((r) => {
            if (!filter) return true;
            return (r.itemNumber || "").toLowerCase().includes(filter)
                || (r.itemDescription || "").toLowerCase().includes(filter)
                || (r.status || "").toLowerCase().includes(filter);
        });

        resultsBody.innerHTML = rows.map((r, index) => `
            <tr>
                <td>${index + 1}</td>
                <td>${escapeHtml(r.itemNumber)}</td>
                <td>${escapeHtml(r.itemDescription || "")}</td>
                <td class="${statusClass(r.status)}">${escapeHtml(r.status)}</td>
                <td>${escapeHtml(new Date(r.updatedUtc).toISOString().replace("T", " ").slice(0, 19))}</td>
            </tr>`).join("");
    }

    function escapeHtml(value) {
        return String(value)
            .replaceAll("&", "&amp;")
            .replaceAll("<", "&lt;")
            .replaceAll(">", "&gt;")
            .replaceAll("\"", "&quot;");
    }

    document.getElementById("uploadBtn").addEventListener("click", async () => {
        const numbers = parseLines(itemInput.value || "");
        if (numbers.length === 0) {
            showStatus("Enter at least one item number.", "danger");
            return;
        }

        await clearAllData();

        const response = await fetch("/api/items/upload", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ itemNumbers: numbers })
        });

        if (!response.ok) {
            showStatus("Upload failed.", "danger");
            return;
        }

        const result = await response.json();
        showStatus(`Uploaded ${result.added} item(s) (${result.totalSubmitted} submitted).`, "success");
        await refreshStatus();
        await refreshResults();
    });

    document.getElementById("uploadCsvBtn").addEventListener("click", async () => {
        const file = csvFile.files?.[0];
        if (!file) {
            showStatus("Choose a CSV file first.", "danger");
            return;
        }

        await clearAllData();

        const formData = new FormData();
        formData.append("file", file);
        const response = await fetch("/api/items/upload/csv", { method: "POST", body: formData });
        if (!response.ok) {
            showStatus("CSV upload failed.", "danger");
            return;
        }

        const result = await response.json();
        showStatus(`CSV uploaded: ${result.added} item(s).`, "success");
        await refreshStatus();
        await refreshResults();
    });

    document.getElementById("startBtn").addEventListener("click", async () => {
        const response = await fetch("/api/items/process", { method: "POST" });
        if (!response.ok) {
            showStatus("Failed to start processing.", "danger");
            return;
        }

        beginProcessingRun();
        showStatus("Processing started.", "info");
        await refreshStatus();
    });

    document.getElementById("retryBtn").addEventListener("click", async () => {
        const response = await fetch("/api/items/retry-failed", { method: "POST" });
        if (!response.ok) {
            showStatus("Retry request failed.", "danger");
            return;
        }

        beginProcessingRun();
        showStatus("Failed items queued for retry.", "info");
        await refreshStatus();
    });

    document.getElementById("exportBtn").addEventListener("click", () => {
        window.location.href = "/api/items/export";
    });

    document.getElementById("clearResultsBtn").addEventListener("click", async () => {
        await clearAllData();
        showStatus("Results cleared.", "info");
    });

    document.getElementById("clearBtn").addEventListener("click", () => {
        itemInput.value = "";
        csvFile.value = "";
    });

    filterInput.addEventListener("input", renderResults);

    async function refreshAll() {
        await Promise.all([refreshStatus(), refreshResults()]);
    }

    async function initializePage() {
        await clearAllData();
    }

    initializePage();
    setInterval(refreshAll, 10000);

    window.addEventListener("pageshow", (event) => {
        if (event.persisted) {
            initializePage();
        }
    });
})();
