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
