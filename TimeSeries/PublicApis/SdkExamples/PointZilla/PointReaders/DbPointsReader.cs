﻿using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using Aquarius.TimeSeries.Client.ServiceModels.Acquisition;
using NodaTime;
using PointZilla.DbClient;
using ServiceStack.Logging;

namespace PointZilla.PointReaders
{
    public class DbPointsReader : PointReaderBase, IPointReader
    {
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public DbPointsReader(Context context)
            : base(context)
        {
        }

        public List<TimeSeriesPoint> LoadPoints()
        {
            if (!Context.DbType.HasValue)
                throw new ExpectedException($"/{nameof(Context.DbType)} must be set");

            ValidateContext();

            var query = ResolveQuery();

            Log.Info($"Querying {Context.DbType} database for points ...");

            using (var dbClient = DbClientFactory.CreateOpened(Context.DbType.Value, Context.DbConnectionString))
            {
                var table = dbClient.ExecuteTable(query);

                ValidateTable(table);

                return table
                    .Rows.Cast<DataRow>()
                    .Select(ConvertRowToPoint)
                    .ToList();
            }
        }

        private void ValidateContext()
        {
            if (string.IsNullOrWhiteSpace(Context.DbConnectionString))
                throw new ExpectedException($"You must specify the /{nameof(Context.DbConnectionString)}= option when /{nameof(Context.DbType)}={Context.DbType}");

            if (string.IsNullOrWhiteSpace(Context.DbQuery))
                throw new ExpectedException($"You must specify the /{nameof(Context.DbQuery)}= option when /{nameof(Context.DbType)}={Context.DbType}");

            ValidateConfiguration(Context);
        }

        private string ResolveQuery()
        {
            if (!Context.DbQuery.StartsWith("@"))
                return Context.DbQuery;

            var queryPath = Context.DbQuery.Substring(1);

            if (!File.Exists(queryPath))
                throw new ExpectedException($"{nameof(Context.DbQuery)} file '{queryPath}' does not exist.");

            return File.ReadAllText(queryPath);
        }

        private void ValidateTable(DataTable dataTable)
        {
            Context.CsvHasHeaderRow = true;

            ValidateHeaderFields(dataTable
                .Columns
                .Cast<DataColumn>()
                .Select(c => c.ColumnName)
                .ToArray());
        }

        private TimeSeriesPoint ConvertRowToPoint(DataRow row)
        {
            Instant? time = null;
            double? value = null;
            int? gradeCode = null;
            List<string> qualifiers = null;

            if (Context.CsvDateOnlyField != null)
            {
                var dateOnly = DateTime.MinValue;
                var timeOnly = DefaultTimeOfDay;

                ParseColumn<DateTime>(row, Context.CsvDateOnlyField.ColumnIndex, dateTime => dateOnly = dateTime.Date);

                if (Context.CsvTimeOnlyField != null)
                {
                    ParseColumn<DateTime>(row, Context.CsvDateOnlyField.ColumnIndex, dateTime => timeOnly = dateTime.TimeOfDay);
                }

                time = InstantFromDateTime(dateOnly.Add(timeOnly));
            }
            else
            {
                ParseColumn<DateTime>(row, Context.CsvDateTimeField.ColumnIndex, dateTime => time = InstantFromDateTime(dateTime));
            }

            ParseColumn<double>(row, Context.CsvValueField.ColumnIndex, number => value = number);

            ParseColumn<int>(row, Context.CsvGradeField?.ColumnIndex, grade => gradeCode = grade);

            ParseNullableColumn<string>(row, Context.CsvQualifiersField?.ColumnIndex, text => qualifiers = QualifiersParser.Parse(text));

            if (time == null)
                return null;

            return new TimeSeriesPoint
            {
                Time = time,
                Value = value,
                GradeCode = gradeCode,
                Qualifiers = qualifiers
            };
        }

        private void ParseColumn<T>(DataRow row, int? columnIndex, Action<T> parseAction) where T : struct
        {
            if (!columnIndex.HasValue || columnIndex <= 0 || columnIndex > row.Table.Columns.Count)
                return;

            var index = columnIndex.Value - 1;

            if (row.IsNull(index))
                return;

            var value = row[index];

            if (!(value is T typedValue))
            {
                typedValue = (T)Convert.ChangeType(value, typeof(T));
            }

            parseAction(typedValue);
        }

        private void ParseNullableColumn<T>(DataRow row, int? columnIndex, Action<T> parseAction) where T : class
        {
            if (!columnIndex.HasValue || columnIndex <= 0 || columnIndex > row.Table.Columns.Count)
                return;

            var index = columnIndex.Value - 1;

            if (row.IsNull(index))
                return;

            var value = row[index];

            if (!(value is T typedValue))
            {
                typedValue = (T)Convert.ChangeType(value, typeof(T));
            }

            if (typedValue != null)
                parseAction(typedValue);
        }
    }
}
