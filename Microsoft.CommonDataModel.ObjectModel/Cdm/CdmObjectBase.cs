﻿//-----------------------------------------------------------------------
// <copyright file="CdmObjectBase.cs" company="Microsoft">
//      All rights reserved.
// </copyright>
//-----------------------------------------------------------------------
namespace Microsoft.CommonDataModel.ObjectModel.Cdm
{
    using Microsoft.CommonDataModel.ObjectModel.Enums;
    using Microsoft.CommonDataModel.ObjectModel.Persistence;
    using Microsoft.CommonDataModel.ObjectModel.ResolvedModel;
    using Microsoft.CommonDataModel.ObjectModel.Utilities;
    using System;
    using System.Collections.Generic;

    public abstract class CdmObjectBase : CdmObject
    {
        public CdmObjectBase(CdmCorpusContext ctx)
        {
            this.Id = CdmCorpusDefinition.NextId();
            this.Ctx = ctx;
            if (ctx != null)
                this.DocCreatedIn = (ctx as ResolveContext).CurrentDoc;
        }

        public int Id { get; set; }
        public abstract CdmObject Copy(ResolveOptions resOpt = null);
        public abstract bool Validate();

        public abstract bool IsDerivedFrom(string baseDef, ResolveOptions resOpt = null);

        public CdmObjectType ObjectType { get; set; }
        public CdmCorpusContext Ctx { get; set; }
        internal CdmDocumentDefinition DocCreatedIn { get; set; }
        internal IDictionary<string, ResolvedTraitSetBuilder> TraitCache { get; set; }
        internal string DeclaredPath { get; set; }
        public CdmObject Owner { get; set; }

        [Obsolete]
        public abstract CdmObjectType GetObjectType();
        public abstract string FetchObjectDefinitionName();
        public abstract T FetchObjectDefinition<T>(ResolveOptions resOpt = null) where T : CdmObjectDefinition;
        public virtual string AtCorpusPath { get; set; }

        public virtual CdmDocumentDefinition InDocument
        {
            get { return this.DocCreatedIn; }
            set { this.DocCreatedIn = (CdmDocumentDefinition)value; }
        }

        internal virtual void ConstructResolvedTraits(ResolvedTraitSetBuilder rtsb, ResolveOptions resOpt)
        {
            return;
        }

        internal virtual ResolvedAttributeSetBuilder ConstructResolvedAttributes(ResolveOptions resOpt, CdmAttributeContext under = null)
        {
            return null;
        }

        private bool resolvingTraits = false;

        [Obsolete()]
        internal virtual ResolvedTraitSet FetchResolvedTraits(ResolveOptions resOpt = null)
        {
            if (resOpt == null)
            {
                resOpt = new ResolveOptions(this);
            }

            const string kind = "rtsb";
            ResolveContext ctx = this.Ctx as ResolveContext;
            string cacheTagA = ((CdmCorpusDefinition)ctx.Corpus).CreateDefinitionCacheTag(resOpt, this, kind);
            ResolvedTraitSetBuilder rtsbAll = null;
            if (this.TraitCache == null)
                this.TraitCache = new Dictionary<string, ResolvedTraitSetBuilder>();
            else if (!string.IsNullOrWhiteSpace(cacheTagA))
                this.TraitCache.TryGetValue(cacheTagA, out rtsbAll);

            // store the previous document set, we will need to add it with
            // children found from the constructResolvedTraits call
            SymbolSet currDocRefSet = resOpt.SymbolRefSet;
            if (currDocRefSet == null)
            {
                currDocRefSet = new SymbolSet();
            }
            resOpt.SymbolRefSet = new SymbolSet();

            if (rtsbAll == null)
            {
                rtsbAll = new ResolvedTraitSetBuilder();

                if (!resolvingTraits)
                {
                    resolvingTraits = true;
                    this.ConstructResolvedTraits(rtsbAll, resOpt);
                    resolvingTraits = false;
                }

                CdmObjectDefinitionBase objDef = this.FetchObjectDefinition<CdmObjectDefinitionBase>(resOpt);
                if (objDef != null)
                {
                    // register set of possible docs
                    ((CdmCorpusDefinition)ctx.Corpus).RegisterDefinitionReferenceSymbols(objDef, kind, resOpt.SymbolRefSet);

                    if (rtsbAll.ResolvedTraitSet == null)
                    {
                        // nothing came back, but others will assume there is a set in this builder
                        rtsbAll.ResolvedTraitSet = new ResolvedTraitSet(resOpt);
                    }
                    // get the new cache tag now that we have the list of docs
                    cacheTagA = ((CdmCorpusDefinition)ctx.Corpus).CreateDefinitionCacheTag(resOpt, this, kind);
                    if (!string.IsNullOrWhiteSpace(cacheTagA))
                        this.TraitCache[cacheTagA] = rtsbAll;
                }
            }
            else
            {
                // cache was found
                // get the SymbolSet for this cached object
                string key = CdmCorpusDefinition.CreateCacheKeyFromObject(this, kind);
                ((CdmCorpusDefinition)ctx.Corpus).DefinitionReferenceSymbols.TryGetValue(key, out SymbolSet tempDocRefSet);
                resOpt.SymbolRefSet = tempDocRefSet;
            }

            // merge child document set with current
            currDocRefSet.Merge(resOpt.SymbolRefSet);
            resOpt.SymbolRefSet = currDocRefSet;

            return rtsbAll.ResolvedTraitSet;
        }

