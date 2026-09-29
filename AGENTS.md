# Repository instructions

## Code reviews

- Use `$dotnet-code-review` for C# and .NET code reviews in this repository.
- Load it from `.agents/skills/dotnet-code-review/SKILL.md` and follow its workflow, severity levels, and output format.
- Use `$javascript-code-review` for JavaScript and Chrome Extension code reviews in this repository.
- Load it from `.codex/skills/javascript-code-review/SKILL.md` and follow its workflow, severity levels, and output format.
- When the versioned `.githooks/pre-push` hook starts either `codex review`, review exactly the base selected by that command.
