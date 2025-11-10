using Enrollments.Domain.StudentAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shared.ValueObjects;

namespace Enrollments.Infrastructure.Persistence.Configurations;

public class StudentConfiguration : IEntityTypeConfiguration<Student>
{
    public void Configure(EntityTypeBuilder<Student> builder)
    {
        builder.ToTable("students");

        // 主キー（StudentId）
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Id)
            .HasConversion(
                v => v.Value,
                v => new StudentId(v))
            .HasColumnName("id")
            .IsRequired();

        // その他のプロパティ
        builder.Property(s => s.Name)
            .HasColumnName("name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(s => s.Email)
            .HasColumnName("email")
            .HasMaxLength(200)
            .IsRequired();

        builder.HasIndex(s => s.Email).IsUnique();

        builder.Property(s => s.Grade)
            .HasColumnName("grade")
            .IsRequired();

        // ドメインイベントは永続化しない
        builder.Ignore(s => s.DomainEvents);
    }
}
