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
}

public static class RoverModulesImplement
{
    public static IReadOnlyList<RoverBattery> batteryList = new RoverBattery[] {
        new() { name = "Niquelin", ah = 5000, price = 50, spriteClass = "niquelin-material" },
        new() { name = "Plomin", ah = 15000, price = 100, spriteClass = "plomin-material" },
        new() { name = "Hidraumax", ah = 20000, price = 150, spriteClass = "hidraumax-material" },
        new() { name = "Infinitum", ah = 30000, price = 200, spriteClass = "infinitum-material" }
    };

    public static IReadOnlyList<RoverArmor> armorList = new RoverArmor[] {
        new() { name = "Aluminio", bangs = 4, price = 300, spriteClass = "aluminium-armor" },
        new() { name = "Hierro", bangs = 10, price = 500, spriteClass = "iron-armor" },
        new() { name = "Acero", bangs = 20, price = 800, spriteClass = "steel-armor" }
    };

    public static IReadOnlyList<RoverAntenna> antennaList = new RoverAntenna[] {
        new() { name = "TP-Radin", radius = 5f, speed = 5f, spriteClass = "tp-radin" },
        new() { name = "TP-Rado", radius = 10f, speed = 2.5f, spriteClass = "tp-radio" },
        new() { name = "TP-Radmax", radius = 100f, speed = 10f, spriteClass = "tp-radmax" },
        new() { name = "TP-Supreme", radius = 100f, speed = 100f, spriteClass = "tp-supreme" }
    };
}

public static class RoverModuleSelection
{
    public static RoverArmor SelectedArmor { get; private set; }
    public static RoverBattery SelectedBattery { get; private set; }
    public static RoverAntenna SelectedAntenna { get; private set; }

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

        OnSelectionChanged?.Invoke();
    }
}
