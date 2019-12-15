﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CdmManifestDeclarationDefinition.cs" company="Microsoft">
//      All rights reserved.
// </copyright>
// <summary>
//   The object model implementation for Folder Declaration.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Microsoft.CommonDataModel.ObjectModel.Cdm
{
    using Microsoft.CommonDataModel.ObjectModel.Enums;
    using Microsoft.CommonDataModel.ObjectModel.Persistence.CdmFolder.Types;
    using Microsoft.CommonDataModel.ObjectModel.ResolvedModel;
    using Microsoft.CommonDataModel.ObjectModel.Utilities;
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// The object model implementation for manifest Declaration.
    /// </summary>
    public class CdmManifestDeclarationDefinition : CdmObjectDefinitionBase, CdmFileStatus
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CdmManifestDeclarationDefinition"/> class.
        /// </summary>
        /// <param name="ctx"> The context. </param>
        /// <param name="manifestName"> The manifest name. </param>
        public CdmManifestDeclarationDefinition(CdmCorpusContext ctx, string name) : base(ctx)
        {
            this.ObjectType = CdmObjectType.ManifestDeclarationDef;
            this.ManifestName = name;
        }

        /// <summary>
        /// The name of the manifest declared.
        /// </summary>
        public string ManifestName { get; set; }

        /// <summary>
        /// The definition.
        /// </summary>
        public string Definition { get; set; }


        /// <summary>
        /// The last file status check time.
        /// </summary>
        public DateTimeOffset? LastFileStatusCheckTime { get; set; }

        /// <summary>
        /// The last file modified time.
        /// </summary>
        public DateTimeOffset? LastFileModifiedTime { get; set; }

        /// <summary>
        /// The last child file modified time.
        /// </summary>
        public DateTimeOffset? LastChildFileModifiedTime { get; set; }

        /// <summary>
        /// Creates an instance from object of folder declaration type.
        /// </summary>
        /// <param name="ctx"> The context. </param>
        /// <param name="obj"> The object to read data from. </param>
        /// <returns> The <see cref="CdmManifestDeclarationDefinition"/> instance generated. </returns>
        [Obsolete("InstanceFromData is deprecated. Please use the Persistence Layer instead.")]
        public static CdmManifestDeclarationDefinition InstanceFromData(CdmCorpusContext ctx, ManifestDeclaration obj)
        {
            return CdmObjectBase.InstanceFromData<CdmManifestDeclarationDefinition, ManifestDeclaration>(ctx, obj);
        }

        /// <inheritdoc />
        [Obsolete]
        public override CdmObjectType GetObjectType()
        {
            return CdmObjectType.ManifestDeclarationDef;
        }

        [Obsolete("CopyData is deprecated. Please use the Persistence Layer instead.")]
        public override dynamic CopyData(ResolveOptions resOpt, CopyOptions options)
        {
            return CdmObjectBase.CopyData<CdmManifestDeclarationDefinition>(this, resOpt, options);
        }

        /// <inheritdoc />
        public override CdmObject Copy(ResolveOptions resOpt = null)
        {
            if (resOpt == null)
            {
                resOpt = new ResolveOptions(this);
            }

            var copy = new CdmManifestDeclarationDefinition(this.Ctx, this.ManifestName)
            {
                Definition = this.Definition,
                LastFileStatusCheckTime = this.LastFileStatusCheckTime,
                LastFileModifiedTime = this.LastFileModifiedTime
            };
            this.CopyDef(resOpt, copy);

            return copy;
        }

        /// <inheritdoc />
        public override bool Validate()
        {
            return !string.IsNullOrWhiteSpace(this.ManifestName) && !string.IsNullOrWhiteSpace(this.Definition);
        }


        /// <inheritdoc />
        public override string GetName()
        {
            return this.ManifestName;
        }

        /// <inheritdoc />
        public override bool Visit(string pathFrom, VisitCallback preChildren, VisitCallback postChildren)
        {
            return false;
        }

        /// <inheritdoc />
        public override bool IsDerivedFrom(string baseName, ResolveOptions resOpt = null)
        {
            if (resOpt == null)
            {
                resOpt = new ResolveOptions(this);
            }

            return false;
        }

        /// <inheritdoc />
        internal override void ConstructResolvedTraits(ResolvedTraitSetBuilder rtsb, ResolveOptions resOpt)
        {
            return;
        }

        /// <inheritdoc />
        internal override ResolvedAttributeSetBuilder ConstructResolvedAttributes(ResolveOptions resOpt, CdmAttributeContext under = null)
        {
            return null;
        }

        /// <inheritdoc />
        public async Task FileStatusCheckAsync()
        {
            string manifestPath = this.GetManifestPath();
            DateTimeOffset? modifiedTime = await (this.Ctx.Corpus as CdmCorpusDefinition).ComputeLastModifiedTimeAsync(manifestPath);

            // update modified times
            this.LastFileStatusCheckTime = DateTimeOffset.UtcNow;
            this.LastFileModifiedTime = TimeUtils.MaxTime(modifiedTime, this.LastFileModifiedTime);

            await this.ReportMostRecentTimeAsync(this.LastFileModifiedTime);
        }

        /// <summary>
        /// Returns the absolute path to the manifest file that this manifest declaration points to
        /// </summary>
        private string GetManifestPath()
        {
            string nameSpace = this.InDocument.Namespace;
            string prefixPath = this.InDocument.FolderPath;
            return $"{nameSpace}:{prefixPath}{(this.Definition.StartsWith("/") ? StringUtils.Slice(this.Definition, 1) : this.Definition)}";
        }

        /// <inheritdoc />
        public async Task ReportMostRecentTimeAsync(DateTimeOffset? childTime)
        {
            if (this.Owner is CdmFileStatus && childTime != null)
                await (this.Owner as CdmFileStatus).ReportMostRecentTimeAsync(childTime);
        }
    }
}