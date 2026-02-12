using FormFinch.JsonSchemaValidation.Validation.Output;

namespace FormFinch.JsonSchemaValidation.ManualTests;

/// <summary>
/// Simple test runner with colored console output for manual validation scenarios.
/// </summary>
internal sealed class TestRunner
{
    private int _total;
    private int _passed;
    private int _failed;

    public void PrintSection(string title)
    {
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine(new string('=', 60));
        Console.WriteLine($"  {title}");
        Console.WriteLine(new string('=', 60));
        Console.ResetColor();
    }

    public void Run(string name, Action test)
    {
        _total++;
        try
        {
            test();
            _passed++;
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"  PASS: {name}");
            Console.ResetColor();
        }
        catch (Exception ex)
        {
            _failed++;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"  FAIL: {name}");
            Console.WriteLine($"        {ex.Message}");
            Console.ResetColor();
        }
    }

    public static void PrintErrors(OutputUnit result, int indent = 0)
    {
        var prefix = new string(' ', indent * 2 + 4);

        if (!result.Valid && result.Error != null)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write($"{prefix}Instance: ");
            Console.ResetColor();
            Console.WriteLine(result.InstanceLocation);

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write($"{prefix}Keyword:  ");
            Console.ResetColor();
            Console.WriteLine(result.KeywordLocation);

            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write($"{prefix}Error:    ");
            Console.ResetColor();
            Console.WriteLine(result.Error);
            Console.WriteLine();
        }

        if (result.Errors != null)
        {
            foreach (var error in result.Errors)
            {
                PrintErrors(error, indent + 1);
            }
        }
    }

    public int PrintSummary()
    {
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine(new string('=', 60));
        Console.WriteLine("  Summary");
        Console.WriteLine(new string('=', 60));
        Console.ResetColor();

        Console.WriteLine($"  Total:  {_total}");

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"  Passed: {_passed}");
        Console.ResetColor();

        if (_failed > 0)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"  Failed: {_failed}");
            Console.ResetColor();
        }
        else
        {
            Console.WriteLine($"  Failed: {_failed}");
        }

        Console.WriteLine();
        return _failed > 0 ? 1 : 0;
    }
}
