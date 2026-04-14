# Getting Personal Keycloak Tokens

This guide s### Authentication Issues

- Check if your account has access to the realm
- Ensure you're using the correct Keycloak environment
- Verify your user account is properly configured

Your personal JWT token will now work perfectly with your API endpoints! 🎉ow to obtain JWT tokens from your Keycloak SSO for API testing with your user account.

## 🔧 **Your Keycloak Configuration**

Based on your configuration (replace with your actual values):

```json
{
  "Keycloak": {
    "AuthServerUrl": "https://your-keycloak-server.com/auth",
    "Realm": "your-realm",
    "Resource": "your-client-id",
    "Credentials": {
      "Secret": "your-client-secret-here"
    }
  }
}
```eycloak Tokens (IDIR/BCeID) for API Testing

This guide shows how to obtain JWT tokens from BC Government's Keycloak SSO using your **IDIR** or **BCeID** account for API testing.

## ?? **Your Keycloak Configuration**

Based on your secrets.json:

```json
{
  "Keycloak": {
    "AuthServerUrl": "https://your-keycloak-server.com/auth",
    "Realm": "your-realm",
    "Resource": "your-client-id",
    "Credentials": {
      "Secret": "your-client-secret-here"
    }
  }
}
```

## ?? **Important Note**

Your client (`your-client-id`) is a **confidential client**, not a service account. This means:
- ? **Client Credentials Flow** won't work for user authentication
- ? **Authorization Code Flow** with browser login is required
- ? You must authenticate with your **IDIR** or **BCeID** account

## ?? **Method 1: Authorization Code Flow (Recommended)**

This is the standard way to get a personal token with user authentication.

### Step 1: Browser Login & Get Authorization Code

1. **Open this URL in your browser** (replace `localhost:8080` with any valid redirect URI):

```url
https://your-keycloak-server.com/auth/realms/your-realm/protocol/openid-connect/auth?client_id=your-client-id&response_type=code&scope=openid%20profile%20email&redirect_uri=http://localhost:8080/callback&state=test123
```

2. **You'll be redirected to your authentication provider** to login

3. **After successful login**, you'll be redirected to:
```
http://localhost:8080/callback?code=AUTH_CODE_HERE&session_state=...&state=test123
```

4. **Copy the `code` parameter** from the URL - this is your authorization code.

### Step 2: Exchange Authorization Code for Token

Now use **Postman** or **curl** to exchange the code for a JWT token:

#### Using Postman

1. **Create New Request**
   - Method: `POST`
   - URL: `https://your-keycloak-server.com/auth/realms/your-realm/protocol/openid-connect/token`

2. **Headers**

   ```http
   Content-Type: application/x-www-form-urlencoded
   ```

3. **Body** (x-www-form-urlencoded)

   ```form-data
   grant_type: authorization_code
   client_id: your-client-id
   client_secret: your-client-secret
   code: [PASTE_AUTH_CODE_FROM_STEP_1]
   redirect_uri: http://localhost:8080/callback
   ```

4. **Send Request** - You'll get your personal JWT token:
   ```json
   {
     "access_token": "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9...",
     "expires_in": 300,
     "refresh_expires_in": 1800,
     "refresh_token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
     "token_type": "Bearer",
     "id_token": "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9...",
     "not-before-policy": 0,
     "session_state": "uuid-here",
     "scope": "openid profile email"
   }
   ```

#### Using curl

```bash
curl -X POST "https://your-keycloak-server.com/auth/realms/your-realm/protocol/openid-connect/token" \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "grant_type=authorization_code" \
  -d "client_id=your-client-id" \
  -d "client_secret=your-client-secret" \
  -d "code=YOUR_AUTH_CODE_HERE" \
  -d "redirect_uri=http://localhost:8080/callback"
```

## ?? **Method 2: Using Refresh Token**

Once you have tokens, you can use the refresh token to get new access tokens without re-authenticating:

### Using Postman

1. **Create New Request**
   - Method: `POST`
   - URL: `https://your-keycloak-server.com/auth/realms/your-realm/protocol/openid-connect/token`

