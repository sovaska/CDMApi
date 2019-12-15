﻿//-----------------------------------------------------------------------
// <copyright file="CdmDocumentDefinition.cs" company="Microsoft">
//      All rights reserved.
// </copyright>
//-----------------------------------------------------------------------
namespace Microsoft.CommonDataModel.ObjectModel.Cdm
{
    using Microsoft.CommonDataModel.ObjectModel.Enums;
    using Microsoft.CommonDataModel.ObjectModel.Persistence.CdmFolder.Types;
    using Microsoft.CommonDataModel.ObjectModel.ResolvedModel;
    using Microsoft.CommonDataModel.ObjectModel.Utilities;
    using System.Collections.Generic;
    using System.Collections.Concurrent;
    using System.Threading.Tasks;
    using System;
    using Microsoft.CommonDataModel.ObjectModel.Utilities.Logging;

    internal class ImportPriorities
    {
        internal IDictionary<CdmDocumentDefinition, int> ImportPriority;
        internal IDictionary<string, CdmDocumentDefinition> MonikerPriorityMap;

        internal ImportPriorities()
        {
            this.ImportPriority = new Dictionary<CdmDocumentDefinition, int>();
            this.MonikerPriorityMap = new Dictionary<string, CdmDocumentDefinition>();
        }

        internal ImportPriorities Copy()
        {
            ImportPriorities c = new ImportPriorities();
            if (this.ImportPriority != null)
            {
                foreach (KeyValuePair<CdmDocumentDefinition, int> pair in this.ImportPriority)
                    c.ImportPriority[pair.Key] = pair.Value;
                foreach (KeyValuePair<string, CdmDocumentDefinition> pair in this.MonikerPriorityMap)
                    c.MonikerPriorityMap[pair.Key] = pair.Value;
            }
            return c;
        }
    }

    public class CdmDocumentDefinition : CdmObjectSimple, CdmContainerDefinition
    {
        internal ConcurrentDictionary<string, CdmObjectDefinitionBase> InternalDeclarations;
        internal ImportPriorities ImportPriorities;
        internal bool NeedsIndexing;
        internal bool IsDirty = true;

        [Obsolete("Only for internal use")]
        public string FolderPath { get; set; }

        [Obsolete("Only for internal use")]
        public string Namespace { get; set; }
        internal bool ImportsIndexed { get; set; }
        internal bool CurrentlyIndexing { get; set; }
        internal DateTimeOffset? _fileSystemModifiedTime { get; set; }

        public CdmDocumentDefinition(CdmCorpusContext ctx, string name)
            : base(ctx)
        {
            this.DocCreatedIn = this;
            this.ObjectType = CdmObjectType.DocumentDef;
            this.Name = name;
            this.JsonSchemaSemanticVersion = "0.9.0";
            this.NeedsIndexing = true;
            this.IsDirty = true;
            this.ImportsIndexed = false;
            this.CurrentlyIndexing = false;

            this.ClearCaches();

            this.Imports = new CdmImportCollection(this.Ctx, this);
            this.Definitions = new CdmDefinitionCollection(this.Ctx, this);
        }    

        /// <summary>
        /// Gets or sets the document name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the document schema.
        /// </summary>
        public string Schema { get; set; }

        /// <summary>
        /// Gets or sets the document json schema semantic version.
        /// </summary>
        public string JsonSchemaSemanticVersion { get; set; }

        /// <summary>
        /// Gets or sets the document folder.
        /// </summary>
        [Obsolete("Use the Owner property instead")]
        public CdmFolderDefinition Folder { get; set; }

        /// <summary>
        /// Gets the document definitions.
        /// </summary>
        public CdmDefinitionCollection Definitions { get; }

        /// <inheritdoc />
        public CdmImportCollection Imports { get; }

        internal void ClearCaches()
        {
            this.InternalDeclarations = new ConcurrentDictionary<string, CdmObjectDefinitionBase>();
        }

         public override CdmObject Copy(ResolveOptions resOpt = null)
        {
            if (resOpt == null)
            {
                resOpt = new ResolveOptions(this);
            }

            CdmDocumentDefinition c = new CdmDocumentDefinition(this.Ctx, this.Name)
            {
                Ctx = this.Ctx,
                IsDirty = true,
                FolderPath = this.FolderPath,
                Schema = this.Schema,
                JsonSchemaSemanticVersion = this.JsonSchemaSemanticVersion,
            };
            foreach (var def in this.Definitions)
                c.Definitions.Add(def);
            foreach (var imp in this.Imports)
                c.Imports.Add(imp);
            return c;
        }

        [Obsolete("CopyData is deprecated. Please use the Persistence Layer instead.")]
        public override dynamic CopyData(ResolveOptions resOpt, CopyOptions options)
        {
            return CdmObjectBase.CopyData<CdmDocumentDefinition>(this, resOpt, options);
        }

