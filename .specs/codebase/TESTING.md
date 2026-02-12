# Cutube Testing Strategy

## Current State

### Test Coverage

**.NET Projects**:
- ✅ `Cutube.Tests` - CLI tests
- ✅ `Cutube.Domain.Tests` - Domain tests
- ✅ `Cutube.Api.Tests` - API tests
- ✅ `Cutube.Worker.Tests` - Worker tests

**Frontend**:
- ✅ `cutube-web/e2e/` - Playwright E2E tests

**Gaps**:
- ❌ No integration test project
- ❌ No explicit test coverage reports
- ❌ No performance/load testing

## Testing Frameworks

### .NET

#### Unit Tests
- **Framework**: xUnit (inferred from project structure)
- **Assertions**: Shouldly or FluentAssertions (verify)
- **Mocking**: Moq? (check usage)
- **Runner**: `dotnet test` or `dotnet vstest`

#### Test Organization
```
[Project].Tests/
├── [Feature]/          # Feature-based organization
│   ├── When_[scenario].cs
│   └── [ClassName]_tests.cs
│
└── [Layer]/           # Layer-based organization
    ├── Services/
    ├── Controllers/
    └── Models/
```

#### Example Structure
```csharp
namespace Cutube.Domain.Tests.Services;

public class TimeHelperTests
{
    public class ParseSeconds
    {
        [Fact]
        public void Returns_time_span_for_valid_input()
        {
            // Arrange
            var input = "90";

            // Act
            var result = TimeHelper.ParseSeconds(input);

            // Assert
            result.Should().Be(TimeSpan.FromMinutes(1.5));
        }

        [Theory]
        [InlineData("")]
        [InlineData("abc")]
        public void Throws_exception_for_invalid_input(string input)
        {
            // Act
            var act = () => TimeHelper.ParseSeconds(input);

            // Assert
            act.Should().Throw<ValidationException>();
        }
    }
}
```

### Frontend (Playwright)

#### E2E Tests
- **Framework**: Playwright 1.58.2
- **Config**: `playwright.config.ts`
- **Reports**: `playwright-report/`
- **Test Results**: `test-results/`

#### Test Organization
```
cutube-web/e2e/
├── [feature].spec.ts     # Feature-based tests
├── pages/               # Page models (optional)
└── fixtures/            # Test data, helpers
```

#### Example Test
```typescript
import { test, expect } from '@playwright/test';

test.describe('Video Download', () => {
  test('user can download a video', async ({ page }) => {
    await page.goto('/');
    await page.fill('[data-testid="video-url"]', 'https://youtube.com/watch?v=...');
    await page.click('[data-testid="download-button"]');

    await expect(page.locator('[data-testid="progress-bar"]')).toBeVisible();
    await expect(page.locator('[data-testid="download-complete"]')).toBeVisible({ timeout: 60000 });
  });

  test('shows error for invalid URL', async ({ page }) => {
    await page.goto('/');
    await page.fill('[data-testid="video-url"]', 'invalid-url');
    await page.click('[data-testid="download-button"]');

    await expect(page.locator('[data-testid="error-message"]')).toContainText('Invalid URL');
  });
});
```

#### Accessibility Testing
- **@axe-core/playwright** for accessibility audits
- Integrated into Playwright tests

## Test Categories

### Unit Tests

**Purpose**: Test individual components in isolation.

**.NET Scope**:
- ✅ Domain models (validation, logic)
- ✅ Services (business logic)
- ✅ Helpers (utilities)
- ❌ Controllers (prefer integration tests)
- ❌ External dependencies (mocked)

**Frontend Scope**:
- ❌ Component unit tests (not currently implemented)
- ✅ E2E tests cover component behavior

