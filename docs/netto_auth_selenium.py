"""
Netto+ Full Auth Flow — customer-program client
-------------------------------------------------
Uses clientId=customer-program which redirects to localhost,
giving completely independent sessions from the Netto scan-and-go app.

Token refresh with customer-program NEVER affects the Netto app's tokens.

Flow:
  1. Gigya login (headless)
  2. Gigya JWT (headless)
  3. DSG PKCE authorization via p-idp.dsgapps.dk (browser — manual login)
  4. Auth code exchange (headless)
  5. Token refresh (headless)
  6. Receipt API call (headless)

Requirements:
    pip install selenium requests

Usage:
    python netto_auth_selenium.py
    Log in manually in the browser window that opens.
"""

import requests
import hashlib
import base64
import secrets
import string
import json
import time
import urllib.parse
from urllib.parse import urlparse, parse_qs

from selenium import webdriver
from selenium.webdriver.chrome.options import Options


# ----------------------------------------------------------------
# CONFIG
# ----------------------------------------------------------------
EMAIL    = "jeppebs03@gmail.com"
PASSWORD = "Lillepip10!"
# ----------------------------------------------------------------

GIGYA_API_KEY  = "3_8MtnEg0K6I9nkOrVGsFLTmrkAJhJAm7b_lRQHO6O71xnIM2MiIQ8aNoa-FOFY6wA"
GIGYA_BASE_URL = "https://accounts.eu1.gigya.com"
IDP_BASE_URL   = "https://p-idp.dsgapps.dk"
IDP_TOKEN_URL  = "https://idp.dsgapps.dk/token"
CLIENT_ID      = "customer-program"
REDIRECT_URI   = "http://localhost"
CLUB_API_URL   = "https://p-club.dsgapps.dk/api/cp"


def generate_pkce():
    verifier = ''.join(secrets.choice(string.ascii_letters + string.digits + '-._~') for _ in range(64))
    challenge = base64.urlsafe_b64encode(
        hashlib.sha256(verifier.encode()).digest()
    ).rstrip(b'=').decode()
    return verifier, challenge


def gigya_login(email, password):
    """Authenticate with Gigya and return a session token."""
    print("[1] Gigya login...")
    resp = requests.post(f"{GIGYA_BASE_URL}/accounts.login", data={
        "apiKey":   GIGYA_API_KEY,
        "loginID":  email,
        "password": password,
        "format":   "json",
    })
    resp.raise_for_status()
    data = resp.json()

    if data.get("errorCode") == 206001:
        print("    First-time consent required — completing registration...")
        reg_token = data["regToken"]
        preferences = json.dumps({
            "dk.salling.newsletter_clubsalling": {"isConsentGranted": True},
            "terms.dk.salling.clubsalling":      {"isConsentGranted": True},
            "privacy.dk.salling.clubsalling":    {"isConsentGranted": True},
            "profiling.dk.salling.clubsalling":  {"isConsentGranted": True}
        })
        resp2 = requests.post(f"{GIGYA_BASE_URL}/accounts.setAccountInfo", data={
            "apiKey":               GIGYA_API_KEY,
            "regToken":             reg_token,
            "format":               "json",
            "preferences":          preferences,
            "finalizeRegistration": "true"
        })
        resp2.raise_for_status()
        if resp2.json().get("errorCode", 0) != 0:
            raise Exception(f"Consent registration failed: {resp2.json().get('errorDetails')}")
        resp3 = requests.post(f"{GIGYA_BASE_URL}/accounts.login", data={
            "apiKey":   GIGYA_API_KEY,
            "loginID":  email,
            "password": password,
            "format":   "json",
        })
        resp3.raise_for_status()
        data = resp3.json()

    if data.get("errorCode", 0) != 0:
        raise Exception(f"Gigya login failed: {data.get('errorDetails') or data.get('errorMessage')}")

    session_token = data["sessionInfo"].get("sessionToken") or data["sessionInfo"].get("cookieValue")
    print(f"    OK — UID: {data['UID']}")
    return data["UID"], session_token


def gigya_get_jwt(session_token):
    """Exchange Gigya session token for a signed JWT."""
    print("[2] Getting Gigya JWT...")
    resp = requests.post(f"{GIGYA_BASE_URL}/accounts.getJWT", data={
        "apiKey":      GIGYA_API_KEY,
        "login_token": session_token,
        "fields":      "data,profile",
        "format":      "json",
    })
    resp.raise_for_status()
    data = resp.json()
    if data.get("errorCode", 0) != 0:
        raise Exception(f"getJWT failed: {data.get('errorDetails')}")
    print("    OK")
    return data["id_token"]