        internal CdmObject FetchObjectFromDocumentPath(string objectPath)
        {
            // in current document?
            if (this.InternalDeclarations.ContainsKey(objectPath))
                return this.InternalDeclarations[objectPath];
            return null;
        }

        [Obsolete]
        public override CdmObjectType GetObjectType()
        {
            return CdmObjectType.DocumentDef;
        }

        public override T FetchObjectDefinition<T>(ResolveOptions resOpt = null)
        {
            if (resOpt == null)
            {
                resOpt = new ResolveOptions(this);
            }

            return default(T);
        }

        public override bool Validate()
        {
            return !string.IsNullOrEmpty(this.Name);
        }

        public override string AtCorpusPath
        {
            get
            {
                return (this.Namespace != null ? this.Namespace : this.Folder.Namespace) + ":" + this.FolderPath + this.Name;
            }
        }

        /// <inheritdoc />
        public override bool Visit(string pathFrom, VisitCallback preChildren, VisitCallback postChildren)
        {
            if (preChildren != null && preChildren.Invoke(this, pathFrom))
                return false;
            if (this.Definitions != null)
                if (this.Definitions.VisitList(pathFrom, preChildren, postChildren))
                    return true;
            if (postChildren != null && postChildren.Invoke(this, pathFrom))
                return true;
            return false;
        }

