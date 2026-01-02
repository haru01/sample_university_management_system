# Test NuGet Packages

Required packages for test projects.

## Package References

Add to test `.csproj` files:

```xml
<ItemGroup>
  <!-- Test Framework -->
  <PackageReference Include="xunit" Version="2.9.3" />
  <PackageReference Include="xunit.runner.visualstudio" Version="3.1.5">
    <PrivateAssets>all</PrivateAssets>
    <IncludeAssets>runtime; build; native; contentfiles; analyzers</IncludeAssets>
  </PackageReference>
  <PackageReference Include="Microsoft.NET.Test.Sdk" Version="18.0.0" />

  <!-- SQLite In-Memory DB -->
  <PackageReference Include="Microsoft.Data.Sqlite" Version="9.0.10" />
  <PackageReference Include="Microsoft.EntityFrameworkCore.Sqlite" Version="9.0.10" />

  <!-- Coverage -->
  <PackageReference Include="coverlet.collector" Version="6.0.4">
    <PrivateAssets>all</PrivateAssets>
    <IncludeAssets>runtime; build; native; contentfiles; analyzers</IncludeAssets>
  </PackageReference>

  <!-- Mocking (use sparingly) -->
  <PackageReference Include="Moq" Version="4.20.72" />
</ItemGroup>
```

## xUnit Runner Configuration

Create `xunit.runner.json` in test project root:

```json
{
  "$schema": "https://xunit.net/schema/current/xunit.runner.schema.json",
  "parallelizeAssembly": true,
  "parallelizeTestCollections": true,
  "maxParallelThreads": -1
}
```

Add to `.csproj`:

```xml
<ItemGroup>
  <None Update="xunit.runner.json">
    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
  </None>
</ItemGroup>
```

## Why SQLite over EF Core InMemory?

| Feature | SQLite | EF Core InMemory |
|---------|--------|------------------|
| UNIQUE constraints | Enforced | Ignored |
| FOREIGN KEY constraints | Enforced | Ignored |
| NULL constraints | Enforced | Ignored |
| Transaction behavior | Realistic | Simplified |
| Closer to PostgreSQL | Yes | No |

SQLite catches constraint violations that InMemory would miss, leading to more reliable tests.

## Running Tests

```bash
# All tests
dotnet test

# By category
dotnet test --filter "Category=Unit"
dotnet test --filter "Category=Integration"

# Specific test class
dotnet test --filter "FullyQualifiedName~CreateStudentCommandHandlerTests"

# With coverage
dotnet test --collect:"XPlat Code Coverage"
```
