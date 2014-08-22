using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TOPLib.Util.DotNet.Persistence.Util
{

    public class ReadOnlyDictionary<K, V> : IEnumerable<KeyValuePair<K, V>>
    {
        private IDictionary<K, V> data = null;

        public ReadOnlyDictionary(IDictionary<K, V> data)
        {
            this.data = data;
        }

        public IEnumerator<KeyValuePair<K, V>> GetEnumerator()
        {
            return data.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return data.GetEnumerator();
        }

        public V this[K key]
        {
            get
            {
                return data[key];
            }
        }
    }
}
