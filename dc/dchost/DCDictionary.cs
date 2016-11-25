using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;

namespace dchost
{
    public class DCDictionary : Dictionary<string, LinkedList<string>>, IDisposable
    {
        public DCDictionary()
        {
        }

        public void Add(DirectoryInfo dir)
        {
            var name = dir.Name.ToLower();
            if (!this.ContainsKey(name))
            {
                this[name] = new LinkedList<string>();
            }
            if(!this[name].Contains(dir.FullName))
            {
                this[name].AddLast(dir.FullName);
            }
        }
        public void Serialize(Stream stream)
        {
            BinaryWriter writer = new BinaryWriter(stream);
            writer.Write(this.Count);
            foreach (var kvp in this)
            {
                writer.Write(kvp.Key.ToLower());
                writer.Write(kvp.Value.Count());
                foreach (var path in kvp.Value)
                {
                    writer.Write(path);
                }
                writer.Flush();
            }
        }

        public void Merge(DCDictionary dcDictionary)
        {
            foreach(var kvp in dcDictionary)
            {
                if(this.ContainsKey(kvp.Key))
                {
                    foreach(var path in kvp.Value)
                    {
                        if(!this[kvp.Key].Contains(path))
                        {
                            this[kvp.Key].AddLast(path);
                        }
                    }
                }
                else
                {
                    this[kvp.Key] = new LinkedList<string>();
                    foreach (var path in kvp.Value)
                    {
                        this[kvp.Key].AddLast(path);
                    }
                }
            }
        }

        public void Deserialize(Stream stream)
        {
            BinaryReader reader = new BinaryReader(stream);
            int count = reader.ReadInt32();
            for (int n = 0; n < count; n++)
            {
                var key = reader.ReadString();
                if (!this.ContainsKey(key))
                {
                    this[key] = new LinkedList<string>();
                }
                var num = reader.ReadInt32();
                for (int i = 0; i < num; i++)
                {
                    var path = reader.ReadString();
                    this[key].AddLast(path);
                }
            }
        }

        public void Dispose()
        {

        }
    }
}
