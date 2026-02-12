# Cutube Code Conventions

## .NET Conventions

### Naming Conventions

#### Classes
- **PascalCase** for all class names
- Examples: `DownloadService`, `YtDlpHelper`, `ValidationResult`

#### Methods
- **PascalCase** for all method names
- Examples: `DownloadVideo()`, `ValidateUrl()`, `ProcessDownload()`

#### Properties/Fields
- **PascalCase** for public properties
- **_camelCase** for private fields
- Examples: `VideoUrl`, `StartTime`, `_httpClient`

#### Interfaces
- **PascalCase** prefixed with `I`
- Examples: `IYtDlpService`, `IProcessService`, `IFileService`

#### Parameters
- **camelCase** for all parameters
- Examples: `videoUrl`, `startTime`, `outputPath`

### Code Style

#### Nullable Reference Types
- **Enabled** across all projects
- Use `?` for nullable types: `string?`, `DownloadResult?`
- Use `!` null-forgiving operator when appropriate

#### ImplicitUsings
- **Enabled** across all projects
- Reduces using statements
- Global usings configured in some projects

#### Access Modifiers
- **Explicit** for class members (private, public, internal)
- Default access modifier: **private** (not explicit)
- **internal** for shared library types

### Modern C# Patterns

#### Pattern Matching
```csharp
// Preferred
if (result is { Success: true, Data: not null })
{
    // Handle success
}

// Over
if (result.Success && result.Data != null)
{
    // Handle success
}
```

#### File-Scoped Namespaces
```csharp
// Preferred (used in codebase)
namespace Cutube.Domain.Models;

public class DownloadRequest
{
    // ...
}
```

#### Primary Constructors
- **Check usage**: Not consistently applied
- **Recommendation**: Apply where appropriate for simple models

#### Records
- **Status**: Not widely used
- **Recommendation**: Consider for immutable models (DTOs, messages)

### LINQ Style

#### Query Syntax vs Method Syntax
- **Method syntax** preferred (more common in codebase)
- Query syntax acceptable for complex joins

#### Example
```csharp
// Preferred
var downloads = await context.Downloads
    .Where(d => d.Status == DownloadStatus.Completed)
    .OrderByDescending(d => d.CreatedAt)
    .ToListAsync();

// Acceptable for complex queries
var results = from d in context.Downloads
              join u in context.Users on d.UserId equals u.Id
              where d.Status == DownloadStatus.Completed
              select new { d, u.UserName };
```

## Frontend Conventions (Next.js)

### File Naming

#### Components
- **PascalCase** for component files
- Examples: `DownloadForm.tsx`, `VideoPlayer.tsx`, `ProgressBar.tsx`

#### Utilities/Helpers
- **camelCase** for utility files
- Examples: `formatDate.ts`, `cn.ts` (className utilities)

#### Pages
- **lowercase** for page files (Next.js App Router)
- Examples: `page.tsx`, `layout.tsx`, `loading.tsx`

### Component Patterns

#### Function Components
- **Functional components with hooks** (standard)
- **No class components**

#### Component Structure
```tsx
// 1. Imports
import { useState } from 'react';

// 2. Types/interfaces
interface MyComponentProps {
  // ...
}

// 3. Component
export function MyComponent({ prop1, prop2 }: MyComponentProps) {
  // 3a. Hooks
  const [state, setState] = useState();

  // 3b. Handlers
  const handleClick = () => {
    // ...
  };

  // 3c. Effects
  useEffect(() => {
    // ...
  }, []);

  // 3d. Render
  return (
    <div>...</div>
  );
}
```

### Naming Conventions

#### Components
- **PascalCase** for component names
- Examples: `DownloadForm`, `VideoPlayer`, `ProgressBar`

#### Hooks
- **camelCase** prefixed with `use`
- Examples: `useDownloads`, `useSignalR`, `useVideoPlayer`

#### Utilities
- **camelCase** for utility functions
- Examples: `formatDate`, `cn`, `formatDuration`

#### Types/Interfaces
- **PascalCase** for types
- Examples: `DownloadRequest`, `VideoMetadata`, `ApiResponse`

### TypeScript Style

#### Type Imports
```tsx
// Preferred
import type { DownloadRequest } from '@/types';

// Also acceptable
import { DownloadRequest } from '@/types';
```

#### Type Definitions
- **Interfaces** for public APIs, component props
- **Types** for unions, intersections, maps

#### Example
```tsx
// Interface for component props
interface ButtonProps {
  variant?: 'primary' | 'secondary';
  onClick: () => void;
}

// Type for utility return
type DownloadStatus = 'pending' | 'downloading' | 'completed' | 'error';
```

### React Patterns

#### State Management
- **React Query** for server state
- **useState** for local component state
- **useContext** for global UI state (theme, etc.)

#### Data Fetching
```tsx
// Preferred (React Query)
const { data, isLoading, error } = useQuery({
  queryKey: ['downloads'],
  queryFn: fetchDownloads,
});

// Fallback (useState + useEffect)
const [downloads, setDownloads] = useState<Download[]>([]);
useEffect(() => {
  fetchDownloads().then(setDownloads);
}, []);
```

