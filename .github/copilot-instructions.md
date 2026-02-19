applyTo: '*'
description: "Global code quality and architecture standards for the Audivo audiobook platform using .NET (C#), React, and MS SQL."
---
# Audivo Global Code Quality Guidelines

## Instructions

Your primary directive is to produce clean, maintainable, scalable, and production-ready code for the Audivo audiobook platform. Always prioritize readability, correctness, performance, and long-term maintainability. Follow the rules below across backend (.NET), frontend (React), and database (MS SQL).

---

## 1. General Code Quality

- Write clear, readable, and self-explanatory code.
- Prefer simple solutions over complex ones.
- Follow existing project conventions and architecture.
- Use meaningful and consistent naming.
- Keep functions small and focused on one responsibility.
- Avoid duplication — reuse existing logic.
- Remove unused code, imports, and variables.
- Comments must explain **why**, not what.
- Avoid magic numbers — use constants or configuration.
- Always leave the code cleaner than before.

---

## 2. Architecture & Design

- Follow Clean Architecture principles.
- Separate concerns: API / Application / Domain / Infrastructure.
- Keep business logic independent from UI and database.
- Prefer composition over inheritance.
- Avoid tight coupling between components.
- Use Dependency Injection properly (.NET).
- Public APIs must be stable, clear, and documented.

---

## 3. .NET / C# Backend Rules

- Follow SOLID principles.
- Use async/await end-to-end — avoid blocking calls.
- Validate all inputs and handle edge cases.
- Never trust client data.
- Use DTOs for API boundaries.
- Use Entity Framework efficiently (no N+1 queries).
- Use CancellationToken in async operations.
- Use structured logging (ILogger).
- Do not swallow exceptions — handle or propagate correctly.
- Secure endpoints with authentication & authorization.
- Never hardcode secrets — use environment/configuration.

---

## 4. React Frontend Rules

- Use functional components and hooks.
- Keep components small and reusable.
- Separate UI, state, and business logic.
- Avoid unnecessary re-renders.
- Handle loading, empty, and error states properly.
- Validate user input before sending to backend.
- Never trust frontend-only validation.
- Keep UI responsive and accessible.
- Follow consistent folder and component structure.
- Avoid hardcoded API URLs — use configuration/env.

---

## 5. MS SQL / Database Rules

- Use proper normalization unless justified.
- Use indexes where performance requires.
- Avoid SELECT * — always specify columns.
- Use parameterized queries / ORM to prevent SQL injection.
- Maintain clear table relationships and constraints.
- Store only necessary data — avoid redundancy.
- Track user progress, bookmarks, and library efficiently.
- Use migrations for schema changes — never manual production edits.
- Ensure queries are optimized and scalable.

---

## 6. Performance & Scalability

- Optimize only when necessary — measure first.
- Avoid unnecessary allocations and heavy operations.
- Stream large data (audio files) efficiently.
- Use pagination for large datasets.
- Avoid blocking I/O and synchronous database calls.
- Cache only when it provides clear benefit.

---

## 7. Security & Reliability

- Validate and sanitize all inputs.
- Never expose sensitive data.
- Use HTTPS and secure authentication (JWT/Identity).
- Protect against common vulnerabilities (SQL Injection, XSS, CSRF).
- Handle failures gracefully — no silent crashes.
- Log important events and errors with context.

---

## 8. Testability

- Write testable, deterministic code.
- Avoid hidden dependencies and global state.
- Keep business logic independent from infrastructure.
- Ensure new public logic can be unit tested.
- Mock only external dependencies.

---

## 9. Consistency

- Follow the same naming, formatting, and structure across backend, frontend, and database.
- Keep code predictable and easy to navigate.
- Use shared models/contracts where appropriate.
- Maintain consistent API design and response format.

---

## General Behavior

- Prefer safe, maintainable, and production-ready solutions.
- Explain why changes improve code quality when refactoring.
- Do not introduce unnecessary complexity.
- Avoid breaking changes unless required.
- Focus on long-term maintainability and scalability of Audivo.