        /// <summary>
        /// saves the document back through the adapter in the requested format
        /// format is specified via document name/extension based on conventions:
        /// 'model.json' for back compat model, '*.manifest.cdm.json' for manifest, '*.cdm.json' for cdm defs
        /// saveReferenced (default false) when true will also save any schema defintion documents that are
        /// linked from the source doc and that have been modified. existing document names are used for those.
        /// returns false on any failure
        /// </summary>
        public async Task<bool> SaveAsAsync(string newName, bool saveReferenced = false, CopyOptions options = null)
        {
            if (options == null)
            {
                options = new CopyOptions();
            }

            ResolveOptions resOpt = new ResolveOptions(this);
            if (await this.IndexIfNeeded(resOpt) == false)
            {
                Logger.Error(nameof(CdmDocumentDefinition), (ResolveContext)this.Ctx, $"Failed to index document prior to save '{this.Name}'", "SaveAsAsync");
                return false;
            }

            // if save to the same document name, then we are no longer 'dirty'
            if (newName == this.Name)
                this.IsDirty = false;

            if (await ((this.Ctx.Corpus) as CdmCorpusDefinition).SaveDocumentAs(this, options, newName, saveReferenced) == false)
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// updates indexes for document content, call this after modifying objects in the document
        /// </summary>
        public async Task<bool> RefreshAsync(ResolveOptions resOpt)
        {
            this.NeedsIndexing = true;
            return await this.IndexIfNeeded(resOpt);
        }

        internal async Task<bool> IndexIfNeeded(ResolveOptions resOpt)
        {
            if (this.NeedsIndexing)
            {
                // make the corpus internal machinery pay attention to this document for this call
                CdmCorpusDefinition corpus = (this.Folder as CdmFolderDefinition).Corpus;
                CdmDocumentDefinition oldDoc = this;

                ConcurrentDictionary<CdmDocumentDefinition, byte> docsJustAdded = new ConcurrentDictionary<CdmDocumentDefinition, byte>();
                ConcurrentDictionary<string, byte> docsNotFound = new ConcurrentDictionary<string, byte>();
                await corpus.ResolveImportsAsync(this, docsJustAdded, docsNotFound);

                // maintain actual current doc
                (corpus.Ctx as ResolveContext).CurrentDoc = oldDoc;
                (this.Ctx.Corpus.Ctx as ResolveContext).CurrentDoc = oldDoc;
                docsJustAdded[this] = 1;

                return corpus.IndexDocuments(resOpt, docsJustAdded);
            }

            return true;
        }

        internal override ResolvedAttributeSetBuilder ConstructResolvedAttributes(ResolveOptions resOpt, CdmAttributeContext under = null)
        {
            return null;
        }

        internal override void ConstructResolvedTraits(ResolvedTraitSetBuilder rtsb, ResolveOptions resOpt)
        {
            return;
        }

        [Obsolete("InstanceFromData is deprecated. Please use the Persistence Layer instead.")]
        public static CdmDocumentDefinition InstanceFromData(CdmCorpusContext ctx, dynamic obj)
        {
            return CdmObjectBase.InstanceFromData<CdmDocumentDefinition, DocumentContent>(ctx, obj);
        }

        internal ImportPriorities GetImportPriorities()
        {
            if (this.ImportPriorities == null)
            {
                this.ImportPriorities = new ImportPriorities();
                this.ImportPriorities.ImportPriority.Add(this, 0);
                this.PrioritizeImports(new HashSet<CdmDocumentDefinition>(), this.ImportPriorities.ImportPriority, 1, this.ImportPriorities.MonikerPriorityMap, false);
            }
            // make a copy so the caller doesn't mess these up
            return this.ImportPriorities.Copy();
        }

        private int PrioritizeImports(HashSet<CdmDocumentDefinition> processedSet, IDictionary<CdmDocumentDefinition, int> priorityMap, int sequence, IDictionary<string, CdmDocumentDefinition> monikerMap, bool skipMonikered = false)
        {
            // goal is to make a map from the reverse order of imports (breadth first) to the first (aka last) sequence number in that list.
            // This gives the semantic that the 'last/shallowest' definition for a duplicate symbol wins,
            // the lower in this list a document shows up, the higher priority its definitions are for resolving conflicts.
            // for 'moniker' imports, keep track of the 'last/shallowest' use of each moniker tag.

            // if already in list, don't do this again
            if (processedSet.Contains(this))
                return sequence;
            processedSet.Add(this);

            if (this.Imports != null)
            {
                // first add the imports done at this level only
                int l = this.Imports.Count;
                // reverse order
                for (int i = l - 1; i >= 0; i--)
                {
                    CdmImport imp = this.Imports.AllItems[i];
                    CdmDocumentDefinition impDoc = imp.ResolvedDocument as CdmDocumentDefinition;
                    // don't add the moniker imports to the priority list
                    bool isMoniker = !string.IsNullOrWhiteSpace(imp.Moniker);
                    if (imp.ResolvedDocument != null && !isMoniker)
                    {
                        if (priorityMap.ContainsKey(impDoc) == false)
                        {
                            // add doc
                            priorityMap.Add(impDoc, sequence);
                            sequence++;
                        }
                    }
                }

                // now add the imports of the imports
                for (int i = l - 1; i >= 0; i--)
                {
                    CdmImport imp = this.Imports.AllItems[i];
                    CdmDocumentDefinition impDoc = imp.ResolvedDocument as CdmDocumentDefinition;
                    // don't add the moniker imports to the priority list
                    bool isMoniker = !string.IsNullOrWhiteSpace(imp.Moniker);
                    if (impDoc?.ImportPriorities != null)
                    {
                        // lucky, already done so avoid recursion and copy
                        ImportPriorities impPriSub = impDoc.GetImportPriorities();
                        impPriSub.ImportPriority.Remove(impDoc); // because already added above
                        foreach (var ip in impPriSub.ImportPriority)
                        {
                            if (priorityMap.ContainsKey(ip.Key) == false)
                            {
                                // add doc
                                priorityMap.Add(ip.Key, sequence);
                                sequence++;
                            }
                        }
                        if (!isMoniker)
                        {
                            foreach (var mp in impPriSub.MonikerPriorityMap)
                            {
                                monikerMap[mp.Key] = mp.Value;
                            }
                        }
                    }
                    else if (impDoc != null)
                    {
                        // skip the monikered imports from here if this is a monikered import itself and we are only collecting the dependencies
                        sequence = impDoc.PrioritizeImports(processedSet, priorityMap, sequence, monikerMap, isMoniker);
                    }
                }
                // skip the monikered imports from here if this is a monikered import itself and we are only collecting the dependencies
                if (!skipMonikered)
                {
                    // moniker imports are prioritized by the 'closest' use of the moniker to the starting doc. so last one found in this recursion
                    for (int i = 0; i < l; i++)
                    {
                        CdmImport imp = this.Imports.AllItems[i];
                        if (imp.ResolvedDocument != null && imp.Moniker != null)
                        {
                            monikerMap[imp.Moniker] = imp.ResolvedDocument as CdmDocumentDefinition;
                        }
                    }
                }
            }
            return sequence;
        }

        internal async Task Reload()
        {
            await (this.Ctx.Corpus as CdmCorpusDefinition)._FetchObjectAsync(this.AtCorpusPath, null, true);
        }

        virtual internal async Task<bool> SaveLinkedDocuments(CopyOptions options = null)
        {
            if (options == null)
            {
                options = new CopyOptions();
            }

            // the only linked documents would be the imports
            if (this.Imports != null)
            {
                foreach (CdmImport imp in this.Imports)
                {
                    // get the document object from the import
                    string docPath = Ctx.Corpus.Storage.CreateAbsoluteCorpusPath(imp.CorpusPath, this);
                    var docImp = await Ctx.Corpus.FetchObjectAsync<CdmDocumentDefinition>(docPath);
                    if (docImp != null && docImp.IsDirty)
                    {
                        // save it with the same name
                        if (await docImp.SaveAsAsync(docImp.Name, true, options) == false)
                        {
                            Logger.Error(nameof(CdmDocumentDefinition), (ResolveContext)this.Ctx, $"Failed to save import '{docImp.Name}'", "SaveLinkedDocuments");
                            return false;
                        }
                    }
                }
            }
            return true;
        }
    }
}
