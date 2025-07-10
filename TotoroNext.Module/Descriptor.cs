﻿using System.Diagnostics;
using System.Reflection;

namespace TotoroNext.Module;

[DebuggerDisplay("{Name} - {Version}")]
public class Descriptor
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public Version Version { get; } = Assembly.GetCallingAssembly().GetName().Version ?? new Version(1, 0, 0);
    
    public string EntryPoint { get; } = Assembly.GetCallingAssembly().GetName().Name ?? "";
    
    public List<string> Components { get; init; } = [];
    
    public string? Description { get; init; }
    
    public string? HeroImage { get; init; }
    
    public Type? SettingViewModel { get; init; }
    
    public bool IsInternal { get; set; } = false;
}