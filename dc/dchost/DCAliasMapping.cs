using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;

namespace dchost
{
    public class DCAliasMapping : Dictionary<string, string>, IDisposable
    {
        public DCAliasMapping()
        {
        }

        public void Serialize(Stream stream)
        {
            BinaryWriter writer = new BinaryWriter(stream);
            writer.Write(this.Count);
            foreach (var kvp in this)
            {
                writer.Write(kvp.Key.ToLower());
                writer.Write(kvp.Value.ToLower());
                writer.Flush();
            }
        }

        public void Deserialize(Stream stream)
        {
            BinaryReader reader = new BinaryReader(stream);
            int count = reader.ReadInt32();
            for (int n = 0; n < count; n++)
            {
                var key = reader.ReadString();
                var value = reader.ReadString();
                if (!this.ContainsKey(key))
                {
                    this[key] = value;
                }
            }
        }

        public void Dispose()
        {

        }
    }
}
