using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BuffAlert.Classes;

namespace BuffAlert.Controllers;

public class ModuleController : IDisposable {
    public List<ModuleBase> Modules { get; } = [..ActivateModules()];

    public ModuleBase? GetModule(ModuleName moduleName)
        => Modules.FirstOrDefault(m => m.ModuleName == moduleName);

    private static IEnumerable<ModuleBase> ActivateModules()
        => Assembly.GetExecutingAssembly()
            .GetTypes()
            .Where(type => type.IsSubclassOf(typeof(ModuleBase)))
            .Where(type => !type.IsAbstract)
            .Select(type => (ModuleBase?)Activator.CreateInstance(type))
            .OfType<ModuleBase>();
    
    public void Dispose() {
        foreach (var module in Modules.OfType<IDisposable>()) {
            module.Dispose();
        }
    }
    
    public void Load() {
        foreach (var module in Modules) {
            module.Load();
        }
    }
    
    public List<WarningState> EvaluateWarnings() {
        var warningList = new List<WarningState>();
        
        foreach (var module in Modules) {
            module.EvaluateWarnings();

            if (module.HasWarnings) {
                warningList.AddRange(module.ActiveWarningStates);
            }
        }

        return warningList;
    }
}