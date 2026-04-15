using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Xeon.XScriptableDB.Editor
{
    /// <summary>
    /// Utility class for formatting SQL execution results and retrieving values.
    /// </summary>
    public static class SqlResultFormatter
    {
        private const BindingFlags FieldBindingFlags =
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

        /// <summary>
        /// Gets the list of column names from a record.
        /// </summary>
        /// <param name="record">The record</param>
        /// <returns>List of column names</returns>
        public static List<string> GetColumnNamesFromRecord(object record)
        {
            if (record == null)
                return new List<string>();

            // For ResultRow, use the Values dictionary keys
            if (record is ResultRow resultRow)
                return resultRow.Values.Keys.ToList();

            // For JoinedRecord, retrieve fields from all tables
            if (record is JoinedRecord joinedRecord)
            {
                var names = new List<string>();
                foreach (var kvp in joinedRecord.TableRecords)
                {
                    var tableAlias = kvp.Key;
                    var tableRecord = kvp.Value;
                    if (tableRecord == null)
                        continue;

                    var recordType = joinedRecord.GetRecordType(tableAlias);
                    if (recordType == null)
                        continue;

                    foreach (var field in recordType.GetFields(FieldBindingFlags))
                    {
                        names.Add($"{tableAlias}.{field.Name}");
                    }
                }
                return names;
            }

            // For a normal record
            var type = record.GetType();
            return type.GetFields(FieldBindingFlags)
                .Select(f => f.Name)
                .ToList();
        }

        /// <summary>
        /// Gets the value of the specified field from a record.
        /// </summary>
        /// <param name="record">The record</param>
        /// <param name="recordType">The record type (used when it is not a ResultRow or JoinedRecord)</param>
        /// <param name="fieldName">The field name</param>
        /// <returns>The field value</returns>
        public static object GetFieldValue(object record, Type recordType, string fieldName)
        {
            if (record == null)
                return null;

            // For ResultRow, retrieve from the Values dictionary
            if (record is ResultRow resultRow)
            {
                if (resultRow.Values.TryGetValue(fieldName, out var value))
                    return value;
                return null;
            }

            // For JoinedRecord
            if (record is JoinedRecord joinedRecord)
                return GetJoinedFieldValue(joinedRecord, fieldName);

            if (recordType == null)
                return null;

            return GetFieldValueFromType(record, recordType, fieldName);
        }

        /// <summary>
        /// Gets field values from a JoinedRecord.
        /// </summary>
        private static object GetJoinedFieldValue(JoinedRecord joinedRecord, string fieldName)
        {
            // When a table alias is included (e.g., "t.Id")
            if (fieldName.Contains('.'))
            {
                var parts = fieldName.Split('.');
                var tableAlias = parts[0];
                var column = parts[1];

                var tableRecord = joinedRecord.GetRecord(tableAlias);
                var tableType = joinedRecord.GetRecordType(tableAlias);
                if (tableRecord != null && tableType != null)
                    return GetFieldValueFromType(tableRecord, tableType, column);
                return null;
            }

            // When no table alias is present, search across all tables
            foreach (var kvp in joinedRecord.TableRecords)
            {
                var tableRecord = kvp.Value;
                if (tableRecord == null)
                    continue;

                var tableType = joinedRecord.GetRecordType(kvp.Key);
                if (HasField(tableType, fieldName))
                    return GetFieldValueFromType(tableRecord, tableType, fieldName);
            }
            return null;
        }

        /// <summary>
        /// Gets a field value using type information.
        /// </summary>
        private static object GetFieldValueFromType(object record, Type recordType, string fieldName)
        {
            if (record == null || recordType == null)
                return null;

            var field = recordType.GetField(fieldName, FieldBindingFlags);
            if (field != null)
                return field.GetValue(record);

            // Search ignoring case
            field = recordType.GetFields(FieldBindingFlags)
                .FirstOrDefault(f => f.Name.Equals(fieldName, StringComparison.OrdinalIgnoreCase));
            if (field != null)
                return field.GetValue(record);

            var property = recordType.GetProperty(fieldName, BindingFlags.Public | BindingFlags.Instance);
            if (property?.CanRead == true)
                return property.GetValue(record);

            return null;
        }

        /// <summary>
        /// Checks whether a field exists in the specified type.
        /// </summary>
        private static bool HasField(Type recordType, string fieldName)
        {
            if (recordType == null)
                return false;

            var field = recordType.GetField(fieldName, FieldBindingFlags);
            if (field != null)
                return true;

            field = recordType.GetFields(FieldBindingFlags)
                .FirstOrDefault(f => f.Name.Equals(fieldName, StringComparison.OrdinalIgnoreCase));
            if (field != null)
                return true;

            var property = recordType.GetProperty(fieldName, BindingFlags.Public | BindingFlags.Instance);
            if (property?.CanRead == true)
                return true;

            return false;
        }

        /// <summary>
        /// Formats a value into a display string.
        /// </summary>
        /// <param name="value">The value</param>
        /// <returns>Display string</returns>
        public static string FormatValue(object value)
        {
            if (value == null)
                return "(null)";

            return value switch
            {
                string s => s,
                bool b => b ? "true" : "false",
                float f => f.ToString("F2"),
                double d => d.ToString("F2"),
                DateTime dt => dt.ToString("yyyy-MM-dd HH:mm:ss"),
                _ => value.ToString()
            };
        }
    }
}
