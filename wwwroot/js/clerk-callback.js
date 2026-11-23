// Glue between Clerk's browser SDK and the server. No secrets here: the token is sent once over HTTPS.
(function () {
    var root = document.getElementById('clerk-root');
    var status = document.getElementById('clerk-status');
    function fail(msg) {
        status.textContent = msg;
        document.getElementById('clerk-fallback').classList.remove('d-none');
    }

    window.addEventListener('load', async function () {
        try {
            await Clerk.load({ ui: { ClerkUI: window.__internal_ClerkUICtor } });

            if (root.dataset.signout === '1') {
                await Clerk.signOut({ redirectUrl: root.dataset.loginUrl });
                return;
            }
            if (!Clerk.session) {
                // Clerk's hosted sign-in sends the browser back to this exact URL afterwards.
                await Clerk.redirectToSignIn({
                    signInForceRedirectUrl: window.location.href,
                    signUpForceRedirectUrl: window.location.href
                });
                return;
            }

            var token = await Clerk.session.getToken();
            var antiforgery = root.querySelector('input[name="__RequestVerificationToken"]').value;
            var res = await fetch(root.dataset.sessionUrl, {
                method: 'POST',
                credentials: 'same-origin',
                headers: { 'Content-Type': 'application/json', 'RequestVerificationToken': antiforgery },
                body: JSON.stringify({ token: token, returnUrl: root.dataset.returnUrl || null })
            });
            if (!res.ok) {
                var err = await res.json().catch(function () { return {}; });
                fail(err.error || 'Sign-in failed. Please try again.');
                return;
            }
            var data = await res.json();
            window.location.replace(data.redirect || root.dataset.defaultUrl);
        } catch (e) {
            fail('Could not reach the sign-in service. Please try again.');
        }
    });
})();
