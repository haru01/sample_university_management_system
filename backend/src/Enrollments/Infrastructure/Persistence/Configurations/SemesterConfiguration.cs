using Enrollments.Domain.SemesterAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Enrollments.Infrastructure.Persistence.Configurations;

public class SemesterConfiguration : IEntityTypeConfiguration<Semester>
{
    public void Configure(EntityTypeBuilder<Semester> builder)
    {
        builder.ToTable("semesters");

        // バッキングフィールドを使用して複合キーをマッピング
        builder.Property("_idYear")
            .HasColumnName("id_year")
            .IsRequired();

        builder.Property("_idPeriod")
            .HasColumnName("id_period")
            .HasMaxLength(20)
            .IsRequired();

        // 複合主キー定義
        builder.HasKey("_idYear", "_idPeriod");

        // Idプロパティ - 読み取り専用としてマッピング（バッキングフィールドから計算）
        builder.Ignore(s => s.Id);

        // その他のプロパティ
        builder.Property(s => s.StartDate)
            .HasColumnName("start_date")
            .IsRequired();

        builder.Property(s => s.EndDate)
            .HasColumnName("end_date")
            .IsRequired();

        // ドメインイベントは永続化しない
        builder.Ignore(s => s.DomainEvents);
    }
}