        private bool resolvingAttributes = false;

        [Obsolete()]
        internal ResolvedAttributeSet FetchResolvedAttributes(ResolveOptions resOpt = null, AttributeContextParameters acpInContext = null)
        {
            if (resOpt == null)
            {
                resOpt = new ResolveOptions(this);
            }

            const string kind = "rasb";
            ResolveContext ctx = this.Ctx as ResolveContext;
            string cacheTag = ((CdmCorpusDefinition)ctx.Corpus).CreateDefinitionCacheTag(resOpt, this, kind, acpInContext != null ? "ctx" : "");
            dynamic rasbCache = null;
            if (cacheTag != null)
                ctx.Cache.TryGetValue(cacheTag, out rasbCache);
            CdmAttributeContext underCtx = null;

            // store the previous document set, we will need to add it with
            // children found from the constructResolvedTraits call
            SymbolSet currDocRefSet = resOpt.SymbolRefSet;
            if (currDocRefSet == null)
            {
                currDocRefSet = new SymbolSet();
            }
            resOpt.SymbolRefSet = new SymbolSet();

            // get the moniker that was found and needs to be appended to all
            // refs in the children attribute context nodes
            string fromMoniker = resOpt.FromMoniker;
            resOpt.FromMoniker = null;

            if (rasbCache == null)
            {
                if (this.resolvingAttributes)
                {
                    // re-entered this attribute through some kind of self or looping reference.
                    return new ResolvedAttributeSet();
                }
                this.resolvingAttributes = true;

                // if a new context node is needed for these attributes, make it now
                if (acpInContext != null)
                    underCtx = CdmAttributeContext.CreateChildUnder(resOpt, acpInContext);

                rasbCache = this.ConstructResolvedAttributes(resOpt, underCtx);
                this.resolvingAttributes = false;

                // register set of possible docs
                CdmObjectDefinition oDef = this.FetchObjectDefinition<CdmObjectDefinitionBase>(resOpt);
                if (oDef != null)
                {
                    ((CdmCorpusDefinition)ctx.Corpus).RegisterDefinitionReferenceSymbols(oDef, kind, resOpt.SymbolRefSet);

                    // get the new cache tag now that we have the list of docs
                    cacheTag = ((CdmCorpusDefinition)ctx.Corpus).CreateDefinitionCacheTag(resOpt, this, kind, acpInContext != null ? "ctx" : null);

                    // save this as the cached version
                    if (!string.IsNullOrWhiteSpace(cacheTag))
                        ctx.Cache[cacheTag] = rasbCache;

                    if (!string.IsNullOrWhiteSpace(fromMoniker) && acpInContext != null && (this as CdmObjectReferenceBase).NamedReference != null)
                    {
                        // create a fresh context
                        CdmAttributeContext oldContext = acpInContext.under.Contents[acpInContext.under.Contents.Count - 1] as CdmAttributeContext;
                        acpInContext.under.Contents.RemoveAt(acpInContext.under.Contents.Count - 1);
                        underCtx = CdmAttributeContext.CreateChildUnder(resOpt, acpInContext);

                        CdmAttributeContext newContext = oldContext.CopyAttributeContextTree(resOpt, underCtx, rasbCache.ResolvedAttributeSet, null, fromMoniker);
                        // since THIS should be a refererence to a thing found in a moniker document, it already has a moniker in the reference
                        // this function just added that same moniker to everything in the sub-tree but now this one symbol has too many
                        // remove one
                        string monikerPathAdded = $"{fromMoniker}/";
                        if (newContext.Definition != null && newContext.Definition.NamedReference != null &&
                            newContext.Definition.NamedReference.StartsWith(monikerPathAdded))
                        {
                            // slice it off the front
                            newContext.Definition.NamedReference = newContext.Definition.NamedReference.Substring(monikerPathAdded.Length);
                        }
                    }
                }
            }
            else
            {
                // cache found. if we are building a context, then fix what we got instead of making a new one
                if (acpInContext != null)
                {
                    // make the new context
                    underCtx = CdmAttributeContext.CreateChildUnder(resOpt, acpInContext);

                    rasbCache.ResolvedAttributeSet.AttributeContext.CopyAttributeContextTree(resOpt, underCtx, rasbCache.ResolvedAttributeSet, null, fromMoniker);
                }
            }

            // merge child document set with current
            currDocRefSet.Merge(resOpt.SymbolRefSet);
            resOpt.SymbolRefSet = currDocRefSet;

            return rasbCache?.ResolvedAttributeSet;
        }