def get_auth_code_via_browser(gigya_jwt, session_token):
    """
    Open Chrome for manual login using customer-program client.
    Redirects to localhost so Selenium can read the URL directly.
    """
    print("[3] Opening browser — please log in manually...")
    verifier, challenge = generate_pkce()

    params = {
        "clientId":              CLIENT_ID,
        "tenantId":              "4",
        "channel":               "CustomerProgram",
        "clientFlow":            "gigya",
        "code_challenge":        challenge,
        "code_challenge_method": "S256",
        "nonce":                 secrets.token_hex(6),
        "clientTraceId":         "CT-PYTHON01",
        "emailOrPhone":          EMAIL,
        "login_hint":            gigya_jwt,
        "login_token":           session_token,
    }
    auth_url = f"{IDP_BASE_URL}/apps?" + urllib.parse.urlencode(params)

    options = Options()
    options.add_argument("--no-sandbox")
    options.add_argument("--disable-dev-shm-usage")
    options.set_capability("goog:loggingPrefs", {"browser": "ALL"})

    driver = webdriver.Chrome(options=options)
    driver.get(auth_url)

    print("\n" + "="*60)
    print("  Steps:")
    print("  1. Enter your password → click Næste")
    print("  (email is pre-filled)")
    print("  Script catches the localhost redirect automatically.")
    print("  2 min timeout.")
    print("="*60 + "\n")

    auth_code = None
    id_token_from_redirect = None
    start = time.time()

    while time.time() - start < 120:
        try:
            current_url = driver.current_url

            # customer-program redirects to localhost
            if "localhost" in current_url and ("code=" in current_url or "id_token=" in current_url):
                print(f"    Redirect caught: {current_url[:120]}")
                parsed = urlparse(current_url)
                fragment_params = parse_qs(parsed.fragment)
                query_params = parse_qs(parsed.query)
                auth_code = query_params.get("code", fragment_params.get("code", [None]))[0]
                id_token_from_redirect = fragment_params.get("id_token", [None])[0]
                break

        except Exception:
            pass

        time.sleep(0.5)

    driver.quit()

    if not auth_code:
        raise Exception("Timed out — auth code not found")

    print(f"    OK — auth code: {auth_code[:20]}... (len={len(auth_code)})")
    return verifier, auth_code, id_token_from_redirect


def exchange_for_tokens(auth_code, verifier):
    """Exchange PKCE auth code for access + refresh + id tokens."""
    print("[4] Exchanging auth code for tokens...")
    resp = requests.post(IDP_TOKEN_URL, data={
        "grant_type":    "authorization_code",
        "code":          auth_code,
        "code_verifier": verifier,
        "client_id":     CLIENT_ID,
        "redirect_uri":  REDIRECT_URI,
    })
    if not resp.ok:
        raise Exception(f"Token exchange failed ({resp.status_code}): {resp.text}")
    print("    OK")
    return resp.json()


def refresh_tokens(refresh_token):
    """
    Silently refresh tokens using customer-program client.
    Completely independent from Netto app's scan-and-go-native tokens.
    """
    print("[5] Refreshing tokens...")
    resp = requests.post(IDP_TOKEN_URL, data={
        "grant_type":    "refresh_token",
        "refresh_token": refresh_token,
        "client_id":     CLIENT_ID,
    })
    if not resp.ok:
        raise Exception(f"Refresh failed ({resp.status_code}): {resp.text}")
    print("    OK")
    return resp.json()


def get_receipts(access_token, id_token):
    """Fetch full receipt history grouped by month."""
    print("[6] Fetching receipts...")
    resp = requests.get(f"{CLUB_API_URL}/receipt", headers={
        "Authorization": f"Bearer {access_token}",
        "x-id_token":    f"Bearer {id_token}",
    })
    if not resp.ok:
        raise Exception(f"Receipt list failed ({resp.status_code}): {resp.text}")
    return resp.json()


if __name__ == "__main__":
    # Steps 1-2: Headless Gigya auth
    uid, session_token = gigya_login(EMAIL, PASSWORD)
    gigya_jwt = gigya_get_jwt(session_token)

    # Step 3: Browser PKCE — manual login
    verifier, auth_code, id_token_hint = get_auth_code_via_browser(gigya_jwt, session_token)

    # Step 4: Exchange code for tokens
    tokens = exchange_for_tokens(auth_code, verifier)
    access_token  = tokens["access_token"]
    refresh_token = tokens["refresh_token"]
    id_token      = tokens.get("id_token", id_token_hint or "")

    print("\n=== TOKENS ===")
    print(f"access_token:  {access_token[:60]}...")
    print(f"refresh_token: {refresh_token}")
    print(f"id_token:      {id_token[:60]}...")
    print(f"\nNote: These tokens use client_id=customer-program")
    print(f"      They are 100% independent from the Netto app's tokens.")

    # Step 5: Test silent refresh
    refreshed    = refresh_tokens(refresh_token)
    access_token = refreshed["access_token"]
    id_token     = refreshed.get("id_token", id_token)
    print("    Refresh successful — tokens rotated")

    # Step 6: Call receipt API
    receipt_data = get_receipts(access_token, id_token)
    groups = receipt_data.get("receiptsList", [])
    total  = sum(len(g["receipts"]) for g in groups)

    print(f"\n=== RECEIPTS ({total} total across {len(groups)} months) ===")
    for group in groups[:3]:
        print(f"\n{group['groupTitle']} — savings: {group['groupSavingsTxt']}")
        for r in group["receipts"][:3]:
            print(f"  {r['title']:20} {r['storeName']:25} {r['salesTotalTxt']:>10}  [{r['type']}]")