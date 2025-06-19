# JBDH Tests

This project contains unit tests for the JBDH application. The tests are organized to mirror the structure of the main project for easy navigation and maintenance.

## Project Structure

```
JBDH.Tests/
├── Controllers/           # Tests for MVC Controllers
├── Services/             # Tests for Business Logic Services
│   ├── UserManagement/   # User-related service tests
│   ├── JobManagement/    # Job-related service tests
│   └── Authentication/   # Authentication service tests
├── Models/               # Tests for Model classes
├── TestHelpers/          # Shared test utilities and data builders
└── README.md            # This file
```

## Test Framework

- **xUnit**: Primary testing framework
- **Moq**: Mocking framework for dependencies
- **Entity Framework In-Memory**: For testing database operations without a real database
- **Microsoft.NET.Test.Sdk**: Test discovery and execution

## Running Tests

### From Command Line

```bash
# Run all tests
dotnet test

# Run tests with verbose output
dotnet test --verbosity normal

# Run tests from a specific project
dotnet test JBDH.Tests

# Run tests with coverage (if configured)
dotnet test --collect:"XPlat Code Coverage"
```

### From Visual Studio

1. Open the solution in Visual Studio
2. Open Test Explorer (Test > Test Explorer)
3. Click "Run All" to run all tests
4. Or right-click on specific test classes/methods to run individual tests

## Test Naming Conventions

Tests follow the **Arrange-Act-Assert** pattern and use descriptive names:

```csharp
[Fact]
public async Task MethodName_Scenario_ExpectedBehavior()
{
    // Arrange - Setup test data and mocks
    
    // Act - Execute the method being tested
    
    // Assert - Verify the results
}
```

## Mocking Strategy

We use Moq to mock dependencies:

```csharp
// Setup mocks in constructor
private readonly Mock<IEmailService> _mockEmailService;

public MyServiceTests()
{
    _mockEmailService = new Mock<IEmailService>();
    _mockEmailService.Setup(x => x.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
        .ReturnsAsync(true);
}
```

## Database Testing

For database operations, we use Entity Framework's In-Memory provider:

```csharp
var options = new DbContextOptionsBuilder<ApplicationDbContext>()
    .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
    .Options;
var context = new ApplicationDbContext(options);
```

## Test Data Builders

Use the `TestDataBuilder` class for creating consistent test data:

```csharp
var user = TestDataBuilder.CreateValidRegisterUserViewModel();
var testUser = TestDataBuilder.CreateTestUser("testuser", "test@example.com");
```

## Best Practices

1. **Isolation**: Each test should be independent and not rely on other tests
2. **Descriptive Names**: Test method names should clearly describe what is being tested
3. **Single Responsibility**: Each test should verify one specific behavior
4. **Clean Setup**: Use constructors for common setup and dispose of resources properly
5. **Mock External Dependencies**: Don't rely on external services or databases in unit tests

## Adding New Tests

1. Create test files in the appropriate folder structure
2. Follow the naming convention: `{ClassName}Tests.cs`
3. Use the existing test patterns as templates
4. Add tests for both success and failure scenarios
5. Include edge cases and boundary conditions

## Test Categories

- **Unit Tests**: Test individual methods in isolation
- **Integration Tests**: Test interactions between components (if needed)
- **Controller Tests**: Test MVC controller actions
- **Service Tests**: Test business logic services

## Continuous Integration

Tests are automatically run in CI/CD pipelines to ensure code quality and prevent regressions.

## Troubleshooting

### Common Issues

1. **Missing Dependencies**: Ensure all required NuGet packages are installed
2. **Mock Setup**: Verify that all required mocks are properly configured
3. **Database Context**: Make sure in-memory database is properly initialized
4. **Async Tests**: Use `async/await` for asynchronous test methods

### Debugging Tests

1. Set breakpoints in test methods
2. Use `Debugger.Break()` for conditional debugging
3. Check test output for detailed error messages
4. Use `Console.WriteLine()` for temporary debugging output

## Contributing

When adding new features to the main project:

1. Write tests first (TDD approach)
2. Ensure all tests pass before committing
3. Maintain test coverage for critical business logic
4. Update this README if adding new testing patterns or tools 