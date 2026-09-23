# Testing and Deployment

This document describes how tests and deployment are configured for the CarteDeBucate project.

## 1. Local tests and code review before push

The versioned `pre-push` hook is located at:

```text
.githooks/pre-push
```

The hook runs automatically every time:

```bash
git push
```

It executes, in order:

* C# tests using `dotnet test`
* JavaScript tests from `CarteDeBucate.ChromeExtension` using `npm test`
* when Codex CLI is installed and logged in, a code review of the commits that
  are about to be pushed

If any test fails, the push is cancelled before the code review starts.

### Activating the versioned hook

The hook is stored in the repository, but Git's `core.hooksPath` setting is
local to each clone. After cloning the repository, run:

```bash
./scripts/setup-git-hooks.sh
```

The setup script configures:

```bash
git config --local core.hooksPath .githooks
```

and ensures that `.githooks/pre-push` is executable. The setup command only
needs to be run once for each clone.

After this configuration, Git uses the versioned hook from `.githooks` instead
of `.git/hooks`. Changes to the hook can therefore be reviewed, committed, and
distributed through GitHub like the rest of the project.

### Codex code review

After both test suites pass, the hook returns to the repository root and checks
whether the `codex` command exists:

```bash
command -v codex
```

When the command exists, the hook checks its authentication state:

```bash
codex login status
```

If Codex CLI is missing or the authentication check fails, the hook displays a
message, skips the code review and final confirmation, and allows the push to
continue after the successful tests.

Before running the tests, the hook reads the ref updates that Git supplies to a
`pre-push` hook on standard input:

```text
<local-ref> <local-object-id> <remote-ref> <remote-object-id>
```

The hook accepts one ref update at a time and requires its local object ID to be
the currently checked-out `HEAD`. The index and working tree must also be clean,
including untracked files, so the tests and review cannot inspect content that
is absent from the commit being pushed. This clean-state check is repeated after
the tests, after the review, and after final confirmation. Pushes containing
multiple refs or a different local branch are cancelled with instructions to
check out and push each branch separately.

For an existing remote branch, the remote object ID must be an ancestor of the
local object ID. Non-fast-forward updates are cancelled because a base-branch
review would not describe the exact history rewrite performed by a force-push.
Together, these checks prevent the review from covering different changes than
the ones actually sent by Git.

When Codex CLI is installed and logged in, an existing remote branch is reviewed
relative to the exact remote object ID supplied by Git:

```bash
codex review --base <remote-object-id>
```

For a new remote branch, Git supplies an all-zero remote object ID. The hook
queries the remote URL for its advertised `HEAD` object ID, calculates its merge
base with the local object ID, and reviews from that merge base. This works even
when the optional local `refs/remotes/<remote>/HEAD` symbolic ref does not exist.
If the remote `HEAD` or a safe merge base cannot be determined, the push is
cancelled instead of running an incomplete review.

Remote branch deletions still run the local tests but skip the code review,
because they do not introduce new code.

Once started, the push is cancelled if the Codex review command cannot be
completed. When the review finishes successfully, its findings remain visible
in the terminal and the hook asks:

```text
Codex review finished. Continue with push? [y/N]
```

Only `y`, `Y`, `yes`, `YES`, or `Yes` allows the push to continue. Any other
answer cancels it. This final decision is intentionally manual: a completed
review does not automatically mean that its findings should be accepted.

The local machine must have the `codex` command installed and authenticated for
the review step to run. Missing or expired authentication skips the optional
review instead of blocking the push.

### Important

The hook script is committed to the repository, but activating it remains a
local setup step. Git does not automatically apply repository configuration
during cloning, so every new clone must run `./scripts/setup-git-hooks.sh`.

The local hook can still be bypassed explicitly with `git push --no-verify`.
GitHub Actions and branch protection remain the server-side enforcement layer.

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
Codex installed and logged in?
    ├── No  → Skip local review
    └── Yes → Codex code review
                  ↓
             Manual confirmation
    ↓
GitHub Actions
    ↓
C# tests + JavaScript tests
    ↓
Tests pass?
    ├── Yes → Render deploys
    └── No  → No deployment
```

This prevents code with failing tests from being pushed and adds an optional,
explicitly confirmed review checkpoint before GitHub Actions and deployment.

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
3. If Codex is installed and logged in, the local code review runs.
4. After a successful review, the user confirms whether the push should continue.
5. If the local checks allow the push, the code is sent to GitHub.
6. GitHub Actions runs the tests again.
7. If GitHub Actions passes, Render can deploy the new version.

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
.githooks/pre-push
```

Versioned Git hook that runs the tests and, when Codex is logged in, the code
review and final confirmation. Stored in the repository.

```text
scripts/setup-git-hooks.sh
```

Configures the current clone to use `.githooks` through `core.hooksPath` and
ensures that the pre-push hook is executable.

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

There are two independent test stages, with an optional local code review before
the push:

**Local testing**

```text
pre-push hook
```

Runs the local tests on the development computer. When Codex CLI is installed
and logged in, it also runs the code review and requires manual confirmation
before the push is allowed.

**Continuous Integration**

```text
GitHub Actions
```

Runs on GitHub after the push and verifies the project in a clean environment.

Using both provides protection before code is uploaded and independent verification after it reaches GitHub.
