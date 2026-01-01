# Slow Query Patterns

Common EF Core performance anti-patterns and their fixes.

## Critical Patterns

### N+1 Query Problem

**Detection**: Loop containing await with repository/context call

```csharp
// BAD: N+1 queries (1 + N database calls)
var enrollments = await _enrollmentRepository.SelectByStudentAsync(studentId);
foreach (var enrollment in enrollments)
{
    var offering = await _courseOfferingRepository.GetByIdAsync(enrollment.OfferingId);
    var course = await _courseRepository.GetByCodeAsync(offering.CourseCode);
}
```

**Fix Option 1**: Use Include for eager loading
```csharp
// GOOD: Single query with joins
var enrollments = await _context.Enrollments
    .Include(e => e.CourseOffering)
        .ThenInclude(co => co.Course)
    .Where(e => e.StudentId == studentId)
    .AsNoTracking()
    .ToListAsync();
```

**Fix Option 2**: Batch load with IN clause
```csharp
// GOOD: 3 queries instead of 1 + N + N
var enrollments = await _enrollmentRepository.SelectByStudentAsync(studentId);
var offeringIds = enrollments.Select(e => e.OfferingId).ToList();
var offerings = await _context.CourseOfferings
    .Where(co => offeringIds.Contains(co.Id))
    .ToDictionaryAsync(co => co.Id);
```

---

### In-Memory Aggregation

**Detection**: ToListAsync followed by .Max/.Min/.Count/.Sum

```csharp
// BAD: Loads all records into memory
var offerings = await _context.CourseOfferings.ToListAsync();
var maxId = offerings.Max(co => co.Id.Value);
```

**Fix**: Use database-level aggregation
```csharp
// GOOD: Aggregation in database
var maxId = await _context.CourseOfferings
    .Select(co => co.Id.Value)
    .DefaultIfEmpty(0)
    .MaxAsync();
```

---

## Warning Patterns

### Unbounded Result Sets

**Detection**: ToListAsync without Take/Skip/pagination

```csharp
// BAD: Could return millions of rows
var students = await _context.Students.ToListAsync();
```

**Fix**: Add pagination or limits
```csharp
// GOOD: Paginated query
var students = await _context.Students
    .OrderBy(s => s.Id)
    .Skip(page * pageSize)
    .Take(pageSize)
    .ToListAsync();
```

---

### Missing Index for Frequent Queries

**Detection**: Queries filtering on non-indexed columns

Check if these columns have indexes:
- Foreign keys (StudentId, OfferingId, etc.)
- Status fields used in WHERE clauses
- Date fields used for range queries
- Email/unique identifiers

**Fix**: Add index in Configuration
```csharp
// In EntityConfiguration.cs
builder.HasIndex(e => e.StudentId)
    .HasDatabaseName("ix_enrollments_student_id");

// For partial indexes (PostgreSQL)
builder.HasIndex(e => new { e.StudentId, e.OfferingId })
    .HasFilter("status != 'Cancelled'")
    .IsUnique();
```

---

## Info Patterns

### Missing AsNoTracking

**Detection**: Read-only queries without AsNoTracking

```csharp
// SUBOPTIMAL: EF tracks entities unnecessarily
var students = await _context.Students
    .Where(s => s.Grade == grade)
    .ToListAsync();
```

**Fix**: Add AsNoTracking for read-only queries
```csharp
// BETTER: No change tracking overhead
var students = await _context.Students
    .AsNoTracking()
    .Where(s => s.Grade == grade)
    .ToListAsync();
```

---

### Cartesian Explosion with Multiple Includes

**Detection**: Multiple .Include() on collections

```csharp
// BAD: Cartesian product explosion
var offerings = await _context.CourseOfferings
    .Include(co => co.Enrollments)
    .Include(co => co.ClassSessions)
    .ToListAsync();
```

**Fix Option 1**: Split queries
```csharp
// BETTER: Separate queries, merged in memory
var offerings = await _context.CourseOfferings
    .AsSplitQuery()
    .Include(co => co.Enrollments)
    .Include(co => co.ClassSessions)
    .ToListAsync();
```

**Fix Option 2**: Use projections
```csharp
// BEST: Only fetch needed data
var offerings = await _context.CourseOfferings
    .Select(co => new OfferingDto
    {
        Id = co.Id,
        EnrollmentCount = co.Enrollments.Count,
        NextSession = co.ClassSessions.OrderBy(cs => cs.Date).FirstOrDefault()
    })
    .ToListAsync();
```

---

## EF Core Logging Setup

Enable query logging to identify slow queries at runtime:

```csharp
// In Program.cs
services.AddDbContext<AppDbContext>(options =>
{
    options.UseNpgsql(connectionString);

    // Development only
    if (env.IsDevelopment())
    {
        options.LogTo(Console.WriteLine, LogLevel.Information);
        options.EnableSensitiveDataLogging();
        options.EnableDetailedErrors();
    }
});
```

Log slow queries only:
```csharp
options.LogTo(
    message => Console.WriteLine(message),
    new[] { DbLoggerCategory.Database.Command.Name },
    LogLevel.Information,
    DbContextLoggerOptions.DefaultWithLocalTime);
```
