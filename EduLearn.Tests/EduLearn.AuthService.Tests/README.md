# EduLearn AuthService — NUnit Tests

## Folder Structure

```
EduLearn.AuthService.Tests/
├── EduLearn.AuthService.Tests.csproj   ← test project file
│
├── Helpers/
│   └── TestHelpers.cs                  ← shared factory methods, fake config
│
├── UnitTests/
│   ├── JwtHelperTests.cs               ← 10 tests for JWT generation & validation
│   └── UserServiceTests.cs             ← 18 tests for business logic (mocked repo)
│
└── IntegrationTests/
    └── UserRepositoryTests.cs          ← 16 tests using EF Core InMemory DB
```

## What Is Tested

| File | Type | Tests |
|---|---|---|
| `JwtHelperTests.cs` | Unit | Token format, claims (UserId, Role, Email), validation |
| `UserServiceTests.cs` | Unit | Register, Login, GetUserById, UpdateProfile, ChangePassword, Deactivate, Google login |
| `UserRepositoryTests.cs` | Integration | CRUD, ExistsByEmail, FindByRole, Search, GoogleId lookup |

**Total: 44 tests**

## How to Run

### Step 1 — Open terminal in the `tests/EduLearn.AuthService.Tests` folder

```bash
cd tests/EduLearn.AuthService.Tests
```

### Step 2 — Restore packages

```bash
dotnet restore
```

### Step 3 — Run all tests

```bash
dotnet test
```

You will see output like:
```
Passed!  - Failed: 0, Passed: 44, Skipped: 0, Total: 44
```

### Step 4 — Run with detailed output (see each test name)

```bash
dotnet test --verbosity normal
```

### Step 5 — Run only Unit tests

```bash
dotnet test --filter "FullyQualifiedName~UnitTests"
```

### Step 6 — Run only Integration tests

```bash
dotnet test --filter "FullyQualifiedName~IntegrationTests"
```

### Step 7 — Run a specific test class

```bash
dotnet test --filter "ClassName=UserServiceTests"
```

## No Setup Required

- **No database needed** — Unit tests use Moq (mocked repo), Integration tests use EF Core InMemory
- **No PostgreSQL** — InMemory database replaces it for testing
- **No appsettings.json** — TestHelpers.CreateFakeConfig() provides fake config
- **No running services** — everything is self-contained

## Test Types Explained

| Type | Uses | When |
|---|---|---|
| Unit Test | Moq (fake repo) | Test business logic in isolation |
| Integration Test | EF Core InMemory | Test real DB queries work correctly |
