﻿using Azunt.Models.Common;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace Azunt.NoteManagement;

public class NoteRepositoryDapper : INoteRepository
{
    private readonly string _connectionString;
    private readonly ILogger<NoteRepositoryDapper> _logger;

    public NoteRepositoryDapper(string connectionString, ILoggerFactory loggerFactory)
    {
        _connectionString = connectionString;
        _logger = loggerFactory.CreateLogger<NoteRepositoryDapper>();
    }

    private SqlConnection GetConnection() => new(_connectionString);

    public async Task<Note> AddAsync(Note model)
    {
        const string sql = @"
            INSERT INTO Notes (Active, Created, CreatedBy, Name, Category, IsDeleted)
            OUTPUT INSERTED.Id
            VALUES (@Active, @Created, @CreatedBy, @Name, @Category, 0)";

        model.Created = DateTimeOffset.UtcNow;

        using var conn = GetConnection();
        model.Id = await conn.ExecuteScalarAsync<long>(sql, model);
        return model;
    }

    public async Task<IEnumerable<Note>> GetAllAsync()
    {
        const string sql = @"
            SELECT Id, Active, Created, CreatedBy, Name, Category
            FROM Notes
            WHERE IsDeleted = 0
            ORDER BY DisplayOrder";

        using var conn = GetConnection();
        return await conn.QueryAsync<Note>(sql);
    }

    public async Task<Note> GetByIdAsync(long id)
    {
        const string sql = @"
            SELECT Id, Active, Created, CreatedBy, Name, Category
            FROM Notes
            WHERE Id = @Id AND IsDeleted = 0";

        using var conn = GetConnection();
        return await conn.QuerySingleOrDefaultAsync<Note>(sql, new { Id = id }) ?? new Note();
    }

    public async Task<bool> UpdateAsync(Note model)
    {
        const string sql = @"
            UPDATE Notes SET
                Active = @Active,
                Name = @Name,
                Category = @Category
            WHERE Id = @Id AND IsDeleted = 0";

        using var conn = GetConnection();
        var affected = await conn.ExecuteAsync(sql, model);
        return affected > 0;
    }

    public async Task<bool> DeleteAsync(long id)
    {
        const string sql = @"
            UPDATE Notes SET IsDeleted = 1
            WHERE Id = @Id AND IsDeleted = 0";

        using var conn = GetConnection();
        var affected = await conn.ExecuteAsync(sql, new { Id = id });
        return affected > 0;
    }

    public async Task<ArticleSet<Note, int>> GetAllAsync<TParentIdentifier>(
        int pageIndex,
        int pageSize,
        string searchField,
        string searchQuery,
        string sortOrder,
        TParentIdentifier parentIdentifier,
        string category = "")
    {
        var items = new List<Note>();
        int totalCount = 0;

        using var conn = GetConnection();

        var whereClauses = new List<string> { "IsDeleted = 0" };
        var parameters = new DynamicParameters();

        if (!string.IsNullOrWhiteSpace(searchQuery))
        {
            whereClauses.Add("Name LIKE @SearchQuery");
            parameters.Add("@SearchQuery", "%" + searchQuery + "%");
        }

        if (!string.IsNullOrWhiteSpace(category))
        {
            whereClauses.Add("Category = @Category");
            parameters.Add("@Category", category);
        }

        string where = string.Join(" AND ", whereClauses);
        string orderBy = sortOrder switch
        {
            "Name" => "ORDER BY Name",
            "NameDesc" => "ORDER BY Name DESC",
            "DisplayOrder" => "ORDER BY DisplayOrder",
            _ => "ORDER BY DisplayOrder"
        };

        const string countSql = "SELECT COUNT(*) FROM Notes WHERE " + "{0}";
        string countCommand = string.Format(countSql, where);

        string dataSql = $@"
            SELECT Id, Active, Created, CreatedBy, Name, Category
            FROM Notes
            WHERE {where}
            {orderBy}
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

        parameters.Add("@Offset", pageIndex * pageSize);
        parameters.Add("@PageSize", pageSize);

        totalCount = await conn.ExecuteScalarAsync<int>(countCommand, parameters);
        var result = await conn.QueryAsync<Note>(dataSql, parameters);

        items = result.ToList();

        return new ArticleSet<Note, int>(items, totalCount);
    }

    public async Task<bool> MoveUpAsync(long id)
    {
        const string getCurrent = "SELECT Id, DisplayOrder FROM Notes WHERE Id = @Id AND IsDeleted = 0";
        const string getUpper = @"
            SELECT TOP 1 Id, DisplayOrder
            FROM Notes
            WHERE DisplayOrder < @DisplayOrder AND IsDeleted = 0
            ORDER BY DisplayOrder DESC";
        const string update = "UPDATE Notes SET DisplayOrder = @DisplayOrder WHERE Id = @Id";

        using var conn = GetConnection();
        await conn.OpenAsync();

        var current = await conn.QuerySingleOrDefaultAsync<(long Id, int DisplayOrder)>(getCurrent, new { Id = id });
        if (current.Id == 0) return false;

        var upper = await conn.QuerySingleOrDefaultAsync<(long Id, int DisplayOrder)>(getUpper, new { DisplayOrder = current.DisplayOrder });
        if (upper.Id == 0) return false;

        using var tx = conn.BeginTransaction();
        try
        {
            await conn.ExecuteAsync(update, new { DisplayOrder = upper.DisplayOrder, Id = current.Id }, tx);
            await conn.ExecuteAsync(update, new { DisplayOrder = current.DisplayOrder, Id = upper.Id }, tx);
            await tx.CommitAsync();
            return true;
        }
        catch
        {
            await tx.RollbackAsync();
            return false;
        }
    }

    public async Task<bool> MoveDownAsync(long id)
    {
        const string getCurrent = "SELECT Id, DisplayOrder FROM Notes WHERE Id = @Id AND IsDeleted = 0";
        const string getLower = @"
            SELECT TOP 1 Id, DisplayOrder
            FROM Notes
            WHERE DisplayOrder > @DisplayOrder AND IsDeleted = 0
            ORDER BY DisplayOrder ASC";
        const string update = "UPDATE Notes SET DisplayOrder = @DisplayOrder WHERE Id = @Id";

        using var conn = GetConnection();
        await conn.OpenAsync();

        var current = await conn.QuerySingleOrDefaultAsync<(long Id, int DisplayOrder)>(getCurrent, new { Id = id });
        if (current.Id == 0) return false;

        var lower = await conn.QuerySingleOrDefaultAsync<(long Id, int DisplayOrder)>(getLower, new { DisplayOrder = current.DisplayOrder });
        if (lower.Id == 0) return false;

        using var tx = conn.BeginTransaction();
        try
        {
            await conn.ExecuteAsync(update, new { DisplayOrder = lower.DisplayOrder, Id = current.Id }, tx);
            await conn.ExecuteAsync(update, new { DisplayOrder = current.DisplayOrder, Id = lower.Id }, tx);
            await tx.CommitAsync();
            return true;
        }
        catch
        {
            await tx.RollbackAsync();
            return false;
        }
    }
}