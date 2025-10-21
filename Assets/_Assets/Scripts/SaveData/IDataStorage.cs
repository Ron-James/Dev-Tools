using System.Collections.Generic;
using System.Threading.Tasks;

// Storage abstraction for save slots (file I/O or other backends)
public interface IDataStorage
{
    string RootFolder { get; }
    string FileExtension { get; }
    void EnsureReady();
    string GetPath(string slotName);
    string MakeSafe(string name);
    Task Write(string slotName, byte[] data);
    Task<byte[]> Read(string slotName);
    bool Exists(string slotName);
    Task Delete(string slotName);
    Task DeleteAll(IEnumerable<string> slotNames);
}

