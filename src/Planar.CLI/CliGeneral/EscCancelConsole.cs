using Spectre.Console;
using Spectre.Console.Rendering;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Planar.CLI.CliGeneral;

internal sealed class EscCancelInput(IAnsiConsoleInput inner) : IAnsiConsoleInput
{
    public bool IsKeyAvailable() => inner.IsKeyAvailable();

    public ConsoleKeyInfo? ReadKey(bool intercept) => Check(inner.ReadKey(intercept));

    public async Task<ConsoleKeyInfo?> ReadKeyAsync(bool intercept, CancellationToken ct)
        => Check(await inner.ReadKeyAsync(intercept, ct));

    private static ConsoleKeyInfo? Check(ConsoleKeyInfo? key)
        => key?.Key == ConsoleKey.Escape ? throw new OperationCanceledException() : key;
}

internal sealed class EscCancelConsole(IAnsiConsole inner) : IAnsiConsole
{
    public Profile Profile => inner.Profile;
    public IAnsiConsoleCursor Cursor => inner.Cursor;
    public IAnsiConsoleInput Input { get; } = new EscCancelInput(inner.Input);
    public IExclusivityMode ExclusivityMode => inner.ExclusivityMode;
    public RenderPipeline Pipeline => inner.Pipeline;

    public void Clear(bool home) => inner.Clear(home);

    public void Write(IRenderable renderable) => inner.Write(renderable);

    public void WriteAnsi(Action<AnsiWriter> action) => inner.WriteAnsi(action);
}