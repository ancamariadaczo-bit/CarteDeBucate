# Repository instructions

## Code reviews

- Use `$dotnet-code-review` for every code review in this repository.
- Load it from `.agents/skills/dotnet-code-review/SKILL.md` and follow its workflow, severity levels, and output format.
- When the versioned `.githooks/pre-push` hook starts `codex review`, review exactly the base selected by that command.
