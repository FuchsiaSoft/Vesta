using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vesta.Misc
{
    class CollectionSplitter<T>
    {
        internal IEnumerable<IEnumerable<T>> SplitCollection
            (IEnumerable<T> masterList, int childCollections)
        {
            int itemCount = masterList.Count();

            if (itemCount == 0)
            {
                List<IEnumerable<T>> result = new List<IEnumerable<T>>();
                result.Add(masterList);
                return result;
            }

            if (itemCount <= childCollections)
            {
                return masterList.Select((f, i) => new { f, i })
                .GroupBy(x => x.i % masterList.Count())
                .Select(g => g.Select(x => x.f).ToList()).ToList();
            }

            return masterList.Select((f, i) => new { f, i })
                .GroupBy(x => x.i % childCollections)
                .Select(g => g.Select(x => x.f).ToList()).ToList();
        }
    }
}
