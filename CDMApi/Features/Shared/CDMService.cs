using System.Collections.Generic;
using System.Threading.Tasks;

namespace CDMApi.Features.Shared
{
    public class CDMService<T> : CDMQuery<T>
    {
        private readonly CDMMetadataRepository _metadataRepository;

        public CDMService(CDMMetadataRepository metadataRepository)
        {
            _metadataRepository = metadataRepository;
        }

        public async Task<bool> InitializeAsync()
        {
            var items = await _metadataRepository.ReadDataAsync<T>().ConfigureAwait(false);

            Entities = new List<T>(items.Count - 1);
            Entities.AddRange(items);

            return true;
        }
    }
}