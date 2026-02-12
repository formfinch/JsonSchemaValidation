using FormFinch.JsonSchemaValidation.ManualTests;
using FormFinch.JsonSchemaValidation.ManualTests.Scenarios;

if (args.Length > 0 && args[0] is "-i" or "--interactive")
{
    InteractiveMode.Run();
    return 0;
}

var runner = new TestRunner();

StaticApiScenarios.Run(runner);
OutputFormatScenarios.Run(runner);
MultiDraftScenarios.Run(runner);
ErrorQualityScenarios.Run(runner);
FormatValidationScenarios.Run(runner);
CompiledValidatorScenarios.Run(runner);

return runner.PrintSummary();
