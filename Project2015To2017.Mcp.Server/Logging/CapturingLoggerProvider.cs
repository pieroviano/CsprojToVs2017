using System.Collections.Concurrent;
using System.Text;
using Microsoft.Extensions.Logging;

namespace Project2015To2017.Mcp.Server.Logging;

/// <summary>
/// Captures all log output produced during a single migration operation so it can be
/// returned in the MCP tool result. Nothing is written to Console — on stdio transport
/// stdout is reserved for the JSON-RPC protocol.
/// </summary>
public sealed class CaptureScope : IDisposable
{
	private readonly ConcurrentQueue<string> lines = new();
	private volatile bool hasError;

	public bool HasError => hasError;

	internal void Append(LogLevel level, string message, Exception? ex)
	{
		if (level >= LogLevel.Error)
		{
			hasError = true;
		}

		var sb = new StringBuilder()
			.Append('[').Append(level).Append("] ")
			.Append(message);
		if (ex != null)
		{
			sb.Append(" :: ").Append(ex.GetType().Name).Append(": ").Append(ex.Message);
		}

		lines.Enqueue(sb.ToString());
	}

	public string Drain() => string.Join(Environment.NewLine, lines);

	public void Dispose()
	{
		// Buffer is released together with the scope instance.
	}
}

/// <summary>
/// <see cref="ILoggerProvider"/> that funnels every log entry produced by
/// <c>MigrationFacility</c> into the currently active <see cref="CaptureScope"/>. Because
/// migration operations are serialized (see <c>MigrationTools</c>), a single "current" scope
/// is sufficient; if concurrent operations are ever required, replace the field with an
/// <see cref="AsyncLocal{T}"/>.
/// </summary>
public sealed class CapturingLoggerProvider : ILoggerProvider
{
	private CaptureScope? current;

	public CaptureScope BeginScope()
	{
		var scope = new CaptureScope();
		current = scope;
		return scope;
	}

	public ILogger CreateLogger(string categoryName) => new CapturingLogger(() => current);

	public void Dispose()
	{
	}

	private sealed class CapturingLogger : ILogger
	{
		private readonly Func<CaptureScope?> scope;

		public CapturingLogger(Func<CaptureScope?> scope) => this.scope = scope;

		public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;

		public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Information;

		public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
			Func<TState, Exception?, string> formatter)
		{
			if (!IsEnabled(logLevel))
			{
				return;
			}

			scope()?.Append(logLevel, formatter(state, exception), exception);
		}

		private sealed class NullScope : IDisposable
		{
			public static readonly NullScope Instance = new();
			public void Dispose() { }
		}
	}
}
