using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CommonDataModel.ObjectModel.Cdm;
using Microsoft.CommonDataModel.ObjectModel.Storage;
using System.IO;
using Microsoft.Extensions.Options;

namespace CDMApi.Features.Shared
{
    public class CDMMetadataRepository : IDisposable
    {
        private bool disposedValue;
        private readonly ADLSAdapter _adapter;
        private readonly CdmCorpusDefinition _cdmCorpus;
        private CdmManifestDefinition _manifest;
        private readonly EntityGenerator _entityGenerator;

        public CDMMetadataRepository(EntityGenerator entityGenerator, IOptions<ADLSSettings> settings)
        {
            if (settings.Value.Hostname.Contains("[TODO]") ||
                settings.Value.Root.Contains("[TODO]") ||
                settings.Value.SharedKey.Contains("[TODO]"))
            {
                throw new Exception("Please set correct values for ADLS settings in appsettings.json");
            }
            _adapter = new ADLSAdapter(settings.Value.Hostname, settings.Value.Root, settings.Value.SharedKey);

            _cdmCorpus = new CdmCorpusDefinition();
            _cdmCorpus.Storage.Mount("adls", _adapter);
            _entityGenerator = entityGenerator;
        }

        public async Task<List<T>> ReadDataAsync<T>()
        {
            if (_manifest == null)
            {
                _manifest = await _cdmCorpus.FetchObjectAsync<CdmManifestDefinition>("adls:/model.json").ConfigureAwait(false);
            }

            var result = new List<T>();

            var entityName = typeof(T).Name;

            var folderDef = _manifest.InDocument.Owner as CdmFolderDefinition;
            foreach (var doc in folderDef.Documents)
            {
                var def = doc.Definitions.FirstOrDefault();
                if (def == null) { continue; }
                if (!(def is CdmEntityDefinition entityDefinition)) { continue; }

                if (!entityName.StartsWith(def.GetName(), StringComparison.InvariantCultureIgnoreCase))
                {
                    continue;
                }

                var entity = _manifest.Entities.FirstOrDefault(e => e.EntityName == def.GetName()) as CdmEntityDeclarationDefinition;

                foreach (var dataPart in entity.DataPartitions)
                {
                    var adapter = _cdmCorpus.Storage.FetchAdapter("adls");
                    var content = await adapter.ReadAsync(dataPart.Location.Replace("adls:", string.Empty)).ConfigureAwait(false);
                    var entities = _entityGenerator.BuildObjectModel<T>(entityDefinition, content);
                    result.AddRange(entities);
                }
            }

            return result;
        }

#if DEBUG
        /// <summary>
        /// This method can be used to create C# classes from CDM metadata. Execute once and you have Model classes in your project.
        /// </summary>
        public async Task CreateModelsAsync()
        {
            if (_manifest == null)
            {
                _manifest = await _cdmCorpus.FetchObjectAsync<CdmManifestDefinition>("adls:/model.json").ConfigureAwait(false);
            }

            var folderDef = _manifest.InDocument.Owner as CdmFolderDefinition;
            foreach (var doc in folderDef.Documents)
            {
                var def = doc.Definitions.FirstOrDefault();
                if (def == null) { continue; }
                if (!(def is CdmEntityDefinition entityDefinition)) { continue; }

                (var fileName, var content) = _entityGenerator.ParseObjectDefinition(entityDefinition);
                var path = Directory.GetCurrentDirectory();
                File.WriteAllText(@$"{path}\Features\CDMFolder\{fileName}", content);
            }
        }
#endif

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _adapter.Dispose();
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}