**Example**:
```csharp
[Fact]
public async Task DownloadAsync_ValidUrl_ReturnsSuccess()
{
    // Arrange
    var mockYtDlp = new Mock<IYtDlpService>();
    mockYtDlp.Setup(x => x.DownloadAsync(It.IsAny<string>(), It.IsAny<string>()))
           .ReturnsAsync(Result.Success(...));

    var service = new DownloadService(mockYtDlp.Object);

    // Act
    var result = await service.DownloadAsync("https://youtube.com/watch?v=...");

    // Assert
    result.IsSuccess.Should().BeTrue();
}
```

### Integration Tests

**Purpose**: Test component interactions.

**Current Status**: ❌ **NOT IMPLEMENTED**

**Recommended Scope**:
- API endpoints with real dependencies
- Worker with RabbitMQ (Testcontainers?)
- Database operations (when added)
- File system operations

**Example** (recommended):
```csharp
public class DownloadEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task PostDownload_ValidRequest_QueuesMessage()
    {
        // Arrange
        var client = _factory.CreateClient();
        var request = new DownloadRequest { Url = "..." };

        // Act
        var response = await client.PostAsJsonAsync("/api/downloads", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
    }
}
```

**Tools to Consider**:
- **WebApplicationFactory** (ASP.NET Core)
- **Testcontainers** (RabbitMQ, PostgreSQL)
- **WireMock.Net** (external API mocking)

### End-to-End Tests

**Purpose**: Test full user flows.

**Current Status**: ✅ **IMPLEMENTED** (Playwright)

**Scope**:
- ✅ Download flow (URL → progress → completion)
- ✅ Error handling (invalid URL, network failures)
- ✅ UI interactions (forms, buttons, navigation)
- ❌ Multi-user scenarios (concurrent downloads)
- ❌ Real-time updates (SignalR)

**Example** (current):
```typescript
test('complete download flow', async ({ page }) => {
  await page.goto('/');
  await page.fill('[data-testid="url-input"]', TEST_VIDEO_URL);
  await page.click('[data-testid="download-button"]');

  // Verify progress updates
  await expect(page.locator('[data-testid="progress"]')).toHaveText(/downloading/);

  // Verify completion
  await expect(page.locator('[data-testid="status"]')).toHaveText(/completed/);
});
```

### Performance Tests

**Purpose**: Test system performance under load.

**Current Status**: ❌ **NOT IMPLEMENTED**

**Recommended Scenarios**:
- Concurrent downloads (10, 100 users)
- RabbitMQ message throughput
- API response times
- Memory usage over time

**Tools to Consider**:
- **BenchmarkDotNet** (.NET microbenchmarks)
- **K6** or **Locust** (load testing)
- **Application Insights** (production monitoring)

## Testing Best Practices

### General Principles

1. **Test Behavior, Not Implementation**
   - Focus on what the code does, not how
   - Avoid testing private methods
   - Use public APIs only

2. **Arrange-Act-Assert**
   - Clear separation in test structure
   - One act, multiple asserts acceptable
   - Descriptive test names

3. **Test Independence**
   - No shared state between tests
   - Tests can run in any order
   - Clean up after each test

4. **Fast Feedback**
   - Unit tests: < 100ms each
   - Integration tests: < 5s each
   - E2E tests: < 30s each

### .NET Specific

#### Naming Conventions
- **Unit tests**: `Method_Scenario_ExpectedOutcome`
- **Integration tests**: `Feature_Scenario_ExpectedOutcome`

#### Example
```csharp
// ✅ Good
public async Task DownloadAsync_ValidUrl_ReturnsSuccess()

// ❌ Bad
public async Task Test1()
```

#### Test Data
- **Inline** for simple tests
- **MemberData** for parameterized tests
- **Theory** for data-driven tests

#### Mocking
- Mock only external dependencies
- Don't mock the system under test
- Verify mock interactions (when important)

### Frontend Specific

#### Selectors
- **data-testid** attributes for test selectors
- Avoid CSS classes, text content (fragile)

