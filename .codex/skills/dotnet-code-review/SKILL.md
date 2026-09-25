---
name: dotnet-code-review
description: Review C#, ASP.NET Core, Entity Framework Core, API, authentication, authorization, repository, service, and .NET test changes for high-confidence defects. Use when the user asks for a code review of .NET application changes, including diffs, commits, branches, pull requests, or working-tree changes. Prioritize correctness, security, async/await, dependency injection, EF Core, authentication and authorization, architecture, performance, and tests; avoid cosmetic style commentary.
---

# .NET Code Review

Review the requested change set and report only concrete, actionable defects.

## Review workflow

1. Establish the exact review scope and comparison base from the request and repository state.
2. Read the full diff, then inspect relevant callers, contracts, configuration, migrations, middleware, persistence code, and tests needed to verify behavior.
3. Trace realistic execution paths before raising a finding. Confirm that the changed code introduces or exposes the problem.
4. Run focused builds or tests when they materially increase confidence and are permitted. Distinguish observed failures from static analysis.
5. Rank findings by severity and confidence. Prefer a few strong findings over broad speculation.

## Review priorities

Inspect in this order, while following evidence wherever it leads:

- Correctness: broken control flow, invalid state, null and boundary handling, error paths, data loss, transaction behavior, concurrency, and contract mismatches.
- Security: trust boundaries, input validation, injection, sensitive data exposure, CSRF, token and cookie handling, unsafe redirects, and insecure defaults.
- Authentication and authorization: scheme configuration, policy enforcement, claims, resource ownership, anonymous access, privilege escalation, and correct 401/403 behavior.
- Async/await: missing awaits, sync-over-async, fire-and-forget work, cancellation, exception propagation, deadlocks, and concurrent `DbContext` use.
- Dependency injection: missing registrations, lifetime mismatches, scoped services captured by singletons, disposal, and environment-specific wiring.
- Entity Framework Core: query semantics, tracking, relationship loading, N+1 queries, projections, migrations, optimistic concurrency, transactions, and async database APIs.
- Architecture: boundary or dependency violations only when they create a concrete correctness, security, operability, or maintainability defect in the changed behavior.
- Performance: repeated I/O, unbounded materialization, unnecessary round trips, hot-path allocations, or blocking work with meaningful runtime impact.
- Tests: incorrect assertions, nondeterminism, false positives, missing coverage for a concrete changed contract, and tests that do not exercise the claimed behavior.

Do not report formatting, naming, subjective refactoring preferences, or speculative future risks as findings. Do not invent failures that depend on unsupported assumptions. If evidence is incomplete, investigate further or omit the finding.

## Severity

- `P0 - Critical`: immediate security compromise, widespread data loss, or a release-blocking failure.
- `P1 - High`: a serious defect likely to affect normal production use or a meaningful security boundary.
- `P2 - Medium`: a real defect triggered by a realistic but narrower condition.
- `P3 - Low`: a limited-impact correctness issue worth fixing; never use this level for cosmetic style.

## Output

List findings first, ordered by severity. Use this structure for every finding:

```text
[P1 - High] Concise defect title
Location: path/to/File.cs:line
Problem: Explain the concrete defect and the execution path that triggers it.
Why it matters: State the user, security, data, reliability, or performance impact.
Suggested fix: Describe the smallest safe correction, including test coverage when useful.
```

Keep code locations tight and point to changed lines whenever possible. Do not combine unrelated defects into one finding.

If no high-confidence defects are found, state that explicitly. Briefly mention any material validation limitation, such as tests that could not be run, without turning it into a finding.
