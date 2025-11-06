using Enrollments.Domain.CourseAggregate;
using Enrollments.Domain.CourseOfferingAggregate;
using Enrollments.Domain.SemesterAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Enrollments.Infrastructure.Persistence.Configurations;

public class CourseOfferingConfiguration : IEntityTypeConfiguration<CourseOffering>
{
    public void Configure(EntityTypeBuilder<CourseOffering> builder)
    {
        builder.ToTable("course_offerings");

        // 主キー (OfferingId値オブジェクト)
        builder.HasKey(co => co.Id);
        builder.Property(co => co.Id)
            .HasColumnName("offering_id")
            .HasConversion(
                id => id.Value,
                value => new OfferingId(value))
            .ValueGeneratedNever(); // 手動で生成

        // CourseCode (値オブジェクト)
        builder.Property(co => co.CourseCode)
            .HasColumnName("course_code")
            .HasMaxLength(10)
            .IsRequired()
            .HasConversion(
                cc => cc.Value,
                value => new CourseCode(value));

        // SemesterId (値オブジェクト)
        builder.Property(co => co.SemesterId)
            .HasColumnName("semester_id")
            .HasMaxLength(50)
            .IsRequired()
            .HasConversion(
                sid => $"{sid.Year}-{sid.Period}",
                value => ParseSemesterId(value));

        // その他のプロパティ
        builder.Property(co => co.Credits)
            .HasColumnName("credits")
            .IsRequired();

        builder.Property(co => co.MaxCapacity)
            .HasColumnName("max_capacity")
            .IsRequired();

        builder.Property(co => co.Instructor)
            .HasColumnName("instructor")
            .HasMaxLength(100);

        builder.Property(co => co.Status)
            .HasColumnName("status")
            .HasMaxLength(20)
            .HasConversion<string>()
            .IsRequired();

        // ユニーク制約: CourseCode + SemesterId
        builder.HasIndex(co => new { co.CourseCode, co.SemesterId })
            .IsUnique()
            .HasDatabaseName("ix_course_offerings_course_semester");

        // 外部キー制約は設定しない（集約間の関係はリポジトリで管理）

        // ドメインイベントは永続化しない
        builder.Ignore(co => co.DomainEvents);
    }

    private static SemesterId ParseSemesterId(string value)
    {
        var parts = value.Split('-');
        return new SemesterId(int.Parse(parts[0]), parts[1]);
    }
}
