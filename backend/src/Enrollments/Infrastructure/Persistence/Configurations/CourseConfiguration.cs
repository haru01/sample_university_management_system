using Enrollments.Domain.CourseAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Enrollments.Infrastructure.Persistence.Configurations;

public class CourseConfiguration : IEntityTypeConfiguration<Course>
{
    public void Configure(EntityTypeBuilder<Course> builder)
    {
        builder.ToTable("courses");

        // 主キー（CourseCode）
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Id)
            .HasConversion(
                v => v.Value,
                v => new CourseCode(v))
            .HasColumnName("code")
            .HasMaxLength(10)
            .IsRequired();

        // その他のプロパティ
        builder.Property(c => c.Name)
            .HasColumnName("name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(c => c.Credits)
            .HasColumnName("credits")
            .IsRequired();

        builder.Property(c => c.MaxCapacity)
            .HasColumnName("max_capacity")
            .IsRequired();

        // ドメインイベントは永続化しない
        builder.Ignore(c => c.DomainEvents);
    }
}