        internal void ClearTraitCache()
        {
            this.TraitCache = null;
        }

        [Obsolete("CopyData is deprecated. Please use the Persistence Layer instead.")]
        public abstract dynamic CopyData(ResolveOptions resOpt = null, CopyOptions options = null);

        [Obsolete("InstanceFromData is deprecated. Please use the Persistence Layer instead.")]
        public static dynamic InstanceFromData<T, U>(CdmCorpusContext ctx, U obj)
            where T : CdmObject
        {
            string persistenceTypeName = "CdmFolder";
            return PersistenceLayer.FromData<T, U>(ctx, obj, persistenceTypeName);
        }

        [Obsolete("CopyData is deprecated. Please use the Persistence Layer instead.")]
        public static dynamic CopyData<T>(T instance, ResolveOptions resOpt = null, CopyOptions options = null)
             where T : CdmObject
        {
            if (resOpt == null)
            {
                resOpt = new ResolveOptions(instance);
            }

            if (options == null)
            {
                options = new CopyOptions();
            }

            string persistenceTypeName = "CdmFolder";
            return PersistenceLayer.ToData<T, dynamic>(instance, resOpt, options, persistenceTypeName);
        }

        internal static CdmCollection<T> ListCopy<T>(ResolveOptions resOpt, CdmObject owner, CdmCollection<T> source) where T : CdmObject
        {
            if (source == null)
                return null;
            CdmCollection<T> casted = new CdmCollection<T>(source.Ctx, owner, source.DefaultType);
            foreach (CdmObject element in source)
            {
                casted.Add(element != null ? (dynamic)element.Copy(resOpt) : null);
            }
            return casted;
        }

        /// <inheritdoc />
        public abstract bool Visit(string path, VisitCallback preChildren, VisitCallback postChildren);

        /// <summary>
        /// Calls the Visit function on all objects in the collection
        /// </summary>
        internal static bool VisitList(IEnumerable<dynamic> items, string path, VisitCallback preChildren, VisitCallback postChildren)
        {
            bool result = false;
            if (items != null)
            {
                foreach (CdmObjectBase element in items)
                {
                    if (element != null)
                    {
                        if (element.Visit(path, preChildren, postChildren))
                        {
                            result = true;
                            break;
                        }
                    }
                }
            }
            return result;
        }

        internal static CdmTraitReference ResolvedTraitToTraitRef(ResolveOptions resOpt, ResolvedTrait rt)
        {
            CdmTraitReference traitRef = null;
            if (rt.ParameterValues != null && rt.ParameterValues.Length > 0)
            {
                traitRef = rt.Trait.Ctx.Corpus.MakeObject<CdmTraitReference>(CdmObjectType.TraitRef, rt.TraitName, false);
                int l = rt.ParameterValues.Length;
                if (l == 1)
                {
                    // just one argument, use the shortcut syntax
                    dynamic val = rt.ParameterValues.Values[0];
                    if (val != null)
                    {
                        traitRef.Arguments.Add(null, val);
                    }
                }
                else
                {
                    for (int i = 0; i < l; i++)
                    {
                        CdmParameterDefinition param = rt.ParameterValues.FetchParameterAtIndex(i);
                        dynamic val = rt.ParameterValues.Values[i];
                        if (val != null)
                        {
                            traitRef.Arguments.Add(param.Name, val);
                        }
                    }
                }
            }
            else
                traitRef = rt.Trait.Ctx.Corpus.MakeObject<CdmTraitReference>(CdmObjectType.TraitRef, rt.TraitName, true);
            if (resOpt.SaveResolutionsOnCopy)
            {
                // used to localize references between documents
                traitRef.ExplicitReference = rt.Trait as CdmTraitDefinition;
                traitRef.DocCreatedIn = (rt.Trait as CdmTraitDefinition).DocCreatedIn;
            }
            return traitRef;
        }

        internal static ResolveOptions CopyResolveOptions(ResolveOptions resOpt)
        {
            ResolveOptions resOptCopy = new ResolveOptions();
            resOptCopy.WrtDoc = resOpt.WrtDoc;
            resOptCopy.RelationshipDepth = resOpt.RelationshipDepth;
            if (resOpt.Directives != null)
                resOptCopy.Directives = resOpt.Directives.Copy();
            resOptCopy.LocalizeReferencesFor = resOpt.LocalizeReferencesFor;
            resOptCopy.IndexingDoc = resOpt.IndexingDoc;
            return resOptCopy;
        }
        public abstract CdmObjectReference CreateSimpleReference(ResolveOptions resOpt = null);
    }
}
