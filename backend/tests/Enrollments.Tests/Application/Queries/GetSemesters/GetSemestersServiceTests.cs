using System;
using Enrollments.Application.Queries.GetSemesters;
using Enrollments.Domain.SemesterAggregate;
using Enrollments.Infrastructure.Persistence;
using Enrollments.Infrastructure.Persistence.Repositories;
using Enrollments.Tests.Builders;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Enrollments.Tests.Application.Queries.GetSemesters;

/// <summary>
/// GetSemestersService - 学期一覧を取得できる
/// </summary>
public class GetSemestersServiceTests
{
    private DbContextOptions<CoursesDbContext> CreateNewContextOptions()
    {
        return new DbContextOptionsBuilder<CoursesDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
    }

    [Fact]
    public async Task 全学期を取得する()
    {
        /// <summary>
        /// Scenario: 全学期を取得する
        /// </summary>
        // Arrange - Given SemesterRepositoryに以下のSemesterが存在する
        var options = CreateNewContextOptions();
        await using var context = new CoursesDbContext(options);
        var repository = new SemesterRepository(context);
                // Setup test data: 3 semesters
        var semester1 = Semester.Create(
          2024,
          "Spring",
          new DateTime(2024, 4, 1),
          new DateTime(2024, 9, 30)
        );
        var semester2 = Semester.Create(
          2024,
          "Fall",
          new DateTime(2024, 10, 1),
          new DateTime(2025, 3, 31)
        );
        var semester3 = Semester.Create(
          2023,
          "Fall",
          new DateTime(2023, 10, 1),
          new DateTime(2024, 3, 31)
        );
        await repository.AddAsync(semester1);
        await repository.AddAsync(semester2);
        await repository.AddAsync(semester3);
        await repository.SaveChangesAsync();


        var service = new GetSemestersService(repository);

        // When GetSemestersQueryを実行する
        var query = new GetSemestersQuery
        {
        };

        // Act
        var result = await service.GetSemestersAsync(query);

        // Assert - Then & And
        // Then 3件のSemesterDtoが返される
        Assert.NotNull(result);
        Assert.Equal(3, result.Count);
        
        // And Yearの降順、Periodの降順でソートされている（最新が先頭）
        Assert.Equal(2024, result[0].Year);
        Assert.Equal("Fall", result[0].Period);
        Assert.Equal(2024, result[1].Year);
        Assert.Equal("Spring", result[1].Period);
        Assert.Equal(2023, result[2].Year);
        Assert.Equal("Fall", result[2].Period);
    }

    [Fact]
    public async Task 現在の学期のみを取得する()
    {
        /// <summary>
        /// Scenario: 現在の学期のみを取得する
        /// </summary>
        // Arrange - Given SemesterRepositoryに複数のSemesterが存在する
        var options = CreateNewContextOptions();
        await using var context = new CoursesDbContext(options);
        var repository = new SemesterRepository(context);
                // Setup test data: multiple semesters
        var currentSemester = Semester.Create(
          2024,
          "Spring",
          new DateTime(2024, 4, 1),
          new DateTime(2024, 9, 30)
        );
        var futureSemester = Semester.Create(
          2024,
          "Fall",
          new DateTime(2024, 10, 1),
          new DateTime(2025, 3, 31)
        );
        var pastSemester = Semester.Create(
          2023,
          "Fall",
          new DateTime(2023, 10, 1),
          new DateTime(2024, 3, 31)
        );
        await repository.AddAsync(currentSemester);
        await repository.AddAsync(futureSemester);
        await repository.AddAsync(pastSemester);
        await repository.SaveChangesAsync();


        var service = new GetSemestersService(repository);

        // When GetSemestersQueryを実行する（CurrentOnly: true）
        var query = new GetSemestersQuery
        {
            CurrentOnly = true,
            CurrentDate = new DateTime(2024, 5, 15),
        };

        // Act
        var result = await service.GetSemestersAsync(query);

        // Assert - Then & And
        // Then 2024年SpringのSemesterDtoのみが返される
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal(2024, result[0].Year);
        Assert.Equal("Spring", result[0].Period);
        
        // And StartDate <= 現在日時 <= EndDate を満たす
        Assert.True(result[0].StartDate <= new DateTime(2024, 5, 15));
        Assert.True(result[0].EndDate >= new DateTime(2024, 5, 15));
    }

    [Fact]
    public async Task 学期が1件も登録されていない場合()
    {
        /// <summary>
        /// Scenario: 学期が1件も登録されていない場合
        /// </summary>
        // Arrange - Given SemesterRepositoryにSemesterが存在しない
        var options = CreateNewContextOptions();
        await using var context = new CoursesDbContext(options);
        var repository = new SemesterRepository(context);
        await context.SaveChangesAsync();

        var service = new GetSemestersService(repository);

        // When GetSemestersQueryを実行する
        var query = new GetSemestersQuery
        {
        };

        // Act
        var result = await service.GetSemestersAsync(query);

        // Assert - Then & And
        // Then 空のリストが返される
        Assert.NotNull(result);
        Assert.Empty(result);
    }
}