2. **Body** (x-www-form-urlencoded)

   ```form-data
   grant_type: refresh_token
   client_id: your-client-id
   client_secret: your-client-secret
   refresh_token: [YOUR_REFRESH_TOKEN]
   ```

## ??? **Method 3: Automated PowerShell Scripts**

For easier automation, use the PowerShell scripts:

```powershell
# Setup configuration (one-time)
.\scripts\setup-dev-secrets.ps1

# Get token automatically
$password = Read-Host -AsSecureString -Prompt "Enter IDIR password"
.\scripts\Get-KeycloakTokenSimple.ps1 -Username "your-idir-username" -Password $password -IdentityProvider "IDIR"

# Use the token
curl -H "Authorization: Bearer $env:KEYCLOAK_TOKEN" https://localhost:7000/Auth/userinfo
```

## ??? **Method 4: Streamlined Browser Method**

For easier testing, you can use a simple HTML page to handle the redirect:

### Create a Local Test Page

Create `token-helper.html`:

```html
<!DOCTYPE html>
<html>
<head>
    <title>Keycloak Token Helper</title>
</head>
<body>
    <h1>Keycloak Token Helper</h1>
    
    <div id="config-form">
        <h2>Configuration</h2>
        <label>Keycloak URL: <input type="text" id="keycloakUrl" placeholder="https://your-keycloak-server/auth" /></label><br>
        <label>Realm: <input type="text" id="realm" placeholder="your-realm" /></label><br>
        <label>Client ID: <input type="text" id="clientId" placeholder="your-client-id" /></label><br>
        <label>Client Secret: <input type="password" id="clientSecret" placeholder="your-client-secret" /></label><br>
        <button onclick="startAuth()">Login with IDIR/BCeID</button>
    </div>
    
    <div id="step2" style="display:none;">
        <h2>Step 2: Exchange Code for Token</h2>
        <p>Authorization Code: <span id="authCode"></span></p>
        <button onclick="getToken()">Get JWT Token</button>
        <div id="tokenResult"></div>
    </div>

    <script>
        function startAuth() {
            const keycloakUrl = document.getElementById('keycloakUrl').value;
            const realm = document.getElementById('realm').value;
            const clientId = document.getElementById('clientId').value;
            
            if (!keycloakUrl || !realm || !clientId) {
                alert('Please fill in all configuration fields');
                return;
            }
            
            // Store config in sessionStorage
            sessionStorage.setItem('keycloakUrl', keycloakUrl);
            sessionStorage.setItem('realm', realm);
            sessionStorage.setItem('clientId', clientId);
            sessionStorage.setItem('clientSecret', document.getElementById('clientSecret').value);
            
            const authUrl = `${keycloakUrl}/realms/${realm}/protocol/openid-connect/auth?` +
                `client_id=${clientId}&` +
                `response_type=code&` +
                `scope=openid profile email&` +
                `redirect_uri=${encodeURIComponent(window.location.origin + '/token-helper.html')}&` +
                `state=test123`;
            
            window.location.href = authUrl;
        }

        async function getToken() {
            const code = document.getElementById('authCode').textContent;
            const keycloakUrl = sessionStorage.getItem('keycloakUrl');
            const realm = sessionStorage.getItem('realm');
            const clientId = sessionStorage.getItem('clientId');
            const clientSecret = sessionStorage.getItem('clientSecret');
            
            const response = await fetch(`${keycloakUrl}/realms/${realm}/protocol/openid-connect/token`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/x-www-form-urlencoded',
                },
                body: new URLSearchParams({
                    grant_type: 'authorization_code',
                    client_id: clientId,
                    client_secret: clientSecret,
                    code: code,
                    redirect_uri: window.location.origin + '/token-helper.html'
                })
            });

            const tokenData = await response.json();
            document.getElementById('tokenResult').innerHTML = `
                <h3>Your JWT Token:</h3>
                <textarea style="width:100%;height:200px;">${tokenData.access_token}</textarea>
                <p><strong>Expires in:</strong> ${tokenData.expires_in} seconds</p>
            `;
        }

        // Check if we came back from Keycloak with a code
        window.onload = function() {
            const urlParams = new URLSearchParams(window.location.search);
            const code = urlParams.get('code');
            
            if (code) {
                document.getElementById('config-form').style.display = 'none';
                document.getElementById('step2').style.display = 'block';
                document.getElementById('authCode').textContent = code;
            }
        };
    </script>
</body>
</html>
```

