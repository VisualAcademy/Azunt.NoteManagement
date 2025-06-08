using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;

namespace Azunt.NoteManagement
{
    /// <summary>
    /// Notes 테이블의 일부 스키마 컬럼을 점진적으로 확장하는 도우미 클래스
    /// </summary>
    public class NoteTableEnhancer
    {
        private readonly string _connectionString;
        private readonly ILogger<NoteTableEnhancer> _logger;

        public NoteTableEnhancer(string connectionString, ILogger<NoteTableEnhancer> logger)
        {
            _connectionString = connectionString;
            _logger = logger;
        }

        public void EnhanceNotesTable()
        {
            var columnsToEnsure = new Dictionary<string, string>
            {
                { "PostDate", "DATETIME NULL DEFAULT GETDATE()" },
                { "PostIp", "NVARCHAR(20) NULL" },
                { "ReadCount", "INT NULL DEFAULT 0" },
                { "Title", "NVARCHAR(512) NULL" }
            };

            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                foreach (var (columnName, columnDefinition) in columnsToEnsure)
                {
                    if (!ColumnExists(connection, "Notes", columnName))
                    {
                        var alterCommand = new SqlCommand($@"
                            ALTER TABLE [dbo].[Notes]
                            ADD [{columnName}] {columnDefinition};", connection);

                        alterCommand.ExecuteNonQuery();
                        _logger.LogInformation($"Column '{columnName}' added to Notes table.");
                    }
                }

                connection.Close();
            }
        }

        private bool ColumnExists(SqlConnection connection, string tableName, string columnName)
        {
            var checkCommand = new SqlCommand(@"
                SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS 
                WHERE TABLE_NAME = @TableName AND COLUMN_NAME = @ColumnName", connection);

            checkCommand.Parameters.AddWithValue("@TableName", tableName);
            checkCommand.Parameters.AddWithValue("@ColumnName", columnName);

            return (int)checkCommand.ExecuteScalar() > 0;
        }

        /// <summary>
        /// 서비스 프로바이더 기반으로 실행할 수 있는 편의 메서드
        /// </summary>
        public static void Run(IServiceProvider services, bool forMaster, string? optionalConnectionString = null)
        {
            try
            {
                var logger = services.GetRequiredService<ILogger<NoteTableEnhancer>>();
                var config = services.GetRequiredService<IConfiguration>();

                string connectionString;

                if (!string.IsNullOrWhiteSpace(optionalConnectionString))
                {
                    connectionString = optionalConnectionString;
                }
                else
                {
                    var tempConnectionString = config.GetConnectionString("DefaultConnection");
                    if (string.IsNullOrEmpty(tempConnectionString))
                        throw new InvalidOperationException("DefaultConnection is not configured in appsettings.json.");

                    connectionString = tempConnectionString;
                }

                var enhancer = new NoteTableEnhancer(connectionString, logger);
                enhancer.EnhanceNotesTable();
            }
            catch (Exception ex)
            {
                var fallbackLogger = services.GetService<ILogger<NoteTableEnhancer>>();
                fallbackLogger?.LogError(ex, "Error while enhancing Notes table schema.");
            }
        }
    }
}
