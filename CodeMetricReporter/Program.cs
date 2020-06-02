using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;

namespace CodeMetricReporter
{
  class Program
  {
    // for console arguments
    private const string ARGS_HELP = "/?";
    private const string ARGS_THRESHOLD = "/threshold";
    private const string ARGS_METRICS_RESULT_DIR = "/metricResultDir";
    private const string ARGS_METRICS_RESULT_PATTERN = "/metricResultPattern";
    private const string ARGS_HTML_REPORT = "/htmlReport";

    private const string HELP_OUTPUTS = @"Code Metrics Parser for VS metrics.exe results

Example: CodeMetricReporter /metricResultPattern:*_metrics.xml /htmlReport:C:\temp\finalReport.html /threshold:80

**Note: 
- It can parse multiple results with the specified file pattern (via /metricResultPattern argument) under the specified directory (via /metricResultDir argument). 
- It will parse the results and aggregate the assembly level metrics into one HTML file.
- The project whose MaintainabilityIndex exceeds threshold value will be marked as RED background.

Help for command-line arguments:
/threshold:<int> Threshold value for the MaintainabilityIndex metric. This argument is optional, and 80 is used if it is ommited.

/metricResultDir:<string> The directory that holds all the code metric results.

/metricResultPattern:<string> The file pattern for metrics.exe results

/htmlReport:<string> The final HTML report file with full file path.";

    private static double ThresholdMaintainabilityIndex { get; set; }
    private static string MetricsResultDirectory { get; set; }
    private static string MetricsResultPattern { get; set; }
    private static string HtmlReportFileWithFullPath { get; set; }

    // for metrics
    private const string PROJECT_NAME = "Project Name";
    private const string MAINTAIN_INDEX = "MaintainabilityIndex";
    private const string CYCL_COMPLEXITY = "CyclomaticComplexity";
    private const string CLASS_COUPLING = "ClassCoupling";
    private const string DEPTH_OF_INHERITANCE = "DepthOfInheritance";
    private const string SOURCE_LINES = "SourceLines";
    private const string EXECUTABLE_LINES = "ExecutableLines";

    private static readonly Dictionary<string, string> ArgumentsDictionary = new Dictionary<string, string>();
    private static readonly List<Dictionary<string, string>> MetricsList = new List<Dictionary<string, string>>();
    private static List<string> _metricResultList = new List<string>();

    static void Main(string[] args)
    {
      ThresholdMaintainabilityIndex = 80; // set default value

      ParseArguments(args);

      ParseMetricsResultWithPattern();
      ParseReports();

      GenerateHtmlReport();
    }

    private static void ParseArguments(string[] args)
    {
      if (args.Length == 1 && args[0] == ARGS_HELP)
      {
        Console.WriteLine(HELP_OUTPUTS);
        Environment.Exit(0);
      }

      try
      {
        foreach (string argument in args)
        {
          var parts = argument.Split(new[] { ':' }, 2); // split for the first : char, to get the path
          ArgumentsDictionary.Add(parts[0], parts[1]);
        }

        if (ArgumentsDictionary.ContainsKey(ARGS_THRESHOLD))
        {
          ThresholdMaintainabilityIndex = double.Parse(ArgumentsDictionary[ARGS_THRESHOLD]);
        }

        MetricsResultDirectory = ArgumentsDictionary[ARGS_METRICS_RESULT_DIR];
        MetricsResultPattern = ArgumentsDictionary[ARGS_METRICS_RESULT_PATTERN];
        HtmlReportFileWithFullPath = ArgumentsDictionary[ARGS_HTML_REPORT];
      }
      catch
      {
        Console.WriteLine($"Invalid arguments. The following arguments are required: {ARGS_HTML_REPORT}, {ARGS_METRICS_RESULT_DIR} and {ARGS_METRICS_RESULT_PATTERN}. Use {ARGS_HELP} for details.");
        Environment.Exit(-1);
      }
    }

    private static void ParseMetricsResultWithPattern()
    {
      var directoryExists = Directory.Exists(MetricsResultDirectory);
      if (!directoryExists)
      {
        Console.WriteLine($"The directory does not exist:{MetricsResultDirectory}.");
        Environment.Exit(-1);
      }

      var list = Directory.GetFiles(MetricsResultDirectory, MetricsResultPattern);
      if (list.Length == 0)
      {
        Console.WriteLine($"No code metric result file under {MetricsResultDirectory} with the pattern {MetricsResultPattern}.");
        Environment.Exit(0);
      }

      _metricResultList = list.ToList();
    }

