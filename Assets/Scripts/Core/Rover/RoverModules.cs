using System.Collections.Generic;

public class RoverBattery : GenericModule
{
    public float ah;
    public float price;
}

public class RoverArmor: GenericModule
{
    public int bangs;
    public float price;
}

public class RoverAntenna: GenericModule
{
    public float radius;
    public float speed;
}

public static class RoverModulesImplement
{
    public static IReadOnlyList<RoverBattery> batteryList = new RoverBattery[] {
        new() {name = "Niquelin", ah = 5000, price = 50},
        new() {name = "Plomin", ah = 15000, price = 100},
        new() {name = "Hidraumax", ah = 20000, price = 150},
        new() {name = "Infinitum", ah = 30000, price = 200}
    };

    public static IReadOnlyList<RoverArmor> armorList = new RoverArmor[] {
        new() {name = "Aluminio", bangs = 4, price = 300},
        new() {name = "Hierro", bangs = 10, price = 500},
        new() {name = "Acero", bangs = 20, price = 800}
    };

    public static IReadOnlyList<RoverAntenna> antennaList = new RoverAntenna[] {
        new() {name = "TP-Radin", radius = 5f, speed = 5f},
        new() {name = "TP-Rado", radius = 10f, speed = 2.5f},
        new() {name = "TP-Radmax", radius = 100f, speed = 10f},
        new() {name = "TP-Supreme", radius = 100f, speed = 100f}
    };
}