---
name: testing-strategy
description: Generate tests following project conventions. Use when writing unit tests, integration tests, creating test fixtures, or implementing test builders. Triggers on requests like "write tests for", "add test coverage", "create test builder", "test this handler", or "implement tests".
---

# Testing Strategy

Generate and maintain tests following project-specific conventions for this .NET/EF Core DDD codebase.

## Quick Reference

### Test Pyramid

```
        +-------------+
        |  App Layer  |  Primary: Integration tests with SQLite in-memory
        |  Handlers   |  IAsyncLifetime pattern for isolation
        +-------------+
        |  Domain     |  Secondary: Unit tests for complex logic only
        |  Logic      |  Skip simple getters/setters
        +-------------+
```

### Key Decisions

- **NO E2E tests** - Application layer integration tests provide sufficient coverage
- **SQLite in-memory** - Not EF Core InMemory provider (real constraints matter)
- **IAsyncLifetime** - Each test method gets fresh DB instance
- **Builder pattern** - Fluent builders with sensible defaults
- **Japanese test names** - Describe behavior clearly

## Test Implementation Guide

### Step 1: Determine Test Type

| Layer | Test Type | When to Test |
|-------|-----------|--------------|
| Application (CommandHandler) | Integration | Always |
| Application (QueryHandler) | Integration | Always |
| Domain (Aggregate) | Unit | Complex state transitions only |
| Domain (Service) | Unit | Complex business rules only |
| Infrastructure | Skip | Covered by Application tests |

### Step 2: Create Test Class

See [references/patterns.md](references/patterns.md) for complete templates:
- Application Layer: IAsyncLifetime pattern with SQLite
- Domain Layer: Simple unit test class

### Step 3: Use Builders for Test Data

See [references/builders.md](references/builders.md) for all available builders.

Key rules:
- Single entity: use defaults
- Multiple entities: **MUST specify unique IDs**

### Step 4: Write Test Methods

See [references/examples.md](references/examples.md) for common scenarios:
- CommandHandler tests
- QueryHandler with related data
- Domain state transitions
- Exception testing

## Resources

| File | Content |
| ---- | ------- |
| [patterns.md](references/patterns.md) | Templates, builders, examples, anti-patterns |
| [packages.md](references/packages.md) | NuGet packages and xUnit config |

## Anti-Patterns Summary

| Anti-Pattern | Correct Approach |
|--------------|------------------|
| Shared DbContext | IAsyncLifetime with fresh DB |
| Constructor init | InitializeAsync per test |
| Default IDs for multiple entities | Explicit ID per entity |
| Testing simple getters | Only test complex logic |
| E2E tests | Application layer integration |
