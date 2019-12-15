﻿//-----------------------------------------------------------------------
// <copyright file="CdmAttributeGroupDefinition.cs" company="Microsoft">
//      All rights reserved.
// </copyright>
//-----------------------------------------------------------------------
namespace Microsoft.CommonDataModel.ObjectModel.Cdm
{
    using Microsoft.CommonDataModel.ObjectModel.Enums;
    using Microsoft.CommonDataModel.ObjectModel.ResolvedModel;
    using Microsoft.CommonDataModel.ObjectModel.Utilities;
    using System;

    /// <summary>
    /// The CDM definition that contains a collection of CdmAttributeItem objects.
    /// </summary>
    public class CdmAttributeGroupDefinition : CdmObjectDefinitionBase, CdmReferencesEntities
    {
        public CdmAttributeGroupDefinition(CdmCorpusContext ctx, string attributeGroupName)
                   : base(ctx)
        {
            this.ObjectType = CdmObjectType.AttributeGroupDef;
            this.AttributeGroupName = attributeGroupName;
            this.Members = new CdmCollection<CdmAttributeItem>(this.Ctx, this, CdmObjectType.TypeAttributeDef);
        }

        /// <summary>
        /// Gets or sets the attribute group name.
        /// </summary>
        public string AttributeGroupName { get; set; }

        /// <summary>
        /// Gets or sets the attribute group context.
        /// </summary>
        public CdmAttributeContextReference AttributeContext { get; set; }

        /// <summary>
        /// Gets the attribute group members.
        /// </summary>
        public CdmCollection<CdmAttributeItem> Members { get; }

        [Obsolete]
        public override CdmObjectType GetObjectType()
        {
            return CdmObjectType.AttributeGroupDef;
        }

        public override bool IsDerivedFrom(string baseDef, ResolveOptions resOpt = null)
        {
            if (resOpt == null)
            {
                resOpt = new ResolveOptions(this);
            }

            return false;
        }

        [Obsolete("CopyData is deprecated. Please use the Persistence Layer instead.")]
        public override dynamic CopyData(ResolveOptions resOpt, CopyOptions options)
        {
            return CdmObjectBase.CopyData<CdmAttributeGroupDefinition>(this, resOpt, options);
        }

        public override CdmObject Copy(ResolveOptions resOpt = null)
        {
            if (resOpt == null)
            {
                resOpt = new ResolveOptions(this);
            }

            CdmAttributeGroupDefinition copy = new CdmAttributeGroupDefinition(this.Ctx, this.AttributeGroupName)
            {
                AttributeContext = (CdmAttributeContextReference)this.AttributeContext?.Copy(resOpt)
            };
            foreach (var newMember in this.Members)
                copy.Members.Add(newMember);
            this.CopyDef(resOpt, copy);
            return copy;
        }

        public override bool Validate()
        {
            return !string.IsNullOrEmpty(this.AttributeGroupName);
        }

        internal CdmAttributeItem AddAttributeDef(CdmAttributeItem attributeDef)
        {
            this.Members.Add(attributeDef);
            return attributeDef;
        }

        public ResolvedEntityReferenceSet FetchResolvedEntityReferences(ResolveOptions resOpt = null)
        {
            if (resOpt == null)
            {
                resOpt = new ResolveOptions(this);
            }

            ResolvedEntityReferenceSet rers = new ResolvedEntityReferenceSet(resOpt);
            if (this.Members != null)
            {
                for (int i = 0; i < this.Members.Count; i++)
                {
                    rers.Add(this.Members.AllItems[i].FetchResolvedEntityReferences(resOpt));
                }
            }
            return rers;
        }

        /// <inheritdoc />
        public override bool Visit(string pathFrom, VisitCallback preChildren, VisitCallback postChildren)
        {
            string path = this.DeclaredPath;
            if (string.IsNullOrEmpty(path))
            {
                path = pathFrom + this.AttributeGroupName;
                this.DeclaredPath = path;
            }
            //trackVisits(path);

            if (preChildren?.Invoke(this, path) == true)
                return false;
            if (this.AttributeContext?.Visit(path + "/attributeContext/", preChildren, postChildren) == true)
                return true;
            if (this.Members != null)
                if (this.Members.VisitList(path + "/members/", preChildren, postChildren))
                    return true;
            if (this.VisitDef(path, preChildren, postChildren))
                return true;

            if (postChildren != null && postChildren.Invoke(this, path))
                return true;
            return false;
        }

        public override string GetName()
        {
            return this.AttributeGroupName;
        }

        internal CdmCollection<CdmAttributeItem> MembersAttributeDefs
        {
            get
            {
                return this.Members;
            }
        }

        internal override ResolvedAttributeSetBuilder ConstructResolvedAttributes(ResolveOptions resOpt, CdmAttributeContext under = null)
        {
            ResolvedAttributeSetBuilder rasb = new ResolvedAttributeSetBuilder();
            if (under != null)
            {
                AttributeContextParameters acpAttGrp = new AttributeContextParameters
                {
                    under = under,
                    type = CdmAttributeContextType.AttributeGroup,
                    Name = this.GetName(),
                    Regarding = this,
                    IncludeTraits = false
                };
                under = rasb.ResolvedAttributeSet.CreateAttributeContext(resOpt, acpAttGrp);
            }

            if (this.Members != null)
            {
                for (int i = 0; i < this.Members.Count; i++)
                {
                    dynamic att = this.Members.AllItems[i];
                    CdmAttributeContext attUnder = under;
                    AttributeContextParameters acpAtt = null;
                    if (under != null)
                    {
                        acpAtt = new AttributeContextParameters
                        {
                            under = under,
                            type = CdmAttributeContextType.AttributeDefinition,
                            Name = att.FetchObjectDefinitionName(),
                            Regarding = att,
                            IncludeTraits = false
                        };
                    }
                    rasb.MergeAttributes(att.FetchResolvedAttributes(resOpt, acpAtt));
                }
            }
            rasb.ResolvedAttributeSet.AttributeContext = under;

            // things that need to go away
            rasb.RemoveRequestedAtts();
            return rasb;
        }

        internal override void ConstructResolvedTraits(ResolvedTraitSetBuilder rtsb, ResolveOptions resOpt)
        {
            // get only the elevated traits from attribute first, then add in all traits from this definition
            if (this.Members != null)
            {
                ResolvedTraitSet rtsElevated = new ResolvedTraitSet(resOpt);
                for (int i = 0; i < this.Members.Count; i++)
                {
                    dynamic att = this.Members.AllItems[i];
                    ResolvedTraitSet rtsAtt = att.FetchResolvedTraits(resOpt);
                    if (rtsAtt?.HasElevated == true)
                        rtsElevated = rtsElevated.MergeSet(rtsAtt, true);
                }
                rtsb.MergeTraits(rtsElevated);
            }
            this.ConstructResolvedTraitsDef(null, rtsb, resOpt);
        }
    }
}