    private static void ParseReports()
    {
      foreach (var reportFile in _metricResultList)
      {
        try
        {
          var metricsDictionary = new Dictionary<string, string>();
          var doc = new XmlDocument();
          doc.Load(reportFile);

          // ReSharper disable PossibleNullReferenceException - disable null check because there is catch block
          var node = doc.DocumentElement.SelectSingleNode("/CodeMetricsReport/Targets/Target");
          var projectName = node.Attributes["Name"]?.InnerText;
          metricsDictionary.Add(PROJECT_NAME, projectName);

          var metricsNodes = node.SelectNodes("Assembly/Metrics/Metric");
          foreach (XmlNode metricNode in metricsNodes)
          {
            var metricName = metricNode.Attributes["Name"]?.InnerText;
            var metricValue = metricNode.Attributes["Value"]?.InnerText;

            // ReSharper disable once AssignNullToNotNullAttribute - disable null check because there is catch block
            metricsDictionary.Add(metricName, metricValue);
          }
          // ReSharper restore PossibleNullReferenceException

          MetricsList.Add(metricsDictionary);
        }
        catch (Exception ex)
        {
          Console.WriteLine($"Exception occurs while parsing file:{reportFile}. Exception:{ex.Message}");
        }
      }
    }

    private static void GenerateHtmlReport()
    {
      var htmlContent = new StringBuilder();
      htmlContent.AppendLine("<html>  <body>  <h2>Code Metrics Report</h2>  <table border='1'>    <tr bgcolor='#92cddc'>");
      htmlContent.AppendFormat($"<th>{PROJECT_NAME}</th>");
      htmlContent.AppendFormat($"<th>{MAINTAIN_INDEX}</th>");
      htmlContent.AppendFormat($"<th>{CYCL_COMPLEXITY}</th>");
      htmlContent.AppendFormat($"<th>{CLASS_COUPLING}</th>");
      htmlContent.AppendFormat($"<th>{DEPTH_OF_INHERITANCE}</th>");
      htmlContent.AppendFormat($"<th>{SOURCE_LINES}</th>");
      htmlContent.AppendFormat($"<th>{EXECUTABLE_LINES}</th>");
      htmlContent.AppendLine("</tr>");

      foreach (var projectMetrics in MetricsList)
      {
        htmlContent.AppendLine("<tr>");
        htmlContent.AppendFormat($"<td>{projectMetrics[PROJECT_NAME]}</td>");

        var isMaintainIndexHealthy = IsMaintainIndexHealthy(projectMetrics[MAINTAIN_INDEX]);
        var cellBackgroundHtml = isMaintainIndexHealthy ? " style='background-color: rgb(154, 205, 50);'" 
                                                        : " style='background-color: rgb(255, 0, 0);'";
        htmlContent.AppendFormat($"<td{cellBackgroundHtml}>{projectMetrics[MAINTAIN_INDEX]}</td>");
        
        htmlContent.AppendFormat($"<td>{projectMetrics[CYCL_COMPLEXITY]}</td>");
        htmlContent.AppendFormat($"<td>{projectMetrics[CLASS_COUPLING]}</td>");
        htmlContent.AppendFormat($"<td>{projectMetrics[DEPTH_OF_INHERITANCE]}</td>");
        htmlContent.AppendFormat($"<td>{projectMetrics[SOURCE_LINES]}</td>");
        htmlContent.AppendFormat($"<td>{projectMetrics[EXECUTABLE_LINES]}</td>");
        htmlContent.AppendLine("</tr>");
      }

      htmlContent.AppendLine("</table></body></html>");
      File.WriteAllText(HtmlReportFileWithFullPath, htmlContent.ToString());
    }

    // check if the MaintainIndex exceeds the pre-defined threshold value.
    // Return ture if the value < threshold, or return false.
    private static bool IsMaintainIndexHealthy(string actualStringValue)
    {
      double actualValue;
      var parsed = double.TryParse(actualStringValue, out actualValue);
      if (!parsed) return false;

      return actualValue >= ThresholdMaintainabilityIndex;
    }
  }
}
