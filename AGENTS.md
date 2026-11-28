# Repository instructions

## Code reviews

- Use `$dotnet-code-review` for C# and .NET code reviews in this repository.
- Load it from `.codex/skills/dotnet-code-review/SKILL.md` and follow its workflow, severity levels, and output format.
- Use `$javascript-code-review` for JavaScript and Chrome Extension code reviews in this repository.
- Load it from `.codex/skills/javascript-code-review/SKILL.md` and follow its workflow, severity levels, and output format.
- When the versioned `.githooks/pre-push` hook starts either `codex review`, review exactly the base selected by that command.

## Where the documentation lives

- Product and technical documentation is in Plane, project CDB, under Pages. Read the page `Start here` first: https://app.plane.so/cartedebucate/projects/79a91058-4927-469c-a296-cf8b04f74799/pages/78c4cfc5-c607-4e3a-9ed4-5d688b2bb089
- `Start here` holds the working rules, the page templates and the index of all pages. The page `Architecture` describes the system as it is today.
- The code is the source of truth. When a page and the code disagree, trust the code and say so.

## How a change is made

- A feature is a change to the product. Each change gets its own pages in Plane, written in this order: PRD (behaviour, no technology), HLD (high-level design), DTD (detailed technical design), then a task breakdown as sub-items of the work item.
- Each step is marked Ready by a person before the next one starts. Do not implement from a page whose status is Draft.
- Small changes such as bug fixes and chores skip these steps.
- Work items are in Plane. Start only on items in the state "Ready for dev", and name the work item (for example CDB-49) in the branch or pull request.

## Pull requests

- Every code change goes through a pull request. Do not push to `main`.
- An agent merges a pull request only when a person tells it to merge that pull request. Never enable auto-merge.

## Secrets

- Never write secret values into code, documentation, work items or pull requests. Name the setting and say where it is set.
