# CODEX.md

## Purpose

This repository already contains documentation describing the project architecture, folder structure, entities, services, and API.

Do not duplicate or redefine that information.

Always use the existing implementation as the source of truth.

---

## Working Rules

Before making any changes:

1. Analyze the existing codebase.
2. Understand the current implementation.
3. Reuse existing components whenever possible.
4. Modify existing files instead of creating duplicate implementations.
5. Do not rewrite working code.
6. Follow the existing architecture, coding style, and project conventions.

---

## Development Guidelines

- Keep changes minimal and maintainable.
- Follow SOLID, DRY, and KISS principles.
- Prefer composition over duplication.
- Use dependency injection for new services.
- Follow existing naming conventions.
- Keep business logic out of controllers and UI.
- Use asynchronous APIs where appropriate.
- Support `CancellationToken` for long-running operations.
- Avoid unnecessary abstractions.

---

## Output Requirements

For every implementation:

1. Summarize the existing implementation relevant to the task.
2. Explain the planned changes.
3. Modify only the required files.
4. Do not regenerate unchanged files.
5. Keep the project in a compilable state after every change.

---

## If Information Is Missing

Do not guess.

State what is missing and request the required information before implementing.

---

## Golden Rule

**Analyze first. Reuse second. Modify third. Create new only when necessary.**