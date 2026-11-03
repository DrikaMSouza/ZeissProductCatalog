using System.Globalization;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using ProductCatalog.Application.Abstractions.Services;
using ProductCatalog.Application.Common.Exceptions;
using ProductCatalog.Infrastructure.Persistence;

namespace ProductCatalog.Infrastructure.IdGeneration;

/// <summary>
/// Allocates product ids from a SQL Server SEQUENCE (100000 - 999999, no cycle).
/// <para>
/// The sequence lives in the shared database, and <c>NEXT VALUE FOR</c> is atomic, so any number of
/// API instances can request ids concurrently without ever receiving the same value - no
/// coordination between instances and no "read max + 1" race. The primary key and a check
/// constraint on the table are a second line of defence.
/// </para>
/// </summary>
internal sealed class SqlSequenceProductIdGenerator(ProductCatalogDbContext dbContext) : IProductIdGenerator
{
    // "The sequence object has reached its minimum or maximum value."
    private const int SequenceExhausted = 11728;

    private static readonly string NextValueSql =
        $"SELECT NEXT VALUE FOR [{ProductCatalogDbContext.Schema}].[{ProductCatalogDbContext.ProductIdSequence}]";

    public async Task<int> NextIdAsync(CancellationToken cancellationToken)
    {
        var database = dbContext.Database;
        await database.OpenConnectionAsync(cancellationToken);

        try
        {
            await using var command = database.GetDbConnection().CreateCommand();
            command.CommandText = NextValueSql;
            command.Transaction = database.CurrentTransaction?.GetDbTransaction();

            var value = await command.ExecuteScalarAsync(cancellationToken);
            return Convert.ToInt32(value, CultureInfo.InvariantCulture);
        }
        catch (SqlException ex) when (ex.Number == SequenceExhausted)
        {
            throw new ProductIdExhaustedException(ex);
        }
        finally
        {
            await database.CloseConnectionAsync();
        }
    }
}
