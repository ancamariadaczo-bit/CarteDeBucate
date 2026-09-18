# Testing and Deployment

This document describes how tests and deployment are configured for the CarteDeBucate project.

## 1. Local tests before push

Git uses a local `pre-push` hook located at:

```text
.git/hooks/pre-push
```

The hook runs automatically every time:

```bash
git push
```

It executes:

* C# tests using `dotnet test`
* JavaScript tests from `CarteDeBucate.ChromeExtension` using `npm test`

If any test fails, the push is cancelled.

### Important

The `pre-push` hook is stored inside `.git` and is **not committed to the repository**.

After cloning the repository on another computer, the hook must be recreated manually.

The file must also be executable:

```bash
chmod +x .git/hooks/pre-push
```

Current JavaScript test directory:

```text
CarteDeBucate.ChromeExtension
```

---

## 2. GitHub Actions

GitHub runs the tests again after code is pushed.

Workflow file:

```text
.github/workflows/tests.yml
```

The workflow runs on:

* push
* pull request

It installs the required .NET and Node.js environments and runs:

```bash
dotnet test
```

and:

```bash
npm test
```

for the Chrome extension.

The project currently targets:

```text
.NET 10
```

GitHub Actions results can be viewed in the **Actions** tab of the GitHub repository.

The README contains a test status badge showing whether the latest tests on `main` passed.

---

## 3. Deployment with Render

The web application is deployed using Render.

Render Auto-Deploy is configured as:

```text
After CI Checks Pass
```

This means Render waits for GitHub Actions before deploying.

Deployment flow:

```text
git push
    ↓
Local pre-push tests
    ↓
GitHub Actions
    ↓
C# tests + JavaScript tests
    ↓
Tests pass?
    ├── Yes → Render deploys
    └── No  → No deployment
```

This prevents code with failing tests from being automatically deployed.

### Web authentication configuration

The non-secret authentication settings shared by local development and production
are stored in:

```text
CarteDeBucate.Web/appsettings.json
```

The shared settings are:

```text
Jwt:Issuer
Jwt:Audience
Jwt:ExpirationMinutes
Cors:AllowedOrigins
ChromeExtension:AuthenticationRedirectUrl
```

`appsettings.Development.json` and `appsettings.Production.json`, when present,
should contain only values that are different for that environment. Environment
variables can override any JSON setting by replacing `:` with `__`, for example
`Jwt__Issuer`.

#### Local JWT secret

The JWT signing key must not be added to `appsettings.json` or another committed
file. Configure it locally with User Secrets:

```bash
dotnet user-secrets set "Jwt:Key" "<local-random-signing-key>" --project CarteDeBucate.Web/CarteDeBucate.Web.csproj
```

For HS256, use at least 32 random bytes of key material. A longer random value is
also acceptable. Do not use a password, repository value, or production key for
local development.

#### Render JWT secret

Configure the production signing key as a secret environment variable in Render:

```text
Jwt__Key=<production-random-signing-key>
```

The production key must also contain at least 32 random bytes and must be different
from the local key. The non-secret values are inherited from `appsettings.json`.
Add Render overrides only when a production value genuinely differs from the
shared value.

#### Chrome extension ID

Before local testing or a production release, read the extension ID from
`chrome://extensions` and verify that the same ID is used by both settings:

```text
Cors:AllowedOrigins
ChromeExtension:AuthenticationRedirectUrl
```

For the currently configured extension ID, the matching values are:

```text
chrome-extension://klggdhkljgmfaakhchnlblgnkgiajenj
https://klggdhkljgmfaakhchnlblgnkgiajenj.chromiumapp.org/authentication-callback
```

If the extension ID changes, update both values together before testing the login
flow.

### Chrome extension API environment

All extension API endpoints are derived from the single `API_BASE_URL` value in:

```text
CarteDeBucate.ChromeExtension/config/apiConfig.js
```

Use this value for local development:

```js
export const API_BASE_URL = "https://localhost:7080";
```

Immediately before creating the production extension ZIP, change it to:

```js
export const API_BASE_URL = "https://cartedebucate.onrender.com";
```

Create the ZIP while the production value is present, then restore
`https://localhost:7080` in the source tree for local development. There is no
automatic environment detection in the extension.

Changing the source files does not update an existing archive. In particular,
`CarteDeBucate.ChromeExtension/Recipe Clipper 1.0.0.zip` must be recreated before
uploading a new package.

---

## 4. Normal development workflow

Typical workflow:

```bash
git status
git add .
git commit -m "Description of changes"
git push
```

When `git push` is executed:

1. Local C# tests run.
2. Local JavaScript tests run.
3. If they pass, the code is pushed to GitHub.
4. GitHub Actions runs the tests again.
5. If GitHub Actions passes, Render can deploy the new version.

---

## 5. Documentation-only changes

If a commit only changes documentation and should not trigger a Render deployment, use:

```bash
git commit -m "[skip render] Update documentation"
```

Example:

```bash
git add README.md docs/
git commit -m "[skip render] Update documentation"
git push
```

---

## 6. Files involved

```text
.git/hooks/pre-push
```

Local Git hook. Not stored in GitHub.

```text
.github/workflows/tests.yml
```

GitHub Actions configuration. Stored in the repository.

```text
CarteDeBucate.ChromeExtension/package.json
```

Contains the JavaScript test command used by `npm test`.

```text
README.md
```

Contains general project information and the GitHub Actions test badge.

---

## 7. Important distinction

There are two independent test stages:

**Local testing**

```text
pre-push hook
```

Runs on the development computer before the push is allowed.

**Continuous Integration**

```text
GitHub Actions
```

Runs on GitHub after the push and verifies the project in a clean environment.

Using both provides protection before code is uploaded and independent verification after it reaches GitHub.
