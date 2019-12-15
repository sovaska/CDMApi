﻿namespace Microsoft.CommonDataModel.ObjectModel.Persistence.CdmFolder
{

    using Microsoft.CommonDataModel.ObjectModel.Cdm;
    using Microsoft.CommonDataModel.ObjectModel.Enums;
    using Microsoft.CommonDataModel.ObjectModel.Persistence.CdmFolder.Types;

    class E2ERelationshipPersistence
    {
        public static CdmE2ERelationship FromData(CdmCorpusContext ctx, E2ERelationship dataObj)
        {
            var relationship = ctx.Corpus.MakeObject<CdmE2ERelationship>(CdmObjectType.E2ERelationshipDef);
            relationship.FromEntity = dataObj.FromEntity;
            relationship.FromEntityAttribute = dataObj.FromEntityAttribute;
            relationship.ToEntity = dataObj.ToEntity;
            relationship.ToEntityAttribute = dataObj.ToEntityAttribute;
            return relationship;
        }

        public static E2ERelationship ToData(CdmE2ERelationship instance)
        {
            return new E2ERelationship
            {
                FromEntity = instance.FromEntity,
                FromEntityAttribute = instance.FromEntityAttribute,
                ToEntity = instance.ToEntity,
                ToEntityAttribute = instance.ToEntityAttribute
            };
        }
    }
}
