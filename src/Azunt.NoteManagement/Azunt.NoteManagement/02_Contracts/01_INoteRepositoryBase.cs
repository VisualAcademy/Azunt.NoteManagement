using Azunt.Repositories;

namespace Azunt.NoteManagement;

/// <summary>
/// 기본 CRUD 작업을 위한 Note 전용 저장소 인터페이스
/// </summary>
public interface INoteRepositoryBase : IRepositoryBase<Note, long>
{
}