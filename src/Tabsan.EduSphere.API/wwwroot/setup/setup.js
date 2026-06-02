(function () {
    const dbType = document.getElementById('dbType');
    const localProvider = document.getElementById('localProvider');
    const localProviderGroup = document.getElementById('localProviderGroup');
    const sqlitePathGroup = document.getElementById('sqlitePathGroup');
    const serverGroup = document.getElementById('serverGroup');
    const portGroup = document.getElementById('portGroup');
    const authGroups = document.querySelectorAll('.auth-group');
    const trustedConnection = document.getElementById('trustedConnection');
    const username = document.getElementById('username');
    const password = document.getElementById('password');

    function setDisplay(el, visible) {
        if (!el) return;
        el.style.display = visible ? '' : 'none';
    }

    function updateVisibility() {
        const isLocal = dbType.value === 'Local';
        setDisplay(localProviderGroup, isLocal);

        const sqlite = isLocal && localProvider.value === 'Sqlite';

        setDisplay(sqlitePathGroup, sqlite);
        setDisplay(serverGroup, !sqlite);
        setDisplay(portGroup, !sqlite);

        authGroups.forEach(g => setDisplay(g, !sqlite));

        if (sqlite) {
            trustedConnection.checked = false;
            trustedConnection.disabled = true;
            username.value = '';
            password.value = '';
        } else {
            trustedConnection.disabled = false;
        }

        const trusted = trustedConnection.checked;
        username.disabled = trusted || sqlite;
        password.disabled = trusted || sqlite;
    }

    if (dbType) dbType.addEventListener('change', updateVisibility);
    if (localProvider) localProvider.addEventListener('change', updateVisibility);
    if (trustedConnection) trustedConnection.addEventListener('change', updateVisibility);

    updateVisibility();

    const form = document.getElementById('dbSetupForm');
    const testButton = document.getElementById('testConnectionButton');
    const testResult = document.getElementById('testResult');

    if (!form || !testButton || !testResult) return;

    testButton.addEventListener('click', async function () {
        testResult.innerHTML = '';

        const formData = new FormData(form);
        const token = formData.get('__RequestVerificationToken');

        try {
            const response = await fetch('/setup/test-connection', {
                method: 'POST',
                headers: {
                    'RequestVerificationToken': token
                },
                body: formData
            });

            const payload = await response.json();
            const cssClass = payload.success ? 'alert alert-success' : 'alert alert-danger';
            testResult.innerHTML = '<div class="' + cssClass + '">' + payload.message + '</div>';
        } catch (error) {
            testResult.innerHTML = '<div class="alert alert-danger">Connection test failed to execute.</div>';
        }
    });
})();
