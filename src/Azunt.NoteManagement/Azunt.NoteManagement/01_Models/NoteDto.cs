namespace Azunt.NoteManagement;

/// <summary>
/// Note 엔터티를 외부 노출용으로 변환하는 DTO 클래스입니다.
/// </summary>
public class NoteDto
{
    public long Id { get; set; }
    public string? Name { get; set; }
    public string? Category { get; set; }
    public DateTimeOffset Created { get; set; }
    public string? CreatedBy { get; set; }
}