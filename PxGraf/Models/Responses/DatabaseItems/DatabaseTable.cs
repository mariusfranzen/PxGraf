#nullable enable
using Px.Utils.Language;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using System;

namespace PxGraf.Models.Responses.DatabaseItems
{
    public enum DatabaseTableError
    {
        Null,
        ContentLoad,
        ContentDimensionMissing,
        TimeDimensionMissing,
    }

    /// <summary>
    /// Represents a px database table.
    /// </summary>
    public class DatabaseTable
    {
        /// <summary>
        /// Code of the database table.
        /// </summary>
        public string Code { get; private set; }
        /// <summary>
        /// Multilanguage name of the database table.
        /// </summary>
        public MultilanguageString Name { get; private set; }
        /// <summary>
        /// Last updated date of the database table.
        /// </summary>
        public DateTime? LastUpdated { get; private set; }
        /// <summary>
        /// Available languages of the database table.
        /// </summary>
        public List<string> Languages { get; private set; }
        /// <summary>
        /// Error identifier
        /// </summary>

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public DatabaseTableError? Error { get; private set; } = null;

        public DatabaseTable(string code, MultilanguageString name, DateTime lastUpdated, List<string> languages)
        {
            Code = code;
            Name = name;
            LastUpdated = lastUpdated;
            Languages = languages;
        }

        private DatabaseTable(string code, MultilanguageString name, List<string> languages, DatabaseTableError error)
        {
            Code = code;
            Name = name;
            Languages = languages;
            Error = error;
        }

        public static DatabaseTable FromError(string code, MultilanguageString name, List<string> languages, DatabaseTableError error)
        {
            return new DatabaseTable(code, name, languages, error);
        }
    }
}
#nullable disable
