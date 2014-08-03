using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CqlPoco.Utils;

namespace CqlPoco.Mapping
{
    /// <summary>
    /// Factory responsible for creating PocoData instances.
    /// </summary>
    internal class PocoDataFactory
    {
        private const BindingFlags PublicInstanceBindingFlags = BindingFlags.Public | BindingFlags.Instance;

        private readonly ConcurrentDictionary<Type, PocoData> _cache; 

        public PocoDataFactory()
        {
            _cache = new ConcurrentDictionary<Type, PocoData>();
        }

        public PocoData GetPocoData<T>()
        {
            return _cache.GetOrAdd(typeof(T), CreatePocoData);
        }
        
        private static PocoData CreatePocoData(Type pocoType)
        {
            bool explicitColumns = pocoType.GetCustomAttributes<ExplicitColumnsAttribute>(true).FirstOrDefault() != null;

            // Find all public instance fields and properties and convert to PocoColumn dictionary keyed by column name
            IEnumerable<PocoColumn> fields = GetMappableFields(pocoType, explicitColumns).Select(PocoColumn.FromField);
            IEnumerable<PocoColumn> properties = GetMappableProperties(pocoType, explicitColumns).Select(PocoColumn.FromProperty);

            LookupKeyedCollection<string, PocoColumn> columns = fields.Union(properties).ToLookupKeyedCollection(pc => pc.ColumnName, StringComparer.OrdinalIgnoreCase);

            // Figure out the table name
            var tableNameAttribute = pocoType.GetCustomAttributes<TableNameAttribute>(true).FirstOrDefault();
            string tableName = tableNameAttribute == null ? pocoType.Name : tableNameAttribute.Value;

            return new PocoData(pocoType, tableName, columns);
        }

        /// <summary>
        /// Gets any public instance fields that are settable for the given type.
        /// </summary>
        private static IEnumerable<FieldInfo> GetMappableFields(Type t, bool explicitColumns)
        {
            return t.GetFields(PublicInstanceBindingFlags).Where(field => field.IsInitOnly == false && ShouldMap(field, explicitColumns));
        }

        /// <summary>
        /// Gets any public instance properties for the given type.
        /// </summary>
        private static IEnumerable<PropertyInfo> GetMappableProperties(Type t, bool explicitColumns)
        {
            return t.GetProperties(PublicInstanceBindingFlags).Where(p => p.CanWrite && ShouldMap(p, explicitColumns));
        }

        private static bool ShouldMap(MemberInfo propOrField, bool explicitColumns)
        {
            // If explicit columns is turned on, must have a ColumnAttribute to be mapped
            if (explicitColumns && propOrField.GetCustomAttributes<ColumnAttribute>(true).FirstOrDefault() == null)
                return false;

            // If explicit columns is not on, ignore anything with an IgnoreAttribute
            if (explicitColumns == false && propOrField.GetCustomAttributes<IgnoreAttribute>(true).FirstOrDefault() != null)
                return false;

            return true;
        }
    }
}