﻿using System;
using System.Collections.Generic;

namespace SharpShooterReportsRunner
{
    public class Context
    {
        public string Server { get; set; }
        public string Username { get; set; } = "admin";
        public string Password { get; set; } = "admin";
        public string TemplatePath { get; set; }
        public string OutputPath { get; set; }
        public bool LaunchReportDesigner { get; set; }
        public string UploadedReportLocation { get; set; }
        public string UploadedReportTitle { get; set; }
        public string QueryFrom { get; set; }
        public string QueryTo { get; set; }
        public GroupBy GroupBy { get; set; } = GroupBy.Year;
        public List<TimeSeries> TimeSeries { get; set; } = new List<TimeSeries>();
        public List<RatingModel> RatingModels { get; set; } = new List<RatingModel>();
        public List<ExternalDataSet> ExternalDataSets { get; set; } = new List<ExternalDataSet>();
        public Dictionary<string,string> ReportParameters { get; set; } = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
        public Dictionary<string, string> ParameterOverrides { get; set; } = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
    }
}
