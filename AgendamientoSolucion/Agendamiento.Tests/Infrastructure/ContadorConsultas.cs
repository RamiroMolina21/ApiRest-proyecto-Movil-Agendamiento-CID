using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Data.Common;

namespace Agendamiento.Tests.Infrastructure;

/// <summary>Interceptor de EF Core que cuenta los comandos SQL ejecutados (lecturas y escrituras).</summary>
public sealed class ContadorConsultas : DbCommandInterceptor
{
    private int _total;

    public int Total => Volatile.Read(ref _total);

    public void Reiniciar() => Interlocked.Exchange(ref _total, 0);

    public override DbDataReader ReaderExecuted(DbCommand command, CommandExecutedEventData eventData, DbDataReader result)
    { Interlocked.Increment(ref _total); return result; }

    public override ValueTask<DbDataReader> ReaderExecutedAsync(DbCommand command, CommandExecutedEventData eventData, DbDataReader result, CancellationToken cancellationToken = default)
    { Interlocked.Increment(ref _total); return ValueTask.FromResult(result); }

    public override object? ScalarExecuted(DbCommand command, CommandExecutedEventData eventData, object? result)
    { Interlocked.Increment(ref _total); return result; }

    public override ValueTask<object?> ScalarExecutedAsync(DbCommand command, CommandExecutedEventData eventData, object? result, CancellationToken cancellationToken = default)
    { Interlocked.Increment(ref _total); return ValueTask.FromResult(result); }

    public override int NonQueryExecuted(DbCommand command, CommandExecutedEventData eventData, int result)
    { Interlocked.Increment(ref _total); return result; }

    public override ValueTask<int> NonQueryExecutedAsync(DbCommand command, CommandExecutedEventData eventData, int result, CancellationToken cancellationToken = default)
    { Interlocked.Increment(ref _total); return ValueTask.FromResult(result); }
}