### Using the Helper

1. Save the HTML file locally
2. Open it in your browser (`file:///path/to/token-helper.html`)
3. Fill in your configuration (from user secrets)
4. Click "Login with IDIR/BCeID"
5. Complete authentication
6. Click "Get JWT Token"
7. Copy the JWT token for API testing

## 🧪 **Testing Your API with the Token**

Once you have your JWT token, test it with your API:

### Using Postman

1. **Create New Request**
   - Method: `GET`
   - URL: `https://localhost:7000/Auth/userinfo`

2. **Authorization Tab**
   - Type: `Bearer Token`
   - Token: `[paste your JWT token here]`

3. **Expected Response** (your IDIR/BCeID info):
   ```json
   {
     "userId": "your-uuid-here",
     "username": "your-idir-username",
     "email": "your.email@gov.bc.ca",
     "fullName": "Your Full Name",
     "firstName": "Your",
     "lastName": "Name",
     "realmRoles": ["user", "admin"],
     "isAdmin": false,
     "isUser": true,
     "requestedAt": "2024-01-16T10:30:00Z"
   }
   ```

### Using curl

```bash
# Test the UserInfo endpoint
curl -H "Authorization: Bearer YOUR_JWT_TOKEN_HERE" \
     https://localhost:7000/Auth/userinfo

# Test a protected profile endpoint
curl -H "Authorization: Bearer YOUR_JWT_TOKEN_HERE" \
     https://localhost:7000/api/profiles/123
```

## 🔍 **Understanding Your Token**

Your JWT token will contain claims specific to your IDIR/BCeID account. Decode it at https://jwt.io to see:

```json
{
  "exp": 1640995200,
  "iat": 1640995000,
  "auth_time": 1640994900,
  "jti": "uuid-here",
  "iss": "https://your-keycloak-server/auth/realms/your-realm",
  "aud": "your-client-id",
  "sub": "your-user-id",
  "typ": "Bearer",
  "azp": "your-client-id",
  "session_state": "session-uuid",
  "acr": "1",
  "realm_access": {
    "roles": ["default-roles-standard", "offline_access", "uma_authorization"]
  },
  "resource_access": {
    "your-client-id": {
      "roles": ["user"]
    }
  },
  "scope": "openid profile email",
  "sid": "session-id",
  "email_verified": true,
  "name": "Your Full Name",
  "preferred_username": "your-idir@idir",
  "given_name": "Your",
  "family_name": "Name",
  "email": "your.email@gov.bc.ca",
  "identity_provider": "idir",
  "idir_user_guid": "your-idir-guid",
  "idir_username": "your-idir-username"
}
```

## 🔒 **Security Notes**

- **Your tokens are personal** - they represent YOUR IDIR/BCeID account
- **Don't share tokens** - they contain your identity information
- **Tokens expire** - typically 5-15 minutes for access tokens
- **Use refresh tokens** - they last longer (usually 30 minutes)
- **HTTPS only** - Never send tokens over HTTP
- **Never commit secrets** - Use user secrets or environment variables

## 🚨 **Troubleshooting**

### "Client not enabled to retrieve service account"
- ✅ **Correct**: This confirms you need user authentication (not service account)
- ✅ **Solution**: Use the browser-based authorization code flow above

### "Invalid redirect URI"
- Make sure the `redirect_uri` in your token request exactly matches what you used in the authorization URL
- BC Government Keycloak may have strict redirect URI validation

### "Invalid client"
- Double-check your client_id and client_secret in your configuration
- Ensure you're using the correct Keycloak server

### Authentication Issues
- Try both IDIR and BCeID if one doesn't work
- Check if your account has access to the specified realm
- Ensure you're using the correct Keycloak environment

### Configuration Not Found
```bash
# Check your user secrets
cd src/Grants.ApplicantPortal.API.Web
dotnet user-secrets list

# Or check environment variables
echo $KEYCLOAK_CLIENT_ID
```

Your personal JWT token will now work perfectly with your API endpoints! 🚀