#### Example
```tsx
// ✅ Good
<button data-testid="download-button">Download</button>
await page.click('[data-testid="download-button"]');

// ❌ Bad
<button className="bg-blue-500">Download</button>
await page.click('.bg-blue-500');
```

#### Page Objects
- Extract reusable interactions
- Keep tests readable
- Single source of truth for selectors

#### Example
```typescript
class DownloadPage {
  constructor(private page: Page) {}

  async enterUrl(url: string) {
    await this.page.fill('[data-testid="url-input"]', url);
  }

  async startDownload() {
    await this.page.click('[data-testid="download-button"]');
  }
}
```

## Current Test Gaps

### Missing Coverage

1. **Error Scenarios**
   - Network failures
   - Invalid video URLs
   - File system errors
   - RabbitMQ downtime

2. **Edge Cases**
   - Very long videos
   - Special characters in filenames
   - Concurrent downloads
   - Large file sizes

3. **Real-time Features**
   - SignalR connection lifecycle
   - Progress updates
   - Reconnection scenarios

4. **Data Persistence**
   - No database tests (not implemented yet)
   - File system cleanup
   - Download history queries

### Recommended Test Projects

1. **Cutube.Integration.Tests**
   - API endpoints with real dependencies
   - Worker with Testcontainers
   - File system operations

2. **Cutube.Performance.Tests**
   - BenchmarkDotNet benchmarks
   - Load tests with K6

3. **cutube-web/unit-tests** (optional)
   - Component unit tests with React Testing Library
   - Hook tests
   - Utility tests

## Quality Gates

### Pre-commit
```bash
# .NET
dotnet test --filter "Category=Unit"  # Fast unit tests only

# Frontend
pnpm test:unit  # If unit tests added
```

### Pre-push
```bash
# .NET
dotnet test  # All tests

# Frontend
pnpm test:e2e  # Playwright
```

### CI/CD
```bash
# Run all tests
dotnet test --collect:"XPlat Code Coverage"  # With coverage
pnpm test:e2e
```

## Test Coverage Goals

### Current Status
- **Unknown** - No coverage reports configured

### Recommended Targets
- **Unit tests**: 80%+ coverage
- **Integration tests**: 70%+ coverage
- **E2E tests**: Critical user paths only

### Tools to Add
- **Coverlet** (coverage collector)
- **ReportGenerator** (coverage reports)
- **GitHub Actions** integration

## Testing Documentation

### Current State
- ❌ No testing documentation
- ❌ No test writing guidelines
- ❌ No test run instructions

### Recommended Additions

1. **TESTING.md**
   - How to run tests
   - Test organization
   - Writing guidelines

2. **Code Comments**
   - Document complex test scenarios
   - Explain why certain tests exist

3. **Test Plans**
   - Feature test plans
   - Regression test lists

## Test Maintenance

### Current Issues
- Tests may be flaky (network dependencies)
- E2E tests may be slow
- No test data management strategy

### Recommendations

1. **Flaky Test Detection**
   - Retriable tests in xUnit
   - Flaky test detection in CI

2. **Test Data Management**
   - Fixed test videos (short, reliable)
   - Mock yt-dlp responses (where possible)
   - Testcontainers for dependencies

3. **Test Performance**
   - Parallel test execution
   - Faster test setup/teardown
   - Selective test running

## Next Steps

1. **Add Integration Tests**
   - Set up WebApplicationFactory
   - Add Testcontainers for RabbitMQ
   - Test API endpoints end-to-end

2. **Improve Coverage**
   - Add Coverlet for coverage
   - Set coverage minimums (80%)
   - Block PRs under coverage

3. **Document Testing**
   - Create TESTING.md
   - Add test writing guidelines
   - Document test scenarios

4. **Add Performance Tests**
   - Benchmark critical paths
   - Load test API
   - Monitor in production

5. **Stabilize E2E Tests**
   - Use test data, not real videos
   - Mock external services where possible
   - Add retry logic for flaky tests
