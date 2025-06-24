using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Azunt.NoteManagement.Models.Configurations
{
    public class NoteConfiguration : IEntityTypeConfiguration<Note>
    {
        public void Configure(EntityTypeBuilder<Note> builder)
        {
            // 매핑할 테이블명 지정
            builder.ToTable("Notes");

            // 기본 키
            builder.HasKey(n => n.Id);

            // Id 자동 증가 설정
            builder.Property(n => n.Id)
                   .ValueGeneratedOnAdd();

            // Created 기본값 지정 (현재 시각)
            builder.Property(n => n.Created)
                   .HasDefaultValueSql("GetDate()");

            // Name: 길이 제한
            builder.Property(n => n.Name)
                   .HasMaxLength(255);

            // FileName: 길이 제한
            builder.Property(n => n.FileName)
                   .HasMaxLength(255);

            // Title: NVarChar(255)로 지정 및 필수 여부
            builder.Property(n => n.Title)
                   .HasColumnType("NVarChar(255)")
                   .HasMaxLength(255);

            // Content: 옵션 필드이지만 NVARCHAR(MAX)로 기본 처리됨 (명시하지 않음)

            // 기타 필드들에 대해서도 필요한 경우 추가 설정 가능
        }
    }
}
