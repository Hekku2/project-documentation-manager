using System;
using System.Collections.Generic;
using System.IO;

namespace Desktop.Models;

public class FileSystemItem
{
    public string Name { get; set; } = string.Empty;
    public string FullPath { get; set; } = string.Empty;
    public bool IsDirectory { get; set; }
    public List<FileSystemItem> Children { get; set; } = new List<FileSystemItem>();
    public long Size { get; set; }
    public DateTime LastModified { get; set; }

    public bool HasChildren => Children.Count > 0;
    public string Extension => IsDirectory ? string.Empty : Path.GetExtension(Name);
    public string DisplayName => Name;
}