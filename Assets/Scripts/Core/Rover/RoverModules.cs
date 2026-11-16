using System;
using System.Collections.Generic;

public class RoverBattery : GenericModule
{
    public float ah;
    public float price;
}

public class RoverArmor : GenericModule
{
    public int bangs;
    public float price;
}

public class RoverAntenna : GenericModule
{
    public float radius;
    public float speed;
    public float price;
}

public static class RoverModulesImplement
{
    public static IReadOnlyList<RoverBattery> batteryList = new RoverBattery[] {
        new() { name = "Niquelin", ah = 2000, price = 50, spriteClass = "niquelin-material" },
        new() { name = "Plomin", ah = 6000, price = 100, spriteClass = "plomin-material" },
        new() { name = "Hidraumax", ah = 8000, price = 150, spriteClass = "hidraumax-material" },
        new() { name = "Infinitum", ah = 10000, price = 250, spriteClass = "infinitum-material" }
    };

    public static IReadOnlyList<RoverArmor> armorList = new RoverArmor[] {
        new() { name = "Aluminum", bangs = 4, price = 300, spriteClass = "aluminium-armor" },
        new() { name = "Iron", bangs = 10, price = 500, spriteClass = "iron-armor" },
        new() { name = "Steel", bangs = 20, price = 800, spriteClass = "steel-armor" }
    };

    public static IReadOnlyList<RoverAntenna> antennaList = new RoverAntenna[] {
        new() { name = "TP-Radin", radius = 100f, speed = 5f, price = 50, spriteClass = "tp-radin" },
        new() { name = "TP-Rado", radius = 300f, speed = 2.5f, price = 100, spriteClass = "tp-radio" },
        new() { name = "TP-Radmax", radius = 500f, speed = 10f, price = 150, spriteClass = "tp-radmax" },
        new() { name = "TP-Supreme", radius = 900f, speed = 100f, price = 250, spriteClass = "tp-supreme" }
    };
}

public static class RoverModuleSelection
{
    private static readonly List<GenericModule> selectedModules = new List<GenericModule>();

    public static RoverArmor SelectedArmor { get; private set; }
    public static RoverBattery SelectedBattery { get; private set; }
    public static RoverAntenna SelectedAntenna { get; private set; }
    public static IReadOnlyList<GenericModule> SelectedModules => selectedModules;
    public static bool IsConfigurationComplete => SelectedArmor != null && SelectedBattery != null && SelectedAntenna != null;

    public static event Action OnSelectionChanged;

    public static void SelectModule(GenericModule module)
    {
        if (module is RoverArmor armor)
        {
            SelectedArmor = armor;
        }
        else if (module is RoverBattery battery)
        {
            SelectedBattery = battery;
        }
        else if (module is RoverAntenna antenna)
        {
            SelectedAntenna = antenna;
        }

        RebuildSelectedModules();
        OnSelectionChanged?.Invoke();
    }

    public static void SaveSelection(RoverArmor armor, RoverBattery battery, RoverAntenna antenna)
    {
        SelectedArmor = armor;
        SelectedBattery = battery;
        SelectedAntenna = antenna;

        RebuildSelectedModules();
        OnSelectionChanged?.Invoke();
    }

    private static void RebuildSelectedModules()
    {
        selectedModules.Clear();
        if (SelectedArmor != null) selectedModules.Add(SelectedArmor);
        if (SelectedBattery != null) selectedModules.Add(SelectedBattery);
        if (SelectedAntenna != null) selectedModules.Add(SelectedAntenna);
    }
}