#### Forms
- **React Hook Form** for form state
- **Zod** for validation schema

#### Example
```tsx
const form = useForm<FormData>({
  resolver: zod(schema),
  defaultValues: {
    url: '',
    format: 'mp4',
  },
});
```

### Styling Conventions

#### Tailwind CSS
- **Utility-first** approach
- **Responsive**: mobile-first (base → md → lg)
- **States**: `hover:`, `focus:`, `active:`

#### Component Variants
- **cva** (class-variance-authority) for variant styles
- **cn()** utility for merging classes

#### Example
```tsx
// Using cva for variants
const buttonVariants = cva({
  base: 'rounded font-medium transition',
  variants: {
    variant: {
      primary: 'bg-blue-600 text-white hover:bg-blue-700',
      secondary: 'bg-gray-200 text-gray-900 hover:bg-gray-300',
    },
  },
});

// Merging with cn()
<Button className={cn(buttonVariants({ variant: 'primary' }), 'mt-4')} />
```

#### Custom CSS
- **Avoid**: Use Tailwind utilities
- **Exception**: Complex animations, third-party component overrides

## Git Conventions

### Commit Messages

**Format**: `<type>: <description>`

#### Types
- `feat:` - New feature
- `fix:` - Bug fix
- `refactor:` - Code refactoring
- `test:` - Tests
- `docs:` - Documentation
- `chore:` - Build/dependencies

#### Examples
```
feat: add flexible time input parsing (1h30m, 90s, etc)
fix: handle invalid YouTube URLs
refactor: extract validation logic to separate service
test: add unit tests for TimeHelper
docs: update README with new features
chore: upgrade to .NET 10.0
```

### Branch Naming

**Format**: `<type>/<name>`

#### Examples
- `feature/add-audio-download`
- `fix/progress-bar-updates`
- `refactor/extract-domain-layer`

## Testing Conventions

### .NET Tests

#### Test Framework
- **xUnit** (inferred from existing tests)
- **Shouldly** or **FluentAssertions** (verify)

#### Test Naming
- **MethodUnderTest_Scenario_ExpectedOutcome**
- Examples:
  - `DownloadAsync_ValidUrl_ReturnsSuccessResult`
  - `ParseTime_InvalidFormat_ThrowsValidationException`

#### Test Structure
```csharp
public class TimeHelperTests
{
    [Fact]
    public void ParseSeconds_ValidInput_ReturnsCorrectTimeSpan()
    {
        // Arrange
        var input = "90";
        var expected = TimeSpan.FromMinutes(1.5);

        // Act
        var result = TimeHelper.ParseSeconds(input);

        // Assert
        result.Should().Be(expected);
    }
}
```

### Playwright Tests (Frontend)

#### Test Organization
- **Page model pattern** for reusable interactions
- **Feature folders** for test grouping

#### Test Naming
- **Descriptive** sentence-style names
- Examples:
  - `user can download video`
  - `progress bar updates during download`

#### Example
```typescript
test('user can download video', async ({ page }) => {
  await page.goto('/');
  await page.fill('[data-testid="video-url"]', 'https://youtube.com/watch?v=...');
  await page.click('[data-testid="download-button"]');
  await expect(page.locator('[data-testid="progress-bar"]')).toBeVisible();
});
```

## Documentation Conventions

### XML Documentation Comments
- **Status**: Inconsistent usage
- **Recommendation**: Add to public APIs

#### Example
```csharp
/// <summary>
/// Downloads a video from YouTube using yt-dlp.
/// </summary>
/// <param name="videoUrl">The URL of the video to download.</param>
/// <param name="outputPath">The directory where the video will be saved.</param>
/// <returns>A <see cref="DownloadResult"/> containing the download status.</returns>
public async Task<DownloadResult> DownloadAsync(string videoUrl, string outputPath)
{
    // ...
}
```

### Code Comments
- **Avoid**: Commenting what code does
- **Use**: Explaining **why** code does something
- **Exception**: Complex business logic rules

## Conventions Gaps

### Missing Conventions

1. **Async/Await**
   - No clear pattern for ConfigureAwait(false)
   - InconsistentConfigureAwait usage

2. **Cancellation Tokens**
   - Not consistently passed to async methods
   - Missing in long-running operations

3. **Error Logging**
   - No structured logging pattern
   - Inconsistent log levels

4. **Configuration**
   - No options pattern standardization
   - Inconsistent configuration injection

5. **Dependency Injection**
   - No standard service lifetime pattern
   - Mix of singleton, scoped, transient

## Recommendations

1. **Create Coding Standards Document**
   - Document these conventions
   - Add to developer onboarding

2. **Enforce with Tooling**
   - **StyleCop Analyzers** for C#
   - **ESLint** with strict rules (already configured)
   - **Prettier** for formatting (already configured)

3. **Code Review Checklist**
   - Verify conventions in PRs
   - Automated checks where possible

4. **Refactor for Consistency**
   - Gradually apply missing conventions
   - Technical debt tasks for cleanup
