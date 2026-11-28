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

These rules apply to every person and every agent, whatever tool or model is used.

A feature is a change to the product. A change that alters behaviour goes through four steps, in this order. Each step is marked Ready by a person before the next one starts.

1. PRD: how the change must behave for the user. No technology.
2. HLD: high-level design. Which parts of the system change and how they work together.
3. DTD: detailed technical design. Everything needed to implement it.
4. Task breakdown: sub-items under the work item in Plane. Each one is small and complete enough to be implemented on its own.

- The PRD, HLD and DTD are pages in Plane, named `[TYPE] YYYY-MM-DD Area: Change (CDB-N)`. Each starts with a status line that is either Draft or Ready.
- Do not implement from a page whose status is Draft.
- Small changes such as bug fixes and chores skip the four steps. They need only a work item with enough detail.
- If you find that a page and the code disagree, or that a page is missing something you need, stop and say so. Do not fill the gap with a guess.

## The board

Work items are in Plane, project CDB. They move through the columns from left to right.

| Column | Meaning | Who moves an item here |
| --- | --- | --- |
| Backlog | An idea or a need. Not specified yet. | Anyone |
| Grooming | Someone is writing the specification right now: PRD, HLD, DTD, sub-items. | The person or agent writing it |
| Ready for dev | Fully specified. Work may start. | A person, or an agent once a person has agreed |
| In Progress | Someone is implementing it. | The person or agent who starts |
| In Review | A pull request is open. | The person or agent who opened it |
| Done | The pull request is merged. | The person or agent who merged it |
| Won't Do | Dropped, with the reason in a comment. | A person only |

- An item enters Ready for dev only when its PRD, HLD and DTD are Ready and its sub-items exist. For a small change, the work item itself has enough detail instead.
- Start only on items in Ready for dev. Never start on an item in Backlog or Grooming, even if it looks simple.
- When you start, move the item to In Progress and assign it. One item, one owner.
- Work on one sub-item at a time. One sub-item is one pull request.
- Do not skip columns and do not move an item backwards without saying why in a comment.

## Pull requests

- Every code change goes through a pull request. Do not push to `main`.
- Name the work item in the branch and in the pull request title. The title starts with the work item and continues with a short name a person can read, for example `CDB-49 Forgot password`. The branch follows the same idea, for example `cdb-49-forgot-password`.
- Fill in the pull request template. Do not delete its checklist.
- When the pull request is open, move the work item to In Review. When it is merged, move it to Done.
- An agent merges a pull request only when a person tells it to merge that pull request. Never enable auto-merge.
- Do not change code outside the scope of the work item. Report what you noticed instead.

## Secrets

- Never write secret values into code, documentation, work items or pull requests. Name the setting and say where it is set.
