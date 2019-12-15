using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Microsoft.CommonDataModel.ObjectModel.Cdm;
using Microsoft.CommonDataModel.ObjectModel.Enums;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace CDMApi.Features.Shared
{
    public class EntityGenerator
    {
        private readonly ILogger<EntityGenerator> _logger;
        private readonly CsvContentParser _csvContentParser;

        public EntityGenerator(ILogger<EntityGenerator> logger, CsvContentParser csvContentParser)
        {
            _logger = logger;
            _csvContentParser = csvContentParser;
        }

#if DEBUG
        public (string fileName, string content) ParseObjectDefinition(CdmEntityDefinition entityDefinition)
        {
            var sb = new StringBuilder();

            var className = $"{entityDefinition.EntityName[0].ToString().ToUpperInvariant()}{entityDefinition.EntityName.Substring(1)}Model";

            sb.AppendLine("using System;");
            sb.AppendLine();

            sb.Append("public class ");
            sb.AppendLine(className);
            sb.AppendLine("{");

            foreach (CdmTypeAttributeDefinition attr in entityDefinition.Attributes)
            {
                sb.Append("    public ");
                sb.Append(attr.DataFormat);
                sb.Append("? ");
                sb.Append(attr.Name);
                sb.AppendLine(" { get; set; }");
            }

            sb.AppendLine("}");
            return (className + ".cs", sb.ToString());
        }
#endif

        public List<T> BuildObjectModel<T>(CdmEntityDefinition entityDefinition, string content)
        {
            if (entityDefinition == null)
            {
                throw new ArgumentNullException(nameof(entityDefinition));
            }

            var lines = _csvContentParser.SplitContentToLines(content, entityDefinition.Attributes.Count);

            var cultureInfo = new CultureInfo("us-EN");
            var result = new List<T>(lines.Count - 1);
            for (var lineNumber = 0; lineNumber < lines.Count; lineNumber++)
            {
                var obj = new JObject();

                var i = 0;
                foreach (CdmTypeAttributeDefinition attr in entityDefinition.Attributes)
                {
                    ConvertAttribute(obj, attr, lines[lineNumber].Skip(i).First(), cultureInfo);
                    i++;
                }

                result.Add(obj.ToObject<T>());
            }

            return result;
        }

        private void ConvertAttribute(JObject obj, CdmTypeAttributeDefinition attribute, string value, CultureInfo cultureInfo)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                obj.Add(attribute.Name, null);
                return;
            }

            switch (attribute.DataFormat)
            {
                case CdmDataFormat.Unknown: throw new NotImplementedException();
                case CdmDataFormat.Int16: throw new NotImplementedException();
                case CdmDataFormat.Int32: ConvertInt32(obj, attribute, value); break;
                case CdmDataFormat.Int64: ConvertInt64(obj, attribute, value); break;
                case CdmDataFormat.Float: throw new NotImplementedException();
                case CdmDataFormat.Double: ConvertDouble(obj, attribute, value); break;
                case CdmDataFormat.Guid: obj.Add(attribute.Name, value); break;
                case CdmDataFormat.String: obj.Add(attribute.Name, value); break;
                case CdmDataFormat.Char: throw new NotImplementedException();
                case CdmDataFormat.Byte: throw new NotImplementedException();
                case CdmDataFormat.Binary: throw new NotImplementedException();
                case CdmDataFormat.Time: throw new NotImplementedException();
                case CdmDataFormat.Date: throw new NotImplementedException();
                case CdmDataFormat.DateTime: ConvertDateTime(obj, attribute, value, cultureInfo); break;
                case CdmDataFormat.DateTimeOffset: ConvertDateTimeOffset(obj, attribute, value, cultureInfo); break;
                case CdmDataFormat.Boolean: ConvertBoolean(obj, attribute, value); break;
                case CdmDataFormat.Decimal: ConvertDecimal(obj, attribute, value); break;
                case CdmDataFormat.Json: throw new NotImplementedException();
                default: throw new NotImplementedException();
            }
        }

        private void ConvertDateTime(JObject obj, CdmTypeAttributeDefinition attribute, string value, CultureInfo cultureInfo)
        {
            if (DateTime.TryParse(value, cultureInfo, DateTimeStyles.None, out var dateTimeValue1))
            {
                obj.Add(attribute.Name, dateTimeValue1);
                return;
            }
            _logger.LogError($"Error converting {attribute.Name} type {attribute.DataType}");
        }

        private void ConvertDateTimeOffset(JObject obj, CdmTypeAttributeDefinition attribute, string value, CultureInfo cultureInfo)
        {
            if (DateTimeOffset.TryParse(value, cultureInfo, DateTimeStyles.None, out var dateTimeValue1))
            {
                obj.Add(attribute.Name, dateTimeValue1);
                return;
            }
            _logger.LogError($"Error converting {attribute.Name} type {attribute.DataType}");
        }

        private void ConvertInt32(JObject obj, CdmTypeAttributeDefinition attribute, string value)
        {
            if (int.TryParse(value, out var doubleValue))
            {
                obj.Add(attribute.Name, doubleValue);
                return;
            }
            _logger.LogError($"Error converting {attribute.Name} type {attribute.DataType}");
        }

        private void ConvertInt64(JObject obj, CdmTypeAttributeDefinition attribute, string value)
        {
            if (long.TryParse(value, out var doubleValue))
            {
                obj.Add(attribute.Name, doubleValue);
                return;
            }
            _logger.LogError($"Error converting {attribute.Name} type {attribute.DataType}");
        }

        private void ConvertDecimal(JObject obj, CdmTypeAttributeDefinition attribute, string value)
        {
            if (decimal.TryParse(value, out var doubleValue))
            {
                obj.Add(attribute.Name, doubleValue);
                return;
            }
            _logger.LogError($"Error converting {attribute.Name} type {attribute.DataType}");
        }

        private void ConvertBoolean(JObject obj, CdmTypeAttributeDefinition attribute, string value)
        {
            if (bool.TryParse(value, out var doubleValue))
            {
                obj.Add(attribute.Name, doubleValue);
                return;
            }
            _logger.LogError($"Error converting {attribute.Name} type {attribute.DataType}");
        }

        private void ConvertDouble(JObject obj, CdmTypeAttributeDefinition attribute, string value)
        {
            if (double.TryParse(value, out var doubleValue))
            {
                obj.Add(attribute.Name, doubleValue);
                return;
            }
            _logger.LogError($"Error converting {attribute.Name} type {attribute.DataType}");
        }
    }
}