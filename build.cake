void RunDotNet(string arguments)
{
    var exitCode = StartProcess("dotnet", arguments);

    if (exitCode != 0)
    {
        Error($"dotnet {arguments} falhou ({exitCode})");
        System.Environment.Exit(exitCode);
    }
}

Task("Clean")
    .Does(() =>
    {
        CleanDirectory("./TestResults");
        CleanDirectory("./CoverageReport");
        RunDotNet("clean MVFC.Veragi.Simulator.slnx --configuration Release");
    });

Task("Restore")
    .IsDependentOn("Clean")
    .Does(() => RunDotNet("restore MVFC.Veragi.Simulator.slnx"));

Task("Build")
    .IsDependentOn("Restore")
    .Does(() => RunDotNet("build MVFC.Veragi.Simulator.slnx --configuration Release --no-restore"));

Task("Test-Coverage")
    .IsDependentOn("Build")
    .Does(() =>
    {
        foreach (var project in GetFiles("./tests/**/*Tests.csproj"))
        {
            RunDotNet($"test \"{project.FullPath}\" --configuration Release --no-build --settings coverage.runsettings --collect:\"XPlat Code Coverage\" --results-directory TestResults");
        }

        RunDotNet("tool restore");
        RunDotNet("tool run reportgenerator -- -reports:TestResults/**/coverage.cobertura.xml -targetdir:CoverageReport -reporttypes:Html;Cobertura;MarkdownSummaryGithub -assemblyfilters:+MVFC.Veragi.Simulator.Api;+MVFC.Veragi.Simulator.Domain;+MVFC.Veragi.Simulator.Data;+MVFC.Veragi.Simulator.IoC;+MVFC.Veragi.Simulator.Shareable;-*Tests*;-*TestHelpers*;-*WebhookWorker*;-*AppHost*");
        var xmlReaderSettings = new System.Xml.XmlReaderSettings
        {
            DtdProcessing = System.Xml.DtdProcessing.Ignore
        };
        using var reader = System.Xml.XmlReader.Create("CoverageReport/Cobertura.xml", xmlReaderSettings);
        var doc = System.Xml.Linq.XDocument.Load(reader);
        var lineRate = doc.Root?.Attribute("line-rate")?.Value ?? "0";
        var branchRate = doc.Root?.Attribute("branch-rate")?.Value ?? "0";

        if (decimal.Parse(lineRate, System.Globalization.CultureInfo.InvariantCulture) != 1m
            || decimal.Parse(branchRate, System.Globalization.CultureInfo.InvariantCulture) != 1m)
        {
            Error("A cobertura de linhas e branches dos cinco projetos de produção deve ser 100%.");
            System.Environment.Exit(1);
        }
    });

Task("Default")
    .IsDependentOn("Test-Coverage");

RunTarget("Default");
