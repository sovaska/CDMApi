using System;
using System.Collections.Generic;
using System.Linq;

namespace CDMApi.Features.Shared
{
    public class CDMQuery<T1>
    {
        protected List<T1> Entities { get; set; }

        public List<T> Query<T>(Predicate<T1> match, Func<T1, T> selector, int skip = 0, int take = 0)
        {
            var results = Entities.FindAll(match);
            if (skip > 0)
            {
                results = results.Skip(skip).ToList();
            }
            if (take > 0)
            {
                results = results.Take(take).ToList();
            }
            return results.Select(selector).ToList();
        }
    }
}