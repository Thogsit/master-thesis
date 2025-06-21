using System;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;

namespace SealedFga.Database;

/// <summary>
///     Singleton instance for SealedFGA DB access.
///     This is mainly thought to be used internally.
///     So think twice before using it directly; you probably shouldn't.
/// </summary>
public class SealedFgaDb {
    public static SealedFgaDb Instance => LazyInstance.Value;

    private const string DbFileName = "SealedFga.db";
    private const string ConnectionString = $"Data Source={DbFileName};Mode=ReadWriteCreate;Pooling=True;";
    private static readonly Lazy<SealedFgaDb> LazyInstance = new(() => new SealedFgaDb());

    /// <summary>
    ///     Makes sure the database is created and initialized.
    /// </summary>
    private SealedFgaDb() {
        InitializeDatabase();
    }

    /// <summary>
    ///     Can be used to open a connection to the database.
    /// </summary>
    /// <example>
    ///     <code>
    ///     using var connection = SealedFgaDb.Instance.OpenConnection();
    ///     using var cmd = new SqliteCommand("DROP DATABASE;", connection);
    ///     cmd.ExecuteNonQuery();
    ///     </code>
    /// </example>
    /// <returns>An already opened SQLite DB connection.</returns>
    public SqliteConnection OpenConnection() {
        var connection = new SqliteConnection(ConnectionString);
        connection.Open();
        return connection;
    }

    /// <summary>
    ///     Enqueues a new FGA operation to be processed later.
    /// </summary>
    /// <param name="operation"></param>
    public void AddFgaOperation(FgaOperation operation) {
        using var connection = OpenConnection();
        const string insertSql = """
                                 INSERT INTO fga_queue (operation_type, user_val, relation_val, object_val)
                                 VALUES (@operationType, @userVal, @relationVal, @objectVal);
                                 """;

        using var cmd = new SqliteCommand(insertSql, connection);
        cmd.Parameters.AddWithValue("@operationType", operation.OperationType);
        cmd.Parameters.AddWithValue("@userVal", operation.RawUser);
        cmd.Parameters.AddWithValue("@relationVal", operation.Relation);
        cmd.Parameters.AddWithValue("@objectVal", operation.RawObject);

        cmd.ExecuteNonQuery();
    }

    /// <summary>
    ///     Enqueues multiple FGA operations to be processed later.
    /// </summary>
    /// <param name="operations">The list of FGA operations to add</param>
    public void AddFgaOperations(IEnumerable<FgaOperation> operations) {
        using var connection = OpenConnection();
        using var transaction = connection.BeginTransaction();

        try {
            const string insertSql = """
                                     INSERT INTO fga_queue (operation_type, user_val, relation_val, object_val)
                                     VALUES (@operationType, @userVal, @relationVal, @objectVal);
                                     """;

            using var cmd = new SqliteCommand(insertSql, connection, transaction);

            // Prepare parameters once
            var operationTypeParam = cmd.Parameters.Add("@operationType", SqliteType.Text);
            var userValParam = cmd.Parameters.Add("@userVal", SqliteType.Text);
            var relationValParam = cmd.Parameters.Add("@relationVal", SqliteType.Text);
            var objectValParam = cmd.Parameters.Add("@objectVal", SqliteType.Text);

            cmd.Prepare();

            foreach (var operation in operations) {
                operationTypeParam.Value = operation.OperationType;
                userValParam.Value = operation.RawUser;
                relationValParam.Value = operation.Relation;
                objectValParam.Value = operation.RawObject;

                cmd.ExecuteNonQuery();
            }

            transaction.Commit();
        } catch {
            transaction.Rollback();
            throw;
        }
    }

    /// <summary>
    ///     Makes sure the database is created and initialized.
    /// </summary>
    private void InitializeDatabase() {
        using var connection = OpenConnection();

        const string createTableSql = $"""
                                       CREATE TABLE IF NOT EXISTS fga_meta (
                                           id INTEGER PRIMARY KEY DEFAULT 42,
                                           version INTEGER NOT NULL DEFAULT 0
                                       );

                                       INSERT OR IGNORE INTO fga_meta DEFAULT VALUES;

                                       CREATE TABLE IF NOT EXISTS fga_queue (
                                           id INTEGER PRIMARY KEY,
                                           -- Operation related data
                                           operation_type TEXT NOT NULL CHECK ( operation_type IN ( '{FgaOperationType.Write}', '{FgaOperationType.Delete}' ) ),
                                           user_val TEXT NOT NULL,
                                           relation_val TEXT NOT NULL,
                                           object_val TEXT NOT NULL,
                                           -- Retry related data with microsecond precision
                                           created_at TEXT NOT NULL DEFAULT (strftime('%Y-%m-%d %H:%M:%f', 'now')),
                                           next_retry_at TEXT NOT NULL DEFAULT (strftime('%Y-%m-%d %H:%M:%f', 'now')),
                                           last_error TEXT,
                                           attempt_count INTEGER DEFAULT 0,
                                           status TEXT DEFAULT '{FgaOperationStatus.Pending}' CHECK ( status IN ( '{FgaOperationStatus.Success}', '{FgaOperationStatus.Failure}', '{FgaOperationStatus.Pending}' ) )
                                       );

                                       -- Optimizes "Which to retry next" queries
                                       CREATE INDEX IF NOT EXISTS idx_status_retry
                                       ON fga_queue(status, next_retry_at);

                                       CREATE INDEX IF NOT EXISTS idx_created_at
                                       ON fga_queue(created_at);
                                       """;

        using var cmd = new SqliteCommand(createTableSql, connection);
        cmd.ExecuteNonQuery();
    }
}
