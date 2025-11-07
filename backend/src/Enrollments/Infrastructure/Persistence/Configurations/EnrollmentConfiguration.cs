using Enrollments.Domain.CourseOfferingAggregate;
using Enrollments.Domain.EnrollmentAggregate;
using Enrollments.Domain.StudentAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Enrollments.Infrastructure.Persistence.Configurations;

/// <summary>
/// Enrollment集約のEntity Framework設定
/// </summary>
public class EnrollmentConfiguration : IEntityTypeConfiguration<Enrollment>
{
    public void Configure(EntityTypeBuilder<Enrollment> builder)
    {
        builder.ToTable("enrollments");

        // 主キー
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .HasColumnName("enrollment_id")
            .HasConversion(
                id => id.Value,
                value => new EnrollmentId(value))
            .ValueGeneratedNever();

        // 外部キー - 値オブジェクト変換
        builder.Property(e => e.StudentId)
            .HasColumnName("student_id")
            .HasConversion(
                id => id.Value,
                value => new StudentId(value))
            .IsRequired();

        builder.Property(e => e.OfferingId)
            .HasColumnName("offering_id")
            .HasConversion(
                id => id.Value,
                value => new OfferingId(value))
            .IsRequired();

        // ステータス
        builder.Property(e => e.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .IsRequired();

        // タイムスタンプ
        builder.Property(e => e.EnrolledAt)
            .HasColumnName("enrolled_at")
            .IsRequired();

        builder.Property(e => e.CompletedAt)
            .HasColumnName("completed_at");

        builder.Property(e => e.CancelledAt)
            .HasColumnName("cancelled_at");

        // 一意制約 - 学生は同じコース開講に対して一度しか登録できない
        builder.HasIndex(e => new { e.StudentId, e.OfferingId })
            .IsUnique()
            .HasDatabaseName("ix_enrollments_student_offering");

        // インデックス: 学生IDでの検索用
        builder.HasIndex(e => e.StudentId)
            .HasDatabaseName("ix_enrollments_student_id");

        // インデックス: コース開講IDでの検索用
        builder.HasIndex(e => e.OfferingId)
            .HasDatabaseName("ix_enrollments_offering_id");

        // インデックス: ステータスでの検索用
        builder.HasIndex(e => e.Status)
            .HasDatabaseName("ix_enrollments_status");

        // StatusHistoryコレクション - 子エンティティとして設定
        builder.OwnsMany(e => e.StatusHistory, history =>
        {
            history.ToTable("enrollment_status_history");

            // 主キー
            history.HasKey("Id");
            history.Property(h => h.Id)
                .HasColumnName("history_id")
                .HasConversion(
                    id => id.Value,
                    value => new EnrollmentStatusHistoryId(value))
                .ValueGeneratedNever();

            // 外部キー（親への参照）
            history.WithOwner()
                .HasForeignKey("EnrollmentId");

            history.Property(h => h.EnrollmentId)
                .HasColumnName("enrollment_id")
                .HasConversion(
                    id => id.Value,
                    value => new EnrollmentId(value))
                .IsRequired();

            // ステータス
            history.Property(h => h.Status)
                .HasColumnName("status")
                .HasConversion<string>()
                .IsRequired();

            // 変更日時
            history.Property(h => h.ChangedAt)
                .HasColumnName("changed_at")
                .IsRequired();

            // 変更実行者
            history.Property(h => h.ChangedBy)
                .HasColumnName("changed_by")
                .HasMaxLength(100)
                .IsRequired();

            // 変更理由
            history.Property(h => h.Reason)
                .HasColumnName("reason")
                .HasMaxLength(1000);

            // メタデータ（JSON）
            history.Property(h => h.Metadata)
                .HasColumnName("metadata")
                .HasColumnType("jsonb");

            // インデックス
            history.HasIndex("EnrollmentId")
                .HasDatabaseName("ix_enrollment_status_history_enrollment_id");

            history.HasIndex(h => h.ChangedAt)
                .HasDatabaseName("ix_enrollment_status_history_changed_at");

            history.HasIndex(h => h.Status)
                .HasDatabaseName("ix_enrollment_status_history_status");
        });

        // DomainEventsは永続化しない
        builder.Ignore(e => e.DomainEvents);
    }
}
