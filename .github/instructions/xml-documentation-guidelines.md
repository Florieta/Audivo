applyTo: '**/*.cs'
description: "Standards for writing clear, consistent, and useful XML documentation comments in the Audivo .NET backend."
---
# XML Documentation Guidelines for Audivo Backend

## Instructions

Your primary directive is to generate clear, accurate, and helpful XML documentation comments for all public and important internal code. Documentation must explain intent, behavior, and usage — not just restate the code.

Write XML comments for:
- Public classes
- Interfaces
- Public methods
- Controllers and API endpoints
- DTOs and Models
- Enums
- Important internal services and complex logic

Avoid documenting trivial private methods unless the logic is non-obvious.

---

## General Rules

- Use clear and simple language.
- Describe **what the code does and why**, not how.
- Keep comments concise but informative.
- Keep documentation synchronized with the code.
- Do not copy method names into descriptions.
- Do not write useless comments like "Gets value".
- Use proper grammar and full sentences.
- Always document async behavior, side effects, and important constraints.

---

## Required XML Tags

### Summary

Every documented element must include a `<summary>`.

- One short paragraph describing purpose and responsibility.
- Must be meaningful and readable in IntelliSense.

```csharp
/// <summary>
/// Provides operations for managing user audiobook collections,
/// including adding, removing, and retrieving saved audiobooks.
/// </summary>
public class LibraryService
