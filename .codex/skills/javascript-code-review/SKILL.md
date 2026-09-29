---
name: javascript-code-review
description: Review JavaScript, browser DOM, Chrome Extension Manifest V3, fetch/API, and Vitest changes for concrete production defects. Use when the user asks for a code review of JavaScript application or Chrome Extension changes, including diffs, commits, branches, pull requests, or working-tree changes. Prioritize correctness, async behavior, security, extension messaging and storage, architecture, and tests; avoid cosmetic rewrites and unnecessary abstractions.
---

# JavaScript Code Review

Review the requested change set and report concrete, actionable findings that matter in production.

## Review workflow

1. Establish the exact review scope and comparison base from the request and repository state.
2. Read the full diff, then inspect the callers, event flows, message senders and receivers, manifest, permissions, API boundaries, DOM assumptions, storage access, and tests needed to verify behavior.
3. Trace realistic success, failure, empty, null, malformed-input, and concurrent execution paths before raising a finding. Confirm that the reviewed change introduces or exposes the problem.
4. Run focused tests or static checks when they materially increase confidence and are permitted. Distinguish observed failures from static analysis.
5. Rank findings by severity and confidence. Prefer a few strong findings over broad speculation.

## Review priorities

Inspect the following areas while following evidence wherever it leads:

- Correctness and edge cases: broken control flow, incorrect conditions, impossible branches, null or empty values, malformed data, stale state, race conditions, dead code, and behavior at input boundaries.
- Async behavior: missing or misplaced `await`, unhandled or floating promises, rejected promises, incorrect sequencing, races, fire-and-forget work, lost errors, and incomplete cleanup.
- Error handling: swallowed exceptions, misleading fallback behavior, failures that leave partial state, missing user-visible failure handling, and errors logged without recovery or propagation.
- Fetch and API calls: incorrect methods, headers, bodies, URL construction, response-status handling, assumptions about response shape, abort or timeout behavior where relevant, and authentication failures.
- DOM behavior: missing element checks, duplicate listeners, listener leaks, fragile selectors, unnecessary global queries, unsafe HTML insertion, and state that drifts from the rendered DOM.
- Chrome Extension Manifest V3 behavior: service-worker lifecycle, extension contexts, messaging, storage, permissions, content-script trust boundaries, and MV3-incompatible APIs or assumptions.
- Security: XSS, unsafe HTML insertion, token exposure, credential logging, insecure message passing, insufficient URL or sender validation, excessive permissions, and unsafe use of page-controlled or external data.
- Architecture and testability: mixed business and DOM logic, API calls embedded across unrelated code, functions with multiple responsibilities, hard-coded dependencies that prevent focused tests, meaningful duplication, and excessive complexity.
- Naming and readability: report only cases that create a credible risk of misunderstanding behavior or introducing defects. Do not report subjective style preferences.
- Tests: false-positive assertions, missing meaningful failure coverage, leaked mock state, excessive coupling to implementation details, and missing coverage for concrete changed behavior.

## Chrome Extension checks

For Chrome Extension code, verify:

- The manifest and APIs are compatible with Manifest V3, including service-worker lifecycle constraints.
- `chrome.runtime.sendMessage` and message listeners handle async responses and `runtime.lastError` or rejected promises correctly for the API style in use.
- Messages between popup, content scripts, and the service worker have clear contracts; privileged receivers validate sender, message type, and untrusted payload fields before acting.
- `chrome.storage.local` access is awaited or callback-handled correctly, expected missing values are handled, and read-modify-write flows do not introduce realistic races.
- Requested permissions and host permissions are no broader than the implemented feature requires.
- Authentication tokens are not exposed unnecessarily to page context, DOM, logs, messages, or less-trusted extension contexts.
- Content scripts treat the page DOM and page-derived values as untrusted input.
- Logic does not incorrectly depend on service-worker globals remaining alive between events.

Do not flag a valid callback-based or promise-based Chrome API solely because another style is preferred. Account for the project's Chrome version and manifest contract when evidence is available.

## DOM checks

- Verify elements may exist before property access or listener registration.
- Detect duplicate registration and missing cleanup when code can initialize or render more than once.
- Treat `innerHTML`, `insertAdjacentHTML`, and equivalent HTML sinks as unsafe when content can be influenced externally. Prefer `textContent` unless HTML rendering is intentional and the content is trusted or sanitized.
- Check whether selectors rely on unstable page structure and whether queries are repeated globally without need.
- Verify event delegation, listener options, and teardown match the element lifecycle.

## Security checks

Trace trust boundaries rather than flagging dangerous-looking APIs in isolation. Look for:

- XSS and unsafe insertion of page-controlled, API-provided, stored, or message-provided content.
- Exposed tokens, credentials, or authentication data, including logs and cross-context messages.
- Trust in values received from the page or less-privileged extension contexts.
- Messages that trigger privileged actions without validating sender, type, payload, or destination.
- URLs that are accepted or opened without validating protocol, origin, or expected shape.
- External data used in privileged operations without validation.

## Architecture checks

Check whether functions have one clear responsibility, business logic is separated from DOM manipulation, API calls have a testable boundary, and dependencies can be replaced in tests. Recommend extracting duplicated logic or splitting large functions only when it clearly reduces defect risk or improves testability. Do not propose abstractions merely to make the design more elaborate.

## Test checks

Look for missing or weak coverage of changed behavior, especially:

- success and API-failure paths;
- invalid, empty, and null input;
- authentication failure;
- missing DOM elements;
- malformed API responses;
- relevant race conditions and async ordering.

For Vitest, verify that assertions prove externally observable behavior, mocks and timers are restored or reset appropriately, error paths are exercised, and tests do not overfit implementation details.

Do not report "missing tests" generically. Tie the finding to a concrete unverified behavior or regression risk introduced by the change.

## Severity

- `P0 - Critical`: a security vulnerability, authentication bypass, data loss, or major application failure with immediate or widespread impact.
- `P1 - High`: a real defect likely to occur in normal usage or a meaningful security-boundary failure.
- `P2 - Medium`: a maintainability, robustness, or edge-case defect triggered by a realistic narrower condition.
- `P3 - Low`: a minor but concrete improvement with limited impact. Do not use this level for purely cosmetic preferences.

## Output

List findings first, grouped by severity and ordered from highest to lowest. Use this structure for every finding:

```text
[P1 - High] Concise defect title
Location: path/to/file.js:line
Relevant code: Include only the smallest useful excerpt or identify the affected expression.
Problem: Explain the concrete defect and the execution path that triggers it.
Why it matters: State the user, security, data, reliability, or maintainability impact.
Recommended change: Describe the smallest safe correction and relevant test coverage.
Example fix: Include a concise example only when it materially clarifies the correction.
```

Keep locations tight and point to changed lines whenever possible. Do not combine unrelated defects into one finding. Do not inflate severity based on hypothetical conditions unsupported by the repository.

If no high-confidence defects are found, state that explicitly. Briefly mention any material validation limitation, such as tests that could not be run, without turning it into a finding.

## Review philosophy

Prefer simple, idiomatic JavaScript and the conventions already established in the codebase.

Do not:

- suggest unnecessary design patterns or abstractions;
- rewrite working code purely for stylistic reasons;
- recommend TypeScript unless the user specifically requests it;
- treat naming, formatting, or personal preferences as defects;
- speculate without a realistic execution path and repository evidence.

Focus on findings that would matter in a real production code review.
