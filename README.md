## Code Metrics Parser for VS metrics.exe results
The command line tool is used to interpret the results (.xml) from Visual Studio code metrics analysis, and parse the XML results from multiple projects and merged them into a HTML report file for summary purpose.

````Example: CodeMetricReporter /metricResultPattern:*_metrics.xml /htmlReport:C:\temp\finalReport.html /threshold:80````

**Note**: 
- It can parse multiple results with the specified file pattern (via /metricResultPattern argument) under the specified directory (via /metricResultDir argument). 
- It will parse the results and aggregate the assembly level metrics into one HTML file.
- The project whose MaintainabilityIndex exceeds threshold value will be marked as RED background.

**Help for command-line arguments:**   
```/threshold:<int>``` Threshold value for the MaintainabilityIndex metric. This argument is optional, and 80 is used if it is ommited.

```/metricResultDir:<string>``` The directory that holds all the code metric results.

```/metricResultPattern:<string>``` The file pattern for metrics.exe results

```/htmlReport:<string>``` The final HTML report file with full file path.

```/?``` Show the help.
