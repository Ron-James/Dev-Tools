using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

// Default file-based storage under Application.persistentDataPath
[Serializable]
public sealed class PersistentDataStorage : IDataStorage
{
    [SerializeField] private string folderName = "Saves";
    [SerializeField] private string fileExtension = ".save";

    public string RootFolder => Path.Combine(Application.persistentDataPath, folderName);
    public string FileExtension => fileExtension;

    public void EnsureReady()
    {
        var root = RootFolder;
        if (!Directory.Exists(root)) Directory.CreateDirectory(root);
    }

    public string MakeSafe(string name)
    {
        foreach (var c in Path.GetInvalidFileNameChars()) name = name.Replace(c, '_');
        return string.IsNullOrWhiteSpace(name) ? "Save" : name.Trim();
    }

    public string GetPath(string slotName) => Path.Combine(RootFolder, MakeSafe(slotName) + fileExtension);

    public Task Write(string slotName, byte[] data)
    {
        var path = GetPath(slotName);
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
        File.WriteAllBytes(path, data);
        return Task.CompletedTask;
    }

    public Task<byte[]> Read(string slotName)
    {
        var bytes = File.ReadAllBytes(GetPath(slotName));
        return Task.FromResult(bytes);
    }

    public bool Exists(string slotName) => File.Exists(GetPath(slotName));

    public Task Delete(string slotName)
    {
        var p = GetPath(slotName);
        if (File.Exists(p)) File.Delete(p);
        return Task.CompletedTask;
    }

    public Task DeleteAll(IEnumerable<string> slotNames)
    {
        if (slotNames != null)
        {
            foreach (var name in slotNames)
            {
                var p = GetPath(name);
                if (File.Exists(p)) File.Delete(p);
            }
        }
        return Task.CompletedTask;
    }
}
