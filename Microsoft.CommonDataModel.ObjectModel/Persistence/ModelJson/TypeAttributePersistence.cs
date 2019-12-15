﻿namespace Microsoft.CommonDataModel.ObjectModel.Persistence.ModelJson
{
    using Microsoft.CommonDataModel.ObjectModel.Cdm;
    using Microsoft.CommonDataModel.ObjectModel.Enums;
    using Microsoft.CommonDataModel.ObjectModel.Persistence.ModelJson.types;
    using Microsoft.CommonDataModel.ObjectModel.Utilities;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    /// <summary>
    /// The type attribute persistence.
    /// </summary>
    class TypeAttributePersistence
    {
        public static async Task<CdmTypeAttributeDefinition> FromData(CdmCorpusContext ctx, Attribute obj, CdmCollection<CdmTraitDefinition> extensionTraitDefList)
        {
            var attribute = ctx.Corpus.MakeObject<CdmTypeAttributeDefinition>(CdmObjectType.TypeAttributeDef, obj.Name);
            // Do a conversion between CDM data format and model.json data type.
            attribute.DataFormat = DataTypeFromData(obj.DataType);

            attribute.Description = obj.Description;

            if (obj.IsHidden == true)
            {
                var isHiddenTrait = ctx.Corpus.MakeObject<CdmTraitReference>(CdmObjectType.TraitRef, "is.hidden");
                isHiddenTrait.IsFromProperty = true;
                attribute.AppliedTraits.Add(isHiddenTrait);
            }

            await Utils.ProcessAnnotationsFromData(ctx, obj, attribute.AppliedTraits);

            ExtensionHelper.ProcessExtensionFromJson(ctx, obj, attribute.AppliedTraits, extensionTraitDefList);

            return attribute;
        }

        public static async Task<Attribute> ToData(CdmTypeAttributeDefinition instance, ResolveOptions resOpt, CopyOptions options)
        {
            var attribute = new Attribute
            {
                Name = instance.Name,
                DataType = DataTypeToData(instance.DataFormat),
                Description = instance.Description
            };

            await Utils.ProcessAnnotationsToData(instance.Ctx, attribute, instance.AppliedTraits);

            var t2pm = new TraitToPropertyMap(instance);

            var isHiddenTrait = t2pm.FetchTraitReference("is.hidden");
            if (isHiddenTrait != null)
            {
                attribute.IsHidden = true;
            }

            return attribute;
        }

        private static CdmDataFormat DataTypeFromData(string dataType)
        {
            switch (dataType.ToLower())
            {
                case "string":
                    return CdmDataFormat.String;
                case "int64":
                    return CdmDataFormat.Int64;
                case "double":
                    return CdmDataFormat.Double;
                case "datetime":
                    return CdmDataFormat.DateTime;
                case "datetimeoffset":
                    return CdmDataFormat.DateTimeOffset;
                case "decimal":
                    return CdmDataFormat.Decimal;
                case "boolean":
                    return CdmDataFormat.Boolean;
                case "guid":
                    return CdmDataFormat.Guid;
                case "json":
                    return CdmDataFormat.Json;
                default:
                    return CdmDataFormat.Unknown;
            }
        }

        private static string DataTypeToData(CdmDataFormat? dataType)
        {
            switch (dataType)
            {
                case CdmDataFormat.Int16:
                case CdmDataFormat.Int32:
                case CdmDataFormat.Int64:
                    return "int64";
                case CdmDataFormat.Float:
                case CdmDataFormat.Double:
                    return "double";
                case CdmDataFormat.Char:
                case CdmDataFormat.String:
                    return "string";
                case CdmDataFormat.Guid:
                    return "guid";
                case CdmDataFormat.Binary:
                    return "boolean";
                case CdmDataFormat.Time:
                case CdmDataFormat.Date:
                case CdmDataFormat.DateTime:
                    return "dateTime";
                case CdmDataFormat.DateTimeOffset:
                    return "dateTimeOffset";
                case CdmDataFormat.Boolean:
                    return "boolean";
                case CdmDataFormat.Decimal:
                    return "decimal";
                case CdmDataFormat.Json:
                    return "json";
                default:
                    return "unclassified";
            }
        }
    }
}
