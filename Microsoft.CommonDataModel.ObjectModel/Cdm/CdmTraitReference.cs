﻿//-----------------------------------------------------------------------
// <copyright file="CdmTraitReference.cs" company="Microsoft">
//      All rights reserved.
// </copyright>
//-----------------------------------------------------------------------
namespace Microsoft.CommonDataModel.ObjectModel.Cdm
{
    using Microsoft.CommonDataModel.ObjectModel.Enums;
    using Microsoft.CommonDataModel.ObjectModel.ResolvedModel;
    using Microsoft.CommonDataModel.ObjectModel.Utilities;
    using System;
    using System.Collections.Generic;

    public class CdmTraitReference : CdmObjectReferenceBase
    {
        /// <summary>
        /// Gets the trait reference argument.
        /// </summary>
        public CdmArgumentCollection Arguments { get; }

        /// <summary>
        /// Gets or sets true if the trait was generated from a property and false it was directly loaded.
        /// </summary>
        public bool IsFromProperty { get; set; }

        internal bool ResolvedArguments;

        public CdmTraitReference(CdmCorpusContext ctx, dynamic trait, bool simpleReference, bool hasArguments)
            : base(ctx, (object)trait, simpleReference)
        {
            this.ObjectType = CdmObjectType.TraitRef;
            this.ResolvedArguments = false;
            this.IsFromProperty = false;
            this.Arguments = new CdmArgumentCollection(this.Ctx, this);
        }

        internal override CdmObjectReferenceBase CopyRefObject(ResolveOptions resOpt, dynamic refTo, bool simpleReference)
        {
            CdmTraitReference copy = new CdmTraitReference(this.Ctx, refTo, simpleReference, this.Arguments?.Count > 0);
            if (!simpleReference)
            {
                copy.ResolvedArguments = this.ResolvedArguments;
            }
            foreach (var arg in this.Arguments)
                copy.Arguments.Add(arg);

            return copy;
        }

        [Obsolete("CopyData is deprecated. Please use the Persistence Layer instead.")]
        public override dynamic CopyData(ResolveOptions resOpt, CopyOptions options)
        {
            return CdmObjectBase.CopyData<CdmTraitReference>(this, resOpt, options);
        }

        /// returns a map from parameter names to the final argument values for a trait reference
        /// values come (in this order) from base trait defaults then default overrides on inheritence
        /// then values supplied on this reference
        internal Dictionary<string, dynamic> GetFinalArgumentValues(ResolveOptions resOpt)
        {
            Dictionary<string, dynamic> finalArgs = new Dictionary<string, dynamic>();
            // get resolved traits does all the work, just clean up the answers
            ResolvedTraitSet rts = this.FetchResolvedTraits(resOpt);
            if (rts == null)
            {
                return null;
            }
            // there is only one resolved trait
            ResolvedTrait rt = rts.First;
            if (rt.ParameterValues != null && rt.ParameterValues.Length > 0)
            {
                int l = rt.ParameterValues.Length;
                for (int i = 0; i < l; i++)
                {
                    var p = rt.ParameterValues.FetchParameterAtIndex(i);
                    dynamic v = rt.ParameterValues.FetchValue(i);
                    string name = p.Name;
                    if (name == null)
                    {
                        name = i.ToString();
                    }
                    finalArgs.Add(name, v);
                }
            }

            return finalArgs;
        }

        [Obsolete]
        public override CdmObjectType GetObjectType()
        {
            return CdmObjectType.TraitRef;
        }

        internal override bool VisitRef(string pathFrom, VisitCallback preChildren, VisitCallback postChildren)
        {
            if (this.Arguments != null)
                if (this.Arguments.VisitList(pathFrom + "/arguments/", preChildren, postChildren))
                    return true;
            return false;
        }

        internal override ResolvedAttributeSetBuilder ConstructResolvedAttributes(ResolveOptions resOpt, CdmAttributeContext under = null)
        {
            return null;
        }

