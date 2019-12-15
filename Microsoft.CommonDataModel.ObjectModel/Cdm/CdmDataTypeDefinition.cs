﻿//-----------------------------------------------------------------------
// <copyright file="CdmDataTypeDefinition.cs" company="Microsoft">
//      All rights reserved.
// </copyright>
//-----------------------------------------------------------------------
namespace Microsoft.CommonDataModel.ObjectModel.Cdm
{
    using Microsoft.CommonDataModel.ObjectModel.Enums;
    using Microsoft.CommonDataModel.ObjectModel.ResolvedModel;
    using Microsoft.CommonDataModel.ObjectModel.Utilities;
    using System;

    public class CdmDataTypeDefinition : CdmObjectDefinitionBase
    {
        /// <summary>
        /// Gets or sets the data type name.
        /// </summary>
        public string DataTypeName { get; set; }

        /// <summary>
        /// Gets or sets the data type extended by this data type.
        /// </summary>
        public CdmDataTypeReference ExtendsDataType { get; set; }
        public override string GetName()
        {
            return this.DataTypeName;
        }

        internal CdmDataTypeReference ExtendsDataTypeRef
        {
            get
            {
                return this.ExtendsDataType;
            }
        }

        public CdmDataTypeDefinition(CdmCorpusContext ctx, string dataTypeName, CdmDataTypeReference extendsDataType = null)
            : base(ctx)
        {
            this.ObjectType = CdmObjectType.DataTypeDef;
            this.DataTypeName = dataTypeName;
            this.ExtendsDataType = extendsDataType;
        }

        [Obsolete]
        public override CdmObjectType GetObjectType()
        {
            return CdmObjectType.DataTypeDef;
        }

        [Obsolete("CopyData is deprecated. Please use the Persistence Layer instead.")]
        public override dynamic CopyData(ResolveOptions resOpt, CopyOptions options)
        {
            return CdmObjectBase.CopyData<CdmDataTypeDefinition>(this, resOpt, options);
        }

        public override CdmObject Copy(ResolveOptions resOpt = null)
        {
            if (resOpt == null)
            {
                resOpt = new ResolveOptions(this);
            }

            CdmDataTypeDefinition copy = new CdmDataTypeDefinition(this.Ctx, this.DataTypeName, null)
            {
                ExtendsDataType = (CdmDataTypeReference)this.ExtendsDataType?.Copy(resOpt)
            };

            this.CopyDef(resOpt, copy);
            return copy;
        }

        public override bool Validate()
        {
            return !string.IsNullOrEmpty(this.DataTypeName);
        }

        /// <inheritdoc />
        public override bool Visit(string pathFrom, VisitCallback preChildren, VisitCallback postChildren)
        {
            string path = this.DeclaredPath;
            if (string.IsNullOrEmpty(path))
            {
                path = pathFrom + this.DataTypeName;
                this.DeclaredPath = path;
            }
            //trackVisits(path);

            if (preChildren?.Invoke(this, path) == true)
                return false;
            if (this.ExtendsDataType?.Visit(path + "/extendsDataType/", preChildren, postChildren) == true)
                return true;
            if (this.VisitDef(path, preChildren, postChildren))
                return true;
            if (postChildren?.Invoke(this, path) == true)
                return true;
            return false;
        }

        public override bool IsDerivedFrom(string baseDef, ResolveOptions resOpt)
        {
            if (resOpt == null)
            {
                resOpt = new ResolveOptions(this);
            }

            return this.IsDerivedFromDef(resOpt, this.ExtendsDataTypeRef, this.GetName(), baseDef);
        }

        internal override void ConstructResolvedTraits(ResolvedTraitSetBuilder rtsb, ResolveOptions resOpt)
        {
            this.ConstructResolvedTraitsDef(this.ExtendsDataTypeRef, rtsb, resOpt);
            //rtsb.CleanUp();
        }

        internal override ResolvedAttributeSetBuilder ConstructResolvedAttributes(ResolveOptions resOpt, CdmAttributeContext under = null)
        {
            return null;
        }
    }
}
