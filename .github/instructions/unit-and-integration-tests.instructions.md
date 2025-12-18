---
applyTo: '**/*.cs'
---
# Rules for Unit and Integration Tests

This rule defines the best practices and tools to use for writing unit and integration tests in the backend project.

## Libraries and Tools
- **Test Framework**: Always use `xunit.v3` for unit and integration tests.
- **Assertions**: Use `FluentAssertions` version 7 for assertions.
- **Mocks / Fakes**: Do not use mock libraries. Use hand-made Fakes where necessary.

## General Steps
1. **Unit Tests**:
   - Write tests for each valid and invalid scenario.
   - Verify exceptions and expected results.

## Unit Tests
- Located in `[project].UnitTests/`.
- Do not interact with real databases or external services.
- Focus on business logic, use cases, and validation.

## Best Practices
- Always write tests before implementation (TDD).
- Document test cases in the corresponding files.
- Use explicit test names to describe their purpose.

## Additional Rule: Continuous Test Execution

- After every significant change or addition, run `dotnet test` or `dotnet watch` to verify the current state of the project.
- This ensures that all tests pass and helps identify issues early in the development process.
- Document the results of the test runs in the corresponding task or user story.

## Updates
This rule must be updated if new tools or practices are adopted in the backend project.