        internal override ResolvedTraitSet FetchResolvedTraits(ResolveOptions resOpt = null)
        {
            if (resOpt == null)
            {
                resOpt = new ResolveOptions(this);
            }

            const string kind = "rtsb";
            ResolveContext ctx = this.Ctx as ResolveContext;
            // get referenced trait
            CdmTraitDefinition trait = this.FetchObjectDefinition<CdmTraitDefinition>(resOpt) as CdmTraitDefinition;
            ResolvedTraitSet rtsTrait = null;
            if (trait == null)
                return ((CdmCorpusDefinition)ctx.Corpus).CreateEmptyResolvedTraitSet(resOpt);

            // see if one is already cached
            // cache by name unless there are parameter
            if (trait.ThisIsKnownToHaveParameters == null)
            {
                // never been resolved, it will happen soon, so why not now?
                rtsTrait = trait.FetchResolvedTraits(resOpt);
            }

            bool cacheByName = true;
            if (trait.ThisIsKnownToHaveParameters != null)
            {
                cacheByName = !((bool)trait.ThisIsKnownToHaveParameters);
            }

            string cacheTag = ((CdmCorpusDefinition)ctx.Corpus).CreateDefinitionCacheTag(resOpt, this, kind, "", cacheByName);
            dynamic rtsResult = null;
            if (cacheTag != null)
                ctx.Cache.TryGetValue(cacheTag, out rtsResult);

            // store the previous reference symbol set, we will need to add it with
            // children found from the constructResolvedTraits call
            SymbolSet currSymRefSet = resOpt.SymbolRefSet;
            if (currSymRefSet == null)
                currSymRefSet = new SymbolSet();
            resOpt.SymbolRefSet = new SymbolSet();

            // if not, then make one and save it
            if (rtsResult == null)
            {
                // get the set of resolutions, should just be this one trait
                if (rtsTrait == null)
                {
                    // store current symbol ref set
                    SymbolSet newSymbolRefSet = resOpt.SymbolRefSet;
                    resOpt.SymbolRefSet = new SymbolSet();

                    rtsTrait = trait.FetchResolvedTraits(resOpt);

                    // bubble up symbol reference set from children
                    if (newSymbolRefSet != null)
                    {
                        newSymbolRefSet.Merge(resOpt.SymbolRefSet);
                    }
                    resOpt.SymbolRefSet = newSymbolRefSet;
                }
                if (rtsTrait != null)
                    rtsResult = rtsTrait.DeepCopy();

                // now if there are argument for this application, set the values in the array
                if (this.Arguments != null && rtsResult != null)
                {
                    // if never tried to line up arguments with parameters, do that
                    if (!this.ResolvedArguments)
                    {
                        this.ResolvedArguments = true;
                        ParameterCollection param = trait.FetchAllParameters(resOpt);
                        CdmParameterDefinition paramFound = null;
                        dynamic aValue = null;

                        int iArg = 0;
                        if (this.Arguments != null)
                        {
                            foreach (CdmArgumentDefinition a in this.Arguments)
                            {
                                paramFound = param.ResolveParameter(iArg, a.Name);
                                a.ResolvedParameter = paramFound;
                                aValue = a.Value;
                                aValue = ((CdmCorpusDefinition)ctx.Corpus).ConstTypeCheck(resOpt, paramFound, aValue);
                                a.Value = aValue;
                                iArg++;
                            }
                        }
                    }
                    if (this.Arguments != null)
                    {
                        foreach (CdmArgumentDefinition a in this.Arguments)
                        {
                            rtsResult.SetParameterValueFromArgument(trait, a);
                        }
                    }
                }

                // register set of possible symbols
                ((CdmCorpusDefinition)ctx.Corpus).RegisterDefinitionReferenceSymbols(this.FetchObjectDefinition<CdmObjectDefinition>(resOpt), kind, resOpt.SymbolRefSet);

                // get the new cache tag now that we have the list of symbols
                cacheTag = ((CdmCorpusDefinition)ctx.Corpus).CreateDefinitionCacheTag(resOpt, this, kind, "", cacheByName);
                if (!string.IsNullOrWhiteSpace(cacheTag))
                    ctx.Cache[cacheTag] = rtsResult;
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
            currSymRefSet.Merge(resOpt.SymbolRefSet);
            resOpt.SymbolRefSet = currSymRefSet;

            return rtsResult;
        }

        internal override void ConstructResolvedTraits(ResolvedTraitSetBuilder rtsb, ResolveOptions resOpt)
        {
            return;
        }
    }
}
