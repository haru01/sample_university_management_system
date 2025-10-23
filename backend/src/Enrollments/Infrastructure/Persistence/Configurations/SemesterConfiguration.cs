using Enrollments.Domain.SemesterAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Enrollments.Infrastructure.Persistence.Configurations;

public class SemesterConfiguration : IEntityTypeConfiguration<Semester>
{
    public void Configure(EntityTypeBuilder<Semester> builder)
    {
        builder.ToTable("semesters");

        // 複合主キー（SemesterId）
        builder.HasKey(s => s.Id);

        // SemesterIdの変換（年度と期間を個別のカラムにマッピング）
        builder.Property(s => s.Id)
            .HasConversion(
                v => v.Year + "|" + v.Period, // DBに保存する形式
                v => CreateSemesterId(v))
            .HasColumnName("id")
            .IsRequired();

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

    private static SemesterId CreateSemesterId(string value)
    {
        var parts = value.Split('|');
        return new SemesterId(int.Parse(parts[0]), parts[1]);
    }
}
