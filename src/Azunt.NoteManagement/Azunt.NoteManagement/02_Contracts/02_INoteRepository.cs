﻿using Azunt.Models.Common;

namespace Azunt.NoteManagement;

/// <summary>
/// Note 전용 확장 저장소 인터페이스 - 페이징, 검색 기능 포함
/// </summary>
public interface INoteRepository : INoteRepositoryBase
{
    /// <summary>
    /// 페이징 + 검색 기능 제공
    /// </summary>
    Task<ArticleSet<Note, int>> GetAllAsync<TParentIdentifier>(
        int pageIndex, int pageSize, string searchField, string searchQuery, string sortOrder, TParentIdentifier parentIdentifier, string category = "");
    Task<bool> MoveUpAsync(long id);
    Task<bool> MoveDownAsync(long id);
}