// Copyright (c) 2026 FormFinch VOF
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE file in the project root for full license